# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.
- For code changes, always provide the full rewritten class/file, not just a diff/snippet — Craig pastes a complete file each time rather than manually merging partial additions.
- For hearthClone architecture, avoid singletons where possible — prefer explicit constructor/field-passed references (as the project already does throughout) over globally-reachable static instances, to keep dependencies visible and avoid Unity singleton pitfalls (script execution order, DontDestroyOnLoad issues, creeping responsibility).

_Last updated: 2026-08-10 (session 20 — added per-turn card draw and a visible mana counter (both confirmed working), then researched real Hearthstone rules to identify remaining gaps: fatigue, board/hand size caps drafted but not yet pasted in or tested)_

## How to use this file
Paste the contents of this file at the start of any new Claude chat to get instant context on the project. Update it at the end of each working session (ask Claude to update it, or do it yourself) so it never goes stale.

## Project Goal
A Hearthstone-style card game built in Unity, single-player vs AI, using C# with a data-driven card/effect system (ScriptableObjects). Developed on Ubuntu using Unity + VS Code, version controlled via Git/GitHub (with Git LFS enabled for art/audio).

**Unity version: 6.5 (60000.5.2f1).** Use TextMeshPro (TMP) for all UI text, not legacy `UnityEngine.UI.Text`.

## Tooling Setup (Done)
- Unity Hub + Unity LTS installed via AppImage
- VS Code with C# Dev Kit + Unity extension
- .NET SDK installed (for VS Code C# debugging, separate from Unity's own runtime)
- Git + GitHub connected via Unity Hub's built-in integration, authenticated via `gh auth login`
- Git LFS enabled for binary assets (art, audio)
- Repo: `github.com/craig-middleton/hearthClone`

## Architecture Decisions
- **Folder structure** under `Assets/_Project/`: Scripts, ScriptableObjects, Prefabs, Art, Scenes
- **Card/effect model**: data-driven via ScriptableObjects — cards reference reusable `CardEffect` assets instead of each card having bespoke code.
- **Assembly Definitions** (`.asmdef`) enforce one-way dependency flow: `Core` (no deps) ← `Effects` (deps: Core) ← `Cards` (deps: Core, Effects) ← `AI`/`UI` (deps: Core, Cards, Effects). `Effects` must NOT reference `Cards`.
- **No singletons** — every class is constructed explicitly and handed the exact references it needs.

## Code Written So Far

| File | Location | Purpose |
|---|---|---|
| `CardData.cs` | `Scripts/Cards/` | ScriptableObject: `cardName`, `description`, `artwork`, `manaCost`, `cardType` (`Minion`/`Spell`), `attack`, `health`, `onPlayEffect`, `targetsSelf`. |
| `CardEffect.cs` | `Scripts/Effects/` | Abstract `ScriptableObject` base; `Execute(GameContext, Target)`. |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Deals damage to a Target, logs remaining health. |
| `GainManaEffect.cs` | `Scripts/Effects/` | Calls `target.GainMana(manaAmount)`. Powers The Coin (and a planned second ramp card). |
| `Minion.cs` | `Scripts/Core/` | Runtime board minion (name, attack, health). **New this session:** `HasSummoningSickness` (true from construction, i.e. the instant a minion is played), `HasAttackedThisTurn`, `CanAttack` (computed: `!HasSummoningSickness && !HasAttackedThisTurn`), `ResetForNewTurn()` (clears both flags). Two independent flags rather than one combined bool, since sickness only ever needs clearing once (on the controller's next turn) while "already attacked" needs clearing every turn. |
| `Player.cs` | `Scripts/Core/` | Health, mana (current/max), board minions list. No `IsAI` flag. |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent(player)` helper. **New this session:** `RemoveDeadMinions()` — strips any minion with `IsDead == true` from both players' boards in one call, since a single attack can affect either side (the attacker can die too, from a minion trade). |
| `GameContext.cs` | `Scripts/Core/` | Holds a real `Board` reference. |
| `Target.cs` | `Scripts/Core/` | Points to a `Player` or `Minion`. `TakeDamage()`, `GetCurrentHealth()`, `GainMana(int)`. |
| `Combat.cs` | `Scripts/Core/` (**new this session**) | Static class, `TryAttack(Minion attacker, Target target, out string failReason)` — validates `attacker.CanAttack`, applies the attacker's damage to the target (reusing the existing `Target` class unchanged), and strikes back at the attacker if the target is a minion (simultaneous damage, matching Hearthstone). Static because it holds no state of its own — a pure function operating on whatever's passed in, closer to `Mathf`/`UnityEngine.Random` than a singleton; nothing reaches into it globally. |
| `TurnManager.cs` | `Scripts/Core/` | Turn order + mana progression. **New this session:** a private `StartTurnFor(player)` now wraps both mana refill and a new `ResetMinionsForNewTurn(player)` pass (calls `minion.ResetForNewTurn()` on every minion that player controls), called from both `StartGame()` and `EndTurn()`. This guarantees a minion summoned on Player One's turn stays sick through all of Player Two's turn, only clearing on Player One's own next turn — exactly matching real Hearthstone timing, no separate "turns since summoned" counter needed. |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper. Builds decks/hands, asymmetric opening hands + Coin, full mulligan flow, manual control mode toggle, click-to-attack combat wiring, `FaceView` health/mana display, win condition (`gameOver`/`winner`, `CheckWinCondition()`). **New this session:** `DrawForCurrentPlayer()` draws exactly one card for whichever player's turn is starting, called right after every `turnManager.EndTurn()` (both the human's End Turn and the second `EndTurn()` that follows the AI's turn) — turn 1 deliberately gets no extra draw since both players already have correct opening hands from `Start()`. Confirmed working via console logs (`"Player X drew {card}..."` now firing every turn, not just at game start). **Not permanent** — delete/rename once real gameplay loop exists. |
| `CardView.cs` | `Scripts/UI/` | Displays one card. Two setup modes sharing a private `WriteCardText()` helper — `SetCard()` (play mode) and `SetCardForMulligan()` (toggle-select mode with dim/highlight visual). |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — generic, reused for both hands. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with `Deck`/`Hand`. `Shuffle()`, `DrawOpeningHand(int)`, `DrawCard()`, `AddCardToHand()`, `MulliganCard()`. `PlayCard()` unchanged. **Drafted but NOT yet pasted in this session**: fatigue damage on empty-deck draw, a `MaxHandSize = 10` cap (overdraw burns the card), and a `MaxBoardSize = 7` cap enforced in `PlayCard()` before mana/hand are touched — see "Current Blocker" below, full code given in chat but application not yet confirmed. |
| `FaceView.cs` | `Scripts/UI/` | The visible player health display. **New this session:** now also shows mana — `healthText.text` includes a second line (`\nMana: {CurrentMana}/{MaxMana}`) alongside the existing HP line. Confirmed working via screenshot: both players' mana correctly ramping and capping at 10. |
| `MinionView.cs` | `Scripts/UI/` | **New this session:** now clickable — added `Button` and `Image` (`minionBackground`) fields. `SetMinion()` gains optional `clickCallback`, `isSelected`, and `showAttackEligibility` parameters (all default so every pre-existing call site keeps compiling unchanged). Visual state: selected attacker tints green (`selectedColor`); a minion that can't currently attack (only checked when `showAttackEligibility` is true, i.e. only for the board belonging to whoever's turn it currently is) tints grey (`cannotAttackColor`) — deliberately never applied to an opponent's board, since a defender's own `CanAttack` status is irrelevant to whether it can be attacked. |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard()` — **new this session:** now takes optional `onMinionClicked`, `selectedAttacker`, and `showAttackEligibility` parameters, passed straight through to each `MinionView.SetMinion()` call. Still generic, still reused for both boards via two separate instances. |
| `FaceView.cs` | `Scripts/UI/` (**new this session**) | The first-ever visible player health display. `SetPlayer(Player, Action<Player> clickCallback)` writes `"{PlayerName}: {Health} HP"` to a TMP text field and wires a `Button` click to the callback — doubles as the attack target for face damage. |
| `AIController.cs` | `Scripts/AI/` | `PerformMulligan(int threshold = 4)` — mulligans expensive cards. `TakeTurn()` — greedy play-what's-affordable loop, targeting-aware via `card.targetsSelf`. **New this session:** after the card-play loop, a new pass attacks the opponent's face directly with every minion that `CanAttack` — simple pure-aggression strategy (no trading/risk logic yet), then calls `board.RemoveDeadMinions()`. |

## Test Assets Created / In Progress
- Original 5: `TestCard_Wisp` (1, 1/1), `TestCard_Goblin` (2, 2/2), `TestCard_RiverCroc` "River Crocodile" (3, 2/3), `TestCard_Fireball` (4, Spell → `Effect_Deal3Damage`), `TestCard_Boulderfist` (5, 4/4).
- `TestCard_Coin` "The Coin" (0, Spell, `targetsSelf = true` → `Effect_GainMana1`) — kept OUT of `Card Pool`, granted only via `EffectTester`'s dedicated `Coin Card` field.
- `Effect_Deal3Damage` (damage = 3), `Effect_GainMana1` (manaAmount = 1).
- **Completed this session** — 10 more unique cards created, bringing `Card Pool` to 15 total entries (confirmed via Inspector screenshot) × `Copies Per Card = 2` = a real 30-card deck, matching Hearthstone's max-2-copies deckbuilding rule rather than just raising the copy count on the original 5:
  - `TestCard_Murloc` (1, 1/1)
  - `TesCard_Watchman` (2, 3/1) — **note the typo in the asset name** ("Tes" not "Test"), purely cosmetic, doesn't affect function, left as-is at Craig's discretion to fix later
  - `TestCard_Shieldbearer` (3, 1/5)
  - `TestCard_Warhorse` (3, 3/3)
  - `TestCard_ArcaneBolt` (3, Spell, `targetsSelf = false` → `Effect_Deal3Damage`)
  - `TestCard_Bear` (4, 3/6)
  - `TestCard_MagePupil` (4, Spell, `targetsSelf = true` → `Effect_GainMana1`)
  - `TestCard_ChargingRhino` (6, 5/5)
  - `TestCard_StoneGuardian` (6, 4/8)
  - `TestCard_AncientColossus` (7, 7/7)
  - No code changes were needed for this — `BuildDeck()`/`Copies Per Card` already handled any pool size. **Not yet playtested** — a fresh Play session drawing from the real 30-card deck hasn't been confirmed error-free yet (see Next Steps).

## Verified Working
- **Board visuals** (session 9), **AI turn logic** (session 10), **opponent board visualization** (session 11), **shuffled decks + draw + opponent hand display** (session 12), **asymmetric opening hands + Coin + multi-card AI turn** (session 13), **full mulligan system, both sides** (session 14) — all previously confirmed.
- **Manual control mode (this session)**: toggle exists and Player Two's hand does become clickable when enabled. **Turn-gating bug found via direct testing** (Craig played Player Two's Coin during Player One's turn) — root cause diagnosed (missing `CurrentPlayer` check) and fix written, but **not yet re-tested after the fix was applied** — confirm next session that turn order is now properly enforced in manual mode.

## Current Blocker / Last Thing Worked On
This session added two small confirmed features (per-turn draw, visible mana counter), then stepped back at Craig's request to research real Hearthstone rules and compare against the current project, in order to find any remaining gaps in core game integrity before moving on to new content/features.

**Confirmed working this session:**
- Per-turn card draw (`DrawForCurrentPlayer()` in `EffectTester`) — console logs confirm hands now grow every turn instead of only at game start.
- Visible mana counter on `FaceView` — confirmed via screenshot showing both players' mana correctly at `1/1` and `0/0` on Turn 1 (matching known Player One vs Player Two mana-refill timing).

**Research summary — real Hearthstone rules vs. hearthClone, compared directly:**
Confirmed fully matching: 30-card deck (max 2 copies), turn order/mana ramp, asymmetric 3-vs-4+Coin opening hand, mulligan (any number, reshuffle+redraw), per-turn draw, playing cards/summoning, click-to-attack combat, summoning sickness, one-attack-per-turn, minion death, win condition at 0 health, basic AI.

**Confirmed gaps, split into two tiers:**
- **Tier 1 (rules-level, affects every game, agreed as the next priority)**: no Fatigue (real Hearthstone: drawing from an empty deck deals escalating damage — 1st time 1 damage, 2nd time 2, etc., instead of doing nothing); no board size cap (real Hearthstone caps at 7 minions); no hand size cap (real Hearthstone caps at 10 cards, overdraw burns the card). None of these have come up yet in normal short playtests, but a long enough game (like the 25+ turn session two sessions ago) could hit them.
- **Tier 2 (content/feature work, deliberately treated as separate "next stage" material, not urgent)**: Taunt (already flagged from session 17), Deathrattle, Charge/Rush, Divine Shield, Windfury, Silence (all keyword mechanics `CardEffect`/`onPlayEffect` doesn't model yet — only Battlecry-style one-shot effects exist), Hero classes + Hero Powers (no hero identity system at all currently — every card is neutral), Weapons (a whole card type/slot that doesn't exist).

**Tier 1 code drafted this session but NOT YET PASTED IN OR TESTED** — full `Player.cs` and `PlayerHand.cs` rewrites were given in chat, covering:
- `Player.cs`: new `FatigueDamage` int counter (starts at 0).
- `PlayerHand.cs`: `DrawCard()` now deals escalating fatigue damage via `CorePlayer.TakeDamage()` when the deck is empty (instead of just logging and doing nothing), and burns (discards) a newly-drawn card if the hand is already at `MaxHandSize = 10`. `PlayCard()` gets a new check — if the card is a Minion and the board is already at `MaxBoardSize = 7`, the play is blocked entirely (before mana is deducted or the card leaves hand), matching real Hearthstone's "nothing happens" behavior for a blocked minion play.
- Both are drop-in replacements needing no Editor/scene changes — just paste and test next session.

**Cosmetic, still low priority:**
- `BoardPanel`/`OpponentBoardPanel` text-clipping issue (minion name cut off at left edge) unaddressed.
- Board panel vertical spacing could use more separation.
- `MulliganPanel` sits in the same screen position as `HandPanel` (fine functionally, hidden after confirm).

## Lessons Learned / Gotchas (useful to remember)
**Assembly definitions (.asmdef)**
- Circular references are rejected — keep dependency direction one-way.
- An asmdef's real identity is its **Name** field (Inspector), not its filename.
- `CS0246` errors can mean either a missing asmdef reference OR a missing `using` statement.
- Cross-assembly type usage needs BOTH the `using` directive AND an explicit **Assembly Definition Reference**.

**Unity Editor / UI basics**
- Script filename must exactly match its class name. Scene changes aren't saved until `Ctrl+S`.
- For stretch-anchored panels, reposition via Rect Transform `Pos Y`/`Left`/`Right`/`Height`, not free dragging.
- A circular gizmo appearing unexpectedly in Scene view is likely just the `Global Light 2D` gizmo — editor-only.
- Editing a runtime `(Clone)` GameObject in Play mode is temporary — use Prefab Edit Mode for permanent changes.
- An empty/unassigned Inspector field for a listener-style hookup fails **silently**.
- Editing a TMP label's `Text Input` box requires clicking in, changing text, then clicking away to commit.
- `CS0102` after a manual paste-in usually means a duplicated field/header block — this is why full-file pastes are now standard.
- A script field needing a component that lives on the *same* GameObject the script is attached to: drag that same GameObject onto its own field, Unity finds the matching component.
- **New this session**: a scene GameObject can accidentally become a prefab instance (showing `Prefab`/`Overrides`/`Select`/`Open` at the top of its Inspector) if it's ever dragged into the Project window — easy to do by accident. For a one-off bootstrapper object never meant to be reused as a prefab (like `EffectTester`), fix via right-click → **Prefab → Unpack Completely** (keeps current field values, restores it to a plain scene object), then delete the now-orphaned prefab asset from the Project window so it doesn't cause confusion later.

**Core architecture principle**
- Keep `Core` types generic and unaware of `CardData`. AI-ness isn't a flag on `Player`.
- When a display/rendering component is already generic, support "two of something" via a second *instance*, not code to juggle two lists.
- A callback using the null-conditional operator (`onClicked?.Invoke(card)`) gives "free" read-only-mode support.
- `ScriptableObject`-based cards support "multiple copies in a deck" via repeated list references — no asset duplication needed.
- Use in-place Fisher-Yates for shuffling a `List<T>`.
- A boolean flag on the data itself (`CardData.targetsSelf`) is the right way to encode "who does this affect."
- A temporary/this-turn-only stat bonus needs no explicit expiry if it modifies a field some other system already resets unconditionally each cycle.
- When a UI component needs two distinct interaction modes sharing most setup, pull shared parts into a private helper and give each mode its own public entry point, rather than a mode-flag parameter on one method.
- A "confirm/complete" boolean guard flag checked at the top of interactive handlers is a simple way to gate a whole phase without a full state machine — appropriate at small scale; worth revisiting as a real state machine if more phases (e.g. combat) get added.
- **New this session**: when adding a toggleable alternate mode (like `manualControlMode`) to code that already has implicit assumptions baked in (like "only Player One's hand is ever clickable, so no one checks whose turn it is"), audit those implicit assumptions explicitly — the toggle didn't introduce a new bug so much as expose a check (turn ownership) that was never needed before because only one hand was ever interactive. New modes are a good forcing function for surfacing this kind of hidden coupling.
- **New this session (deck design)**: Hearthstone's real deckbuilding rule is many unique cards each capped at ~2 copies, not few unique cards with many copies — worth preserving that shape (add more `CardData` assets, not a higher `copiesPerCard`) to keep matches feeling varied, and because `BuildDeck()`/`Copies Per Card` was deliberately built to scale to any pool size without code changes, so growing the card pool is pure content work, not engineering work.
- **New this session (UI Layout Groups)**: any prefab that's a child of a Horizontal/Vertical Layout Group with `Control Child Size` enabled MUST have a `Layout Element` component defining its own `Preferred Width`/`Height` — without one, the layout group has no sensible size to assign it and results can range from squished/clipped to comically oversized, depending on what "no size" resolves to. When one prefab (`CardView`) works fine in a layout group but a sibling prefab (`MinionView`) renders broken under the same layout settings, checking whether both actually have a `Layout Element` component is the first thing to check — don't assume the layout group settings themselves are wrong.
- **New this session (Editor view vs. real UI bugs)**: before concluding a UI element is actually clipped/broken, check whether the Game view *pane itself* in the Editor is just too short (e.g. a Console panel docked directly below eating vertical space) — this can crop the rendered frame and look identical to a real clipping bug. Try maximizing/resizing the Game view tab before assuming the underlying Rect Transform or Layout Group settings need changing.
- **New this session (Canvas nesting — recurring failure mode)**: if a UI GameObject has completely correct script wiring, Rect Transform values, and Inspector field assignments, but still never renders anything at all, check whether it's actually nested *under* the scene's `Canvas` in the Hierarchy before debugging anything else. A UI element that's a sibling of the Canvas (same indentation level) rather than a child of it will not render, full stop, regardless of how correct everything else about it is — and this is easy to cause by accident when dragging objects around in the Hierarchy. This exact issue hit the same two GameObjects (`PlayerFaceDisplay`/`OpponentFaceDisplay`) twice across sessions — worth checking Canvas nesting FIRST for any newly-invisible UI element, before checking scripts, values, or Inspector wiring.
- **New this session (Raycast Target on background images)**: a plain background `Image` component (one that isn't itself meant to be clicked) rarely needs `Raycast Target` checked. If it's left checked and the panel's position/size ever changes to overlap other clickable UI, it can silently intercept clicks meant for elements underneath it — with no error, just unresponsive buttons. Worth unchecking `Raycast Target` on purely-decorative background images as a general habit, not just when a bug like this actually appears.

**Git / GitHub / environment**
- GitHub requires `gh auth login` or a PAT (repo scope only) for HTTPS git auth.
- `.gitignore` should exclude `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`.
- Unity's crash-recovery prompt (`Assets/_Recovery/`) is safe to accept; delete the folder after.
- .NET SDK (install via Microsoft's apt feed, not Ubuntu's default repo) is only needed for VS Code's C# IntelliSense/debugging.

## Next Steps (in order)
1. **Paste in and test the Tier 1 code drafted this session**: updated `Player.cs` (adds `FatigueDamage`) and `PlayerHand.cs` (fatigue damage on empty-deck draw, 10-card hand cap with burn, 7-minion board cap blocking play). Test by playing a long enough game to actually empty a deck and/or fill a board to 7.
2. Quick check: confirm all input is truly inert after game over (cards, minions, End Turn all clicked once post-win to verify no residual action fires).
3. Quick explicit check: same minion attacking twice in one turn should be rejected (`HasAttackedThisTurn`) — logic looks sound and was indirectly exercised, but never explicitly tried.
4. Consider smarter AI attack logic (trading/risk awareness) — right now it's pure face-aggression only.
5. **Tier 2 content work (deliberately separate "next stage")**: Taunt, Deathrattle, Charge/Rush, Divine Shield, Windfury, Silence, Hero classes + Hero Powers, Weapons — each would need its own scoping/design pass before building, same as mulligan and combat did.
6. Record `BoardPanel`'s corrected `Pos Y` value and `HandPanel`'s current Rect Transform values for documentation completeness.
7. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
8. Consider upgrading click-to-play to real drag-and-drop, if desired.

## Git Habits Being Followed
- Simple commit template: `git add .` / `git commit -m "short one-line summary"` / `git push`
- Push regularly to `github.com/craig-middleton/hearthClone`
