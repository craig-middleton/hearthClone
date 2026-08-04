# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.
- For code changes, always provide the full rewritten class/file, not just a diff/snippet — Craig pastes a complete file each time rather than manually merging partial additions.
- For hearthClone architecture, avoid singletons where possible — prefer explicit constructor/field-passed references (as the project already does throughout) over globally-reachable static instances, to keep dependencies visible and avoid Unity singleton pitfalls (script execution order, DontDestroyOnLoad issues, creeping responsibility).

_Last updated: 2026-08-04 (session 15 — added a manual-control testing toggle so Craig can play both sides directly; scoped a future combat/Taunt system; deck expanded to a real 30 cards; cleaned up an accidental EffectTester prefab)_

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
| `Minion.cs` | `Scripts/Core/` | Runtime board minion (name, attack, health) only — **no Taunt flag, no attack/combat capability yet** (see Next Steps: Combat System). |
| `Player.cs` | `Scripts/Core/` | Health, mana (current/max), board minions list. No `IsAI` flag. |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent(player)` helper. |
| `GameContext.cs` | `Scripts/Core/` | Holds a real `Board` reference. |
| `Target.cs` | `Scripts/Core/` | Points to a `Player` or `Minion`. `TakeDamage()`, `GetCurrentHealth()`, `GainMana(int)`. |
| `TurnManager.cs` | `Scripts/Core/` | Turn order + mana progression. `StartGame()`: turn 1, Player One first, **only refills Player One's mana at game start** — Player Two's mana stays at 0/0 until their first real `EndTurn()`. `EndTurn()`: swaps `CurrentPlayer`, increments turn, refills mana (+1/turn, capped at 10). |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper. Builds both decks/hands, asymmetric 3-vs-4+Coin opening hands, full mulligan flow (human UI + AI auto-mulligan). **New this session:** `manualControlMode` bool field (Inspector-toggleable) — when true, Player Two's hand becomes clickable via a new `OnOpponentCardClicked()` handler (mirrors `OnCardClicked()`, operates on `playerTwoHand`/`playerTwo`), and `OnEndTurnClicked()` skips `aiController.TakeTurn()` entirely, leaving both sides to be played manually. `RefreshHandDisplay()` passes `OnOpponentCardClicked` as the opponent hand's callback only when `manualControlMode` is true (`null` otherwise, same as before). **Bug found and fixed this session**: both `OnCardClicked()` and `OnOpponentCardClicked()` now also check `turnManager.CurrentPlayer` and early-return if it's not that hand's owner's turn — without this, both hands were clickable at any time regardless of whose turn it was, which let Player Two's cards be played during Player One's turn (surfaced as a mana-tracking confusion, since Player Two's mana hadn't been refilled by a real turn yet). Mulligan phase itself is unaffected by `manualControlMode` — the AI always auto-mulligans for Player Two, only card-play/turn-taking is manual-mode-aware. |
| `CardView.cs` | `Scripts/UI/` | Displays one card. Two setup modes sharing a private `WriteCardText()` helper — `SetCard()` (play mode) and `SetCardForMulligan()` (toggle-select mode with dim/highlight visual). |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — generic, reused for both hands. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with `Deck`/`Hand`. `Shuffle()`, `DrawOpeningHand(int)`, `DrawCard()`, `AddCardToHand()`, `MulliganCard()`. `PlayCard()` unchanged. |
| `MinionView.cs` | `Scripts/UI/` | Display-only, no click/Button. |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard(List<Minion>)` — generic, reused for both boards. |
| `AIController.cs` | `Scripts/AI/` | `PerformMulligan(int threshold = 4)` — mulligans expensive cards. `TakeTurn()` — greedy play-what's-affordable loop, targeting-aware via `card.targetsSelf`. |

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
None blocking, but several verification steps are queued for next session — this was a session heavy on building/setup with less time left for playtesting than usual. In order: manual control mode was built, a real bug found and fixed (turn-gating), fix not yet re-verified; the 10 new test cards were completed and confirmed in `Card Pool` (30-card deck), but not yet playtested; and `EffectTester` was found to have accidentally become a prefab asset at some point (likely from being dragged into the Project window) rather than staying a plain scene GameObject — this was fixed at the very end of the session via **Unpack Completely** + deleting the orphaned prefab asset from the Project window, restoring it to a normal scene GameObject. **This fix itself hasn't been tested with a fresh Play session yet either** — worth confirming `EffectTester` still behaves identically now that it's unpacked.

