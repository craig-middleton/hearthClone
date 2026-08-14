# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.
- For code changes, always provide the full rewritten class/file, not just a diff/snippet — Craig pastes a complete file each time rather than manually merging partial additions.
- For hearthClone architecture, avoid singletons where possible — prefer explicit constructor/field-passed references (as the project already does throughout) over globally-reachable static instances, to keep dependencies visible and avoid Unity singleton pitfalls (script execution order, DontDestroyOnLoad issues, creeping responsibility).

_Last updated: 2026-08-13 (session 23 — Craig set up Claude Code (desktop app, Local environment) against the actual project folder for the first time; it found and fixed two real bugs: a late win-check after a lethal AI turn, and a silent no-op on a targetless card effect)_

## Tooling note: Claude Code now in the workflow
Craig has Claude Desktop installed on Ubuntu and has started using its **Code** tab (Local environment, pointed at the `hearthClone` project folder) as an additional way to work on the codebase, alongside this chat-based workflow. Claude Code reads/edits files directly on disk — no more copy-pasting needed when working that way. Both workflows are expected to continue being used; this file should stay the single source of truth regardless of which one makes a given change, so any session (chat-based or Claude Code) should update it.

## How to use this file
Paste the contents of this file at the start of any new Claude chat to get instant context on the project. Update it at the end of each working session (ask Claude to update it, or do it yourself) so it never goes stale.

