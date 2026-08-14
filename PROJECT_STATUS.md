# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.
- For code changes, always provide the full rewritten class/file, not just a diff/snippet — Craig pastes a complete file each time rather than manually merging partial additions.
- For hearthClone architecture, avoid singletons where possible — prefer explicit constructor/field-passed references (as the project already does throughout) over globally-reachable static instances, to keep dependencies visible and avoid Unity singleton pitfalls (script execution order, DontDestroyOnLoad issues, creeping responsibility).

_Last updated: 2026-08-15 (session 24 — card artwork pipeline built and fully verified: Leonardo AI-generated art now displays correctly on both hand cards (CardView) and board minions (MinionView), with graceful fallback to text-only for cards without art)_

## Tooling note: Claude Code now in the workflow
Craig has Claude Desktop installed on Ubuntu and has started using its **Code** tab (Local environment, pointed at the `hearthClone` project folder) as an additional way to work on the codebase, alongside this chat-based workflow. Claude Code reads/edits files directly on disk — no more copy-pasting needed when working that way. Both workflows are expected to continue being used; this file should stay the single source of truth regardless of which one makes a given change, so any session (chat-based or Claude Code) should update it.

## How to use this file
Paste the contents of this file at the start of any new Claude chat to get instant context on the project. Update it at the end of each working session (ask Claude to update it, or do it yourself) so it never goes stale.