**Immediate next-session priorities, in order:**
1. Confirm `EffectTester` still works correctly after being unpacked from its accidental prefab state (quick sanity check — Play, confirm no missing references, no errors).
2. Confirm the 30-card deck plays correctly — a fresh Play session hasn't been run since the 10 new cards were added (console should show `Deck remaining: 27` after Player One's 3-card draw, `Deck remaining: 25` after Player Two's 4-card draw + Coin, given a 30-card deck).
3. Confirm the turn-gating fix (`OnCardClicked`/`OnOpponentCardClicked` now checking `turnManager.CurrentPlayer`) actually resolves the manual-mode issue — Player Two's cards should be unclickable during Player One's turn and vice versa.
4. **Original regression check, still not done** (carried over from session 14, got sidetracked into manual-mode work this session): confirm End Turn / normal AI-controlled turn-taking still works correctly after a mulligan completes — this predates the manual-mode toggle and should be checked with `manualControlMode = false`.
5. Re-confirm `TestCard_Goblin`'s Mana Cost is `2` (long-running check-item, still not re-verified in several sessions).

**Newly scoped, deliberately deferred to its own future session:**
- **Combat system (attacking + Taunt)** — currently minions can be summoned to a board but nothing can ever attack anything; `Target.TakeDamage()` is only ever invoked by spell effects (Fireball) today, never by minion combat. Taunt is a targeting *rule* ("must attack a Taunt minion first if one exists") that inherently depends on an attack system existing first, so it can't be built in isolation. This is a meaningfully large feature — likely comparable in scope to the mulligan system or larger — touching `Minion` (Taunt flag, "has attacked this turn" tracking), `Player`/`Board` (attack targeting/validation rules), UI (click-to-attack interaction), and `AIController` (attack decision-making). Deliberately not started casually at a session's end; needs its own planning pass (design decisions, then build) the way mulligan got.
- Related, smaller and still deferred: on-play effects beyond simple spell-style damage/mana (e.g. Deathrattles, passive/aura effects) — `CardEffect`/`onPlayEffect` currently only models one-shot Battlecry-style effects.

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

**Git / GitHub / environment**
- GitHub requires `gh auth login` or a PAT (repo scope only) for HTTPS git auth.
- `.gitignore` should exclude `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`.
- Unity's crash-recovery prompt (`Assets/_Recovery/`) is safe to accept; delete the folder after.
- .NET SDK (install via Microsoft's apt feed, not Ubuntu's default repo) is only needed for VS Code's C# IntelliSense/debugging.

## Next Steps (in order)
1. Confirm `EffectTester` behaves correctly after being unpacked from its accidental prefab state.
2. Verify the 30-card deck draws correctly in a fresh Play session.
3. Confirm the manual-mode turn-gating fix works.
4. Regression-check normal AI-controlled turn-taking (manual mode off) still works post-mulligan.
5. Re-confirm `TestCard_Goblin`'s Mana Cost is `2`.
6. Board panel spacing + minion text-clipping cosmetic cleanup.
7. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
8. Consider upgrading click-to-play to real drag-and-drop, if desired.
9. **Combat system (attacking + Taunt)** — dedicated future session; needs its own design/scoping pass before building (see "Newly scoped" above).

## Git Habits Being Followed
- Simple commit template: `git add .` / `git commit -m "short one-line summary"` / `git push`
- Push regularly to `github.com/craig-middleton/hearthClone`