## NEXT SESSION PRIORITY: Card Artwork
Craig wants to start introducing visuals, beginning with real card artwork (not effects/animations first). He'll generate images externally via an AI image tool of his choice — Claude has no image-generation capability inside this Unity/coding context, so this step happens outside the conversation. Plan, agreed but not yet started:
1. Craig generates card art externally (even just 1-2 images to start is enough to wire up and test before doing all 15).
2. Import into Unity: drag image files into the `Art` folder, select each, change **Texture Type** to `Sprite (2D and UI)` in the Inspector, Apply.
3. Assign to `CardData`: each card asset already has an unused `Artwork` field (a `Sprite`, present since the very first `CardData.cs` was written) — drag the sprite onto the field for the matching card.
4. **Code change needed (not yet written)**: `CardView.cs` currently only ever writes text (name/cost/stats) — it needs a new `Image` component reference and one line in both `SetCard()` and `SetCardForMulligan()` to display `card.artwork`.
5. Editor step: add an `Image` child (or reuse the existing background) on the `CardView` prefab, wire it to the new field.
Craig explicitly asked Claude to remember this as the very next thing to pick up.

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
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper. Builds decks/hands, asymmetric opening hands + Coin, full mulligan flow, manual control mode toggle, click-to-attack combat wiring, `FaceView` health/mana display, win condition, per-turn draw, Taunt enforcement, minimal Hero Power. **Bug fixed via Claude Code this session**: `OnEndTurnClicked()` previously called `CheckWinCondition()` only once, at the very end, after both `EndTurn()` calls and the AI's full `TakeTurn()` had already run — meaning if the AI's own attack phase landed the killing blow, the code would still barrel through refilling the human's mana and clearing minion summoning sickness for a turn that should never have started. Fixed by calling `CheckWinCondition()` immediately after each turn-advancing step (first `EndTurn()`+draw, then again after `aiController.TakeTurn()`), with each subsequent step gated on `!gameOver`. Pure reordering — `CheckWinCondition()` was already safe to call repeatedly since it early-returns `if (gameOver) return;`. This edge case was never hit in prior playtesting since all observed wins happened via manual `ResolveAttack()` clicks, which already checked the win condition correctly. **Not permanent** — delete/rename once real gameplay loop exists. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with `Deck`/`Hand`. `Shuffle()`, `DrawOpeningHand()`, `AddCardToHand()`, `MulliganCard()`. `DrawCard()` deals escalating fatigue damage (`CorePlayer.FatigueDamage++` then `TakeDamage()`) when the deck is empty; burns a newly-drawn card if `Hand.Count >= MaxHandSize` (10). `PlayCard()` blocks Minion plays if `BoardMinions.Count >= MaxBoardSize` (7, checked before mana/hand are touched), and passes `card.hasTaunt` through to the `Minion` constructor on summon (Taunt fully verified working; fatigue/board cap not yet stress-tested — need a long enough game to trigger either). **Bug fixed via Claude Code this session**: `PlayCard()`'s effect-execution line was `if (card.onPlayEffect != null && effectTarget != null)` — if a card had an effect but no target was passed in, the card still cost mana and left the hand, but the effect silently never fired, with no error or log. Now logs a warning (`"{cardName} has an onPlayEffect but no target was provided — effect was skipped."`) in that case instead of failing silently. Every current caller (`EffectTester`, `AIController`) always passes a target today, so this doesn't change current behavior — it's there to immediately surface the issue if a future code path ever forgets to pass one. |
| `Player.cs` | `Scripts/Core/` | Health, mana, board minions, `FatigueDamage`. **New this session**: `HasUsedHeroPowerThisTurn` bool, mirroring `Minion.HasAttackedThisTurn`'s pattern. |
| `TurnManager.cs` | `Scripts/Core/` | Turn order + mana progression + minion turn-reset. **New this session**: `StartTurnFor()` also resets `player.HasUsedHeroPowerThisTurn = false` alongside the existing mana refill and minion reset. |
| `MinionView.cs` | `Scripts/UI/` | Displays one board minion. **New this session**: `nameText.text` appends `" (Taunt)"` when `minion.HasTaunt` — a minimal but functional visual indicator (no new prefab art), confirmed rendering correctly on both boards. |
| `AIController.cs` | `Scripts/AI/` | `PerformMulligan()`, `TakeTurn()` (card-play loop + attack phase). **New this session (Taunt)**: the attack loop now checks `board.GetTauntMinions(opponent)` before choosing a target for each attacking minion — if the opponent has any Taunt minions, the AI attacks the first one found instead of face; otherwise face as before. Uses the same `Board.GetTauntMinions()` helper as the human path, keeping both sides consistent. |
| `CardView.cs` | `Scripts/UI/` | Displays one card. Two setup modes sharing a private `WriteCardText()` helper — `SetCard()` (play mode) and `SetCardForMulligan()` (toggle-select mode with dim/highlight visual). |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — generic, reused for both hands. |
| `CardData.cs` | `Scripts/Cards/` | ScriptableObject: `cardName`, `description`, `artwork`, `manaCost`, `cardType`, `attack`, `health`, `hasTaunt` (new this session), `onPlayEffect`, `targetsSelf`. |
| `Minion.cs` | `Scripts/Core/` | Runtime board minion. `HasSummoningSickness`, `HasAttackedThisTurn`, `CanAttack`, `ResetForNewTurn()`. **New this session**: `HasTaunt` bool field, settable via an optional constructor parameter (`hasTaunt = false` default) so all existing call sites still compile. |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent()`, `RemoveDeadMinions()`. **New this session**: `GetTauntMinions(Player player)` — returns whatever Taunt minions that player currently controls (no separate "alive" filter needed, since dead minions are already stripped by `RemoveDeadMinions()`). Used identically by both the human attack path (`EffectTester`) and the AI (`AIController`) to enforce the same rule on both sides. |
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
None — a productive session under time pressure. Tier 1 (fatigue, board/hand caps) was pasted in and compiled cleanly but wasn't stress-tested this session (neither is easy to trigger in a short playtest). Taunt was designed, built, and **fully verified working in real play** in the same session — both the rejection path and the successful-attack path confirmed via direct testing.

**Taunt — confirmed working via direct playtesting:**
- `TestCard_Shieldbearer` (1/5 Minion) had its new `Has Taunt` checkbox enabled in the Inspector — no new card asset needed, just flipped the existing card's flag.
- Both boards correctly render `"Shieldbearer (Taunt)"` for any Taunt minion.
- **Rejection confirmed**: tried attacking a non-Taunt enemy minion while the opponent had a Taunt minion in play — correctly blocked, console showed `"Player Two has a Taunt minion — you must attack it first."`, and the attacker stayed selected.
- **Success confirmed**: attacking the Taunt minion directly afterward correctly resolved — console showed `"Shieldbearer attacked."`, and the Taunt minion's health dropped exactly as expected (1/3 → 1/2 from a 1-attack hit).
- AI-side enforcement (`AIController.cs` checking `GetTauntMinions()` before choosing its own attack target) was built using the identical `Board` helper as the human path, but wasn't separately confirmed via AI-initiated attacks this session — worth watching for next time a game reaches a state where the AI faces an opposing Taunt minion.

**Tier 1 (fatigue/board/hand caps) — pasted in, compiles cleanly, not yet stress-tested:**
- Neither fatigue (needs a fully emptied deck) nor the board cap (needs 7 minions on one side) came up naturally in that session's shorter Taunt-focused playtest. Worth a longer test session at some point to confirm both actually fire correctly when triggered.

**Session 23 — first Claude Code session, two real bugs found and fixed** (see `EffectTester.cs`/`PlayerHand.cs` rows above for full detail): a late win-check that could let a turn transition happen after the AI already won mid-turn, and a silent no-op when a card effect had no target passed in. Both fixes reviewed and confirmed sound in this chat. Neither has been re-playtested since the fix — the win-check bug in particular is hard to trigger deliberately, since it needs the AI's attack phase to land the exact killing blow.

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
1. **Card artwork** (see "NEXT SESSION PRIORITY" note above) — the very next thing to pick up.
2. Longer playtest to actually trigger and confirm fatigue and the 7-minion board cap.
3. Watch for an AI-initiated attack against an opposing Taunt minion to confirm that side of the enforcement too.
4. Quick check: confirm all input is truly inert after game over.
5. Quick explicit check: same minion attacking twice in one turn should be rejected.
6. Consider smarter AI attack logic (trading/risk awareness).
7. **Remaining Tier 2 content work**: Deathrattle, Charge/Rush, Divine Shield, Windfury, Silence, full Hero classes + real Hero Powers (current one is a deliberate minimal placeholder, not the real system), Weapons — each needs its own scoping/design pass before building.
8. Record `BoardPanel`'s corrected `Pos Y` value and `HandPanel`'s current Rect Transform values for documentation completeness.
9. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
10. Consider upgrading click-to-play to real drag-and-drop, if desired.

## Git Habits Being Followed
- Simple commit template: `git add .` / `git commit -m "short one-line summary"` / `git push`
- Push regularly to `github.com/craig-middleton/hearthClone`