## Card Artwork — DONE this session
Full pipeline built and verified working end-to-end:
- **Art source**: Leonardo AI (free tier), JPG downloads work fine (no transparency needed since art fills the card's background area). Aspect ratio guidance used: portrait, close to 3:4, e.g. 768×1024 or similar.
- **Import**: drag JPG into `Art` folder → select it → Inspector → **Texture Type** → `Sprite (2D and UI)` → Apply.
- **Assignment**: drag the imported sprite onto the relevant `CardData` asset's pre-existing `Artwork` field (this field existed since the very first `CardData.cs` but was unused until now).
- **Code (both `CardView.cs` and `MinionView.cs`)**: each gained a new `Image artworkImage` field; the existing shared text-writing helper (`WriteCardText()` in `CardView`, inline in `MinionView.SetMinion()`) now also sets `artworkImage.sprite` when `card.artwork`/`minion.Artwork` is non-null, or sets `artworkImage.enabled = false` to cleanly hide the image slot when there's no art — so cards/minions without generated art still render correctly as text-only, no broken/blank sprites.
- **`Minion.cs`**: gained an `Artwork` field (`Sprite`) and a new optional constructor parameter, since board minions are runtime objects separate from `CardData` and needed their own copy of the reference. `PlayerHand.PlayCard()` passes `card.artwork` through when constructing a `Minion`, mirroring how `card.hasTaunt` was already threaded through the same constructor call.
- **Editor setup (done for both `CardView` and `MinionView` prefabs)**: added a new `Image` child (`ArtworkImage`) positioned as the *first* child (so it renders behind the name/stats text, not covering it), wired to the new script field.
- **Bug hit and fixed this session**: on `MinionView`, the `Artwork Image` field was left unassigned (`None`) after adding the child — this is the same "empty field fails silently" pattern hit several times before (Button/Image references need explicit dragging, the code can't infer it). Board minions showed a blank white box (Unity's default unconfigured `Image` color) instead of either the real art or a clean hidden fallback, until the field was actually wired.
- **Confirmed via screenshot**: Goblin (the only card with generated art so far) displays correctly on both the hand card and the board minion, on both players' sides; cards/minions without art (Wisp, Shieldbearer, etc.) correctly show as plain text with no visual glitch.
- **Only one card has real art so far (Goblin)** — the rest of the 15-card pool still needs art generated the same way if Craig wants full coverage. No further code/Editor work needed for additional cards — just repeat the generate → import → assign steps per card.

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
| `CardEffect.cs` | `Scripts/Effects/` | Abstract `ScriptableObject` base; `Execute(GameContext, Target)`. |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Deals damage to a Target, logs remaining health. |
| `GainManaEffect.cs` | `Scripts/Effects/` | Calls `target.GainMana(manaAmount)`. Powers The Coin. |
| `CardData.cs` | `Scripts/Cards/` | ScriptableObject: `cardName`, `description`, `artwork` (Sprite — now actively used, see Card Artwork section), `manaCost`, `cardType`, `attack`, `health`, `hasTaunt`, `onPlayEffect`, `targetsSelf`. |
| `Minion.cs` | `Scripts/Core/` | Runtime board minion. `HasSummoningSickness` (true from construction), `HasAttackedThisTurn`, `CanAttack` (computed), `ResetForNewTurn()`, `HasTaunt` (optional constructor param, `false` default), `Artwork` (Sprite, optional constructor param, `null` default). All new fields use optional constructor parameters so existing call sites keep compiling unchanged as each was added. |
| `Player.cs` | `Scripts/Core/` | Health, mana (current/max), board minions list, `FatigueDamage`, `HasUsedHeroPowerThisTurn`. No `IsAI` flag. |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent()`, `RemoveDeadMinions()`, `GetTauntMinions(Player player)` (returns that player's Taunt minions — no separate "alive" filter needed since dead minions are already stripped by `RemoveDeadMinions()`). Used identically by both the human attack path (`EffectTester`) and the AI (`AIController`) to enforce Taunt consistently on both sides. |
| `GameContext.cs` | `Scripts/Core/` | Holds a real `Board` reference. |
| `Target.cs` | `Scripts/Core/` | Points to a `Player` or `Minion`. `TakeDamage()`, `GetCurrentHealth()`, `GainMana(int)`. |
| `Combat.cs` | `Scripts/Core/` | Static class, `TryAttack(Minion attacker, Target target, out string failReason)` — validates `attacker.CanAttack`, applies damage to the target, strikes back at the attacker if the target is a minion (simultaneous damage). Static/stateless — a pure function, not a singleton. |
| `TurnManager.cs` | `Scripts/Core/` | Turn order + mana progression. A private `StartTurnFor(player)` wraps mana refill, `ResetMinionsForNewTurn(player)` (clears summoning sickness/attacked flags), and `player.HasUsedHeroPowerThisTurn = false` — called from both `StartGame()` and `EndTurn()`. Guarantees a minion summoned on one turn stays sick through the opponent's whole turn, clearing only on the controller's own next turn — matching real Hearthstone timing with no separate counter needed. |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper. Builds decks/hands, asymmetric opening hands + Coin, full mulligan flow, manual control mode toggle, click-to-attack combat wiring, `FaceView` health/mana display, win condition, per-turn draw, Taunt enforcement, minimal Hero Power (`heroPowerButton`/`OnHeroPowerClicked()`, 2 mana, deal 1 to enemy face). **Bug fixed via Claude Code**: `OnEndTurnClicked()` previously called `CheckWinCondition()` only once at the very end, after both `EndTurn()` calls and the AI's full `TakeTurn()` had already run — if the AI's own attack landed the killing blow, the code would still refill the human's mana/clear minion sickness for a turn that should never have started. Fixed by calling `CheckWinCondition()` immediately after each turn-advancing step, gated on `!gameOver`. **Not permanent** — delete/rename once real gameplay loop exists. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with `Deck`/`Hand`. `Shuffle()`, `DrawOpeningHand()`, `AddCardToHand()`, `MulliganCard()`. `DrawCard()` deals escalating fatigue damage when the deck is empty; burns a newly-drawn card if hand is full (10). `PlayCard()` blocks Minion plays if the board is full (7, checked before mana/hand are touched), and passes `card.hasTaunt`/`card.artwork` through to the `Minion` constructor on summon. **Bug fixed via Claude Code**: the effect-execution line silently skipped a card's effect with no log if a target wasn't provided — now logs a warning instead. Taunt and artwork fully verified working; fatigue/board cap not yet stress-tested (need a long enough game to trigger either). |
| `CardView.cs` | `Scripts/UI/` | Displays one card. Two setup modes sharing a private `WriteCardText()` helper — `SetCard()` (play mode) and `SetCardForMulligan()` (toggle-select mode with dim/highlight visual). **New this session (artwork)**: a new `artworkImage` field; `WriteCardText()` now also sets `artworkImage.sprite = card.artwork` when present, or `artworkImage.enabled = false` to cleanly hide it when a card has no art — confirmed working for both display modes since both share the same helper. |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — generic, reused for both hands. |
| `MinionView.cs` | `Scripts/UI/` | Displays one board minion. Clickable (`Button`), `minionBackground` (Image) for selected/attack-eligibility tinting, `nameText` appends `" (Taunt)"` for Taunt minions. `SetMinion()` takes optional `clickCallback`, `isSelected`, `showAttackEligibility` (all defaulted so old call sites still compile). **New this session (artwork)**: a new `artworkImage` field, same pattern as `CardView` — shows `minion.Artwork` if present, hides itself if not. **Bug hit and fixed this session**: the `Artwork Image` field was left unassigned in the prefab Inspector after adding the child, causing a blank white box instead of either art or a clean hidden fallback — fixed by dragging the `ArtworkImage` child onto the field. |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard()` — takes optional `onMinionClicked`, `selectedAttacker`, `showAttackEligibility`, passed straight through to each `MinionView.SetMinion()` call. Generic, reused for both boards via two separate instances. |
| `FaceView.cs` | `Scripts/UI/` | The visible player health + mana display. `SetPlayer(Player, Action<Player> clickCallback)` writes `"{PlayerName}: {Health} HP\nMana: {CurrentMana}/{MaxMana}"` and wires a `Button` click to the callback — doubles as the attack target for face damage. |
| `AIController.cs` | `Scripts/AI/` | `PerformMulligan()`, `TakeTurn()` (card-play loop + attack phase). Attack loop checks `board.GetTauntMinions(opponent)` before choosing a target for each attacking minion — attacks the first Taunt minion found if any exist, otherwise face. Uses the same `Board.GetTauntMinions()` helper as the human path, keeping both sides consistent. |

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
1. Generate art for the remaining 14 cards, following the same Leonardo AI → import → assign steps documented above (only Goblin has real art so far).
2. Longer playtest to actually trigger and confirm fatigue and the 7-minion board cap.
3. Watch for an AI-initiated attack against an opposing Taunt minion to confirm that side of the enforcement too.
4. Quick check: confirm all input is truly inert after game over.
5. Quick explicit check: same minion attacking twice in one turn should be rejected.
6. Consider smarter AI attack logic (trading/risk awareness).
7. **Remaining Tier 2 content work**: Deathrattle, Charge/Rush, Divine Shield, Windfury, Silence, full Hero classes + real Hero Powers (current one is a deliberate minimal placeholder, not the real system), Weapons — each needs its own scoping/design pass before building.
8. Record `BoardPanel`'s corrected `Pos Y` value and `HandPanel`'s current Rect Transform values for documentation completeness.
9. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
10. Consider upgrading click-to-play to real drag-and-drop, if desired.
11. Consider simple visual effects/animation feedback (flashes, particles for spells/attacks) as the next visual layer after card artwork.

## Git Habits Being Followed
- Simple commit template: `git add .` / `git commit -m "short one-line summary"` / `git push`
- Push regularly to `github.com/craig-middleton/hearthClone`
