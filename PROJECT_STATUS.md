# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.
- For code changes, always provide the full rewritten class/file, not just a diff/snippet — Craig pastes a complete file each time rather than manually merging partial additions.
- For hearthClone architecture, avoid singletons where possible — prefer explicit constructor/field-passed references (as the project already does throughout) over globally-reachable static instances, to keep dependencies visible and avoid Unity singleton pitfalls (script execution order, DontDestroyOnLoad issues, creeping responsibility).

_Last updated: 2026-08-03 (session 14 — full mulligan system built and verified end-to-end for both the human player and the AI)_

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
- **Card/effect model**: data-driven via ScriptableObjects — cards reference reusable `CardEffect` assets (e.g. "Deal 3 Damage", "Gain 1 Mana") instead of each card having bespoke code.
- **Assembly Definitions** (`.asmdef`) enforce one-way dependency flow:
  ```
  Core (no dependencies)
    ↑
  Cards (depends on Core, Effects)
    ↑
  Effects (depends on Core only)
    ↑
  AI, UI (depend on Core, Cards, Effects)
  ```
  `Effects` must NOT reference `Cards`. `AI.asmdef` references `Core`, `Cards`, `Effects`. `UI.asmdef` references `AI.asmdef` too, since `EffectTester` calls into `AIController`.
- **No singletons** — every class is constructed explicitly and handed the exact references it needs (see Working Preferences above). This has held consistently since the project's start and is now an explicit stated preference, not just an emergent pattern.

## Code Written So Far

| File | Location | Purpose |
|---|---|---|
| `CardData.cs` | `Scripts/Cards/` | ScriptableObject: `cardName`, `description`, `artwork`, `manaCost`, `cardType` (`Minion`/`Spell`), `attack`, `health`, `onPlayEffect`, `targetsSelf` (declares whether a card's effect targets its own caster, e.g. The Coin, vs the opponent). |
| `CardEffect.cs` | `Scripts/Effects/` | Abstract `ScriptableObject` base class; `Execute(GameContext, Target)`. |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Deals damage to a Target, logs remaining health. |
| `GainManaEffect.cs` | `Scripts/Effects/` | Same pattern as `DealDamageEffect`; calls `target.GainMana(manaAmount)`. Powers The Coin. |
| `Minion.cs` | `Scripts/Core/` | Runtime board minion (name, attack, health). Generic — no `CardData` knowledge. |
| `Player.cs` | `Scripts/Core/` | Health, mana (current/max), board minions list. No `IsAI` flag. |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent(player)` helper. |
| `GameContext.cs` | `Scripts/Core/` | Holds a real `Board` reference. |
| `Target.cs` | `Scripts/Core/` | Points to a `Player` or `Minion`. `TakeDamage()`, `GetCurrentHealth()`, `GainMana(int)` (adds to `CurrentMana` only — a one-turn bonus naturally cleared by `TurnManager.RefillMana()`). |
| `TurnManager.cs` | `Scripts/Core/` | Turn order + mana progression. `StartGame()`: turn 1, Player One first. `EndTurn()`: swaps `CurrentPlayer`, increments turn, refills mana (+1/turn, capped at 10). |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper MonoBehaviour. `cardPool`/`copiesPerCard` build each player's deck; both hands `.Shuffle()`. Player One draws 3 (`DrawOpeningHand(3)`, goes first); Player Two draws 4 + gets `coinCard` via `AddCardToHand()` (goes second). **New this session:** full mulligan orchestration. In `Start()`, after hands/Coin are set up, `aiController.PerformMulligan()` runs immediately (AI needs no UI), then `ShowMulliganUI()` populates a dedicated `mulliganPanel` with the human's hand using `CardView.SetCardForMulligan()` (toggle-select, not immediate play). Clicking cards toggles membership in a `mulliganSelections` HashSet via `OnMulliganCardToggled()`. `OnConfirmMulliganClicked()` (wired to `confirmMulliganButton`) calls `playerOneHand.MulliganCard()` for each selected card, tears down the mulligan panel/GameObjects, sets `mulliganComplete = true`, reveals the normal hand/board UI, and *only then* wires up `endTurnButton`'s listener. A `mulliganComplete` guard on `OnCardClicked()` and `OnEndTurnClicked()` makes both early-return no-ops until mulligan is confirmed, preventing any normal gameplay action before the phase ends. **Not permanent** — delete/rename once real gameplay loop exists. |
| `CardView.cs` | `Scripts/UI/` | Displays one card. **New this session:** now has two setup modes sharing a private `WriteCardText()` helper — `SetCard(CardData, Action<CardData>)` (existing "click to play" behavior, unchanged) and `SetCardForMulligan(CardData, Action<CardData>)` (new: clicking toggles a private `isSelectedForMulligan` flag, tints a new `cardBackground` Image field between `normalColor`/`selectedForMulliganColor`, and invokes a toggle callback instead of a play callback). |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — generic, reused for both hands and unrelated to the new mulligan-mode rendering (which `EffectTester` does directly via `Instantiate` + `SetCardForMulligan`, since it needs toggle semantics `HandDisplay` doesn't have). |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with `Deck`/`Hand`. `Shuffle()` (Fisher-Yates), `DrawOpeningHand(int count = 5)`, `DrawCard()`, `AddCardToHand(CardData)` (bypasses the deck — used for The Coin). **New this session:** `MulliganCard(CardData card)` — removes the card from `Hand`, returns it to `Deck`, reshuffles, draws a replacement via the existing `DrawCard()`. Reuses existing methods entirely rather than writing new draw logic. `PlayCard(CardData, GameContext, Target)` unchanged. |
| `MinionView.cs` | `Scripts/UI/` | Display-only, no click/Button. |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard(List<Minion>)` — generic, reused for both boards. |
| `AIController.cs` | `Scripts/AI/` | Wraps a `PlayerHand` (the AI's) + `GameContext`/`Board`. **New this session:** `PerformMulligan(int mulliganThreshold = 4)` — snapshots the AI's hand, mulligans any card costing `>= mulliganThreshold`, keeps the rest. Simple "keep cheap, ditch expensive" heuristic, easy to tune via the threshold param. `TakeTurn()` unchanged from last session (targeting-aware via `card.targetsSelf`). |

## Test Assets Created
- `TestCard_Wisp` (1 mana, 1/1 Minion), `TestCard_Goblin` (2 mana, 2/2 Minion), `TestCard_RiverCroc` "River Crocodile" (3 mana, 2/3 Minion), `TestCard_Fireball` (4 mana, Spell, `targetsSelf = false`, → `Effect_Deal3Damage`), `TestCard_Boulderfist` (5 mana, 4/4 Minion) — the 5-card pool, `Copies Per Card = 2` → 10-card deck each.
- `TestCard_Coin` "The Coin" (0 mana, Spell, `targetsSelf = true`, → `Effect_GainMana1`) — deliberately NOT in `Card Pool`, only granted via `EffectTester`'s dedicated `Coin Card` field.
- `Effect_Deal3Damage` (`DealDamageEffect`, damage = 3), `Effect_GainMana1` (`GainManaEffect`, manaAmount = 1).

## Scene/Editor Setup (new this session)
- `MulliganPanel` — duplicate of `HandPanel`, currently occupying the same screen position (acceptable since it's hidden after confirm). Populated at runtime by `EffectTester`, not pre-populated in the scene.
- `ConfirmMulliganButton` — new Button, labeled "Keep Hand", positioned near `EndTurnButton`.
- `CardView` prefab — added a `Card Background` field wired to its own root `Image` component (dragged the `CardView` GameObject itself onto its own new field, in Prefab Edit Mode).
- `EffectTester`'s new `Mulligan UI` Inspector section wired: `Mulligan Panel` → `MulliganPanel`, `Card View Prefab` → same `CardView` prefab asset used by `HandDisplayController`, `Confirm Mulligan Button` → `ConfirmMulliganButton`.

## Verified Working
- **Board visuals** (session 9), **AI turn logic** (session 10), **opponent board visualization** (session 11), **shuffled decks + 5-card draw + opponent hand display** (session 12), **asymmetric 3-vs-4 opening hands + The Coin + a genuine multi-card AI turn** (session 13) — all previously confirmed, unchanged this session.
- **AI mulligan (this session)**: Confirmed via Console — AI drew its 4+Coin hand, then `PerformMulligan()` correctly mulliganed both cards costing ≥4 (Boulderfist, Fireball), keeping the cheaper cards and The Coin, with each mulligan producing a genuine reshuffled replacement draw (in one case redrawing a second copy of the same card it had just mulliganed away — correct behavior, since the deck holds 2 copies of everything).
- **Human mulligan, full interaction (this session)**: Confirmed visually and via Console — 3-card hand appeared in `MulliganPanel`, clicking a card (Boulderfist) visibly dimmed it, clicking again un-dimmed it, clicking **Keep Hand** correctly swapped only the marked card(s) (Boulderfist → Wisp) while leaving unmarked cards untouched, the mulligan panel became inactive in the Hierarchy, and both players' normal hands (human's post-mulligan 3, AI's post-mulligan 5) rendered correctly afterward.
- This is the first session where **every planned "Next Steps" item going in was fully completed and verified** — no partial/deferred work.

## Current Blocker / Last Thing Worked On
None — session ended on the mulligan system being fully built and verified for both players.

**Not yet re-tested / worth a quick check next session:**
- Clicking **End Turn** after confirming mulligan wasn't explicitly re-confirmed this session (the screenshot showed the button present and presumably functional, but the actual click-through wasn't shown in the last exchange) — worth one quick playthrough to confirm turn-taking resumes exactly as it did before the mulligan system was added, with no regression from the new `mulliganComplete` guard logic.

**Cosmetic, still low priority:**
- The known `BoardPanel`/`OpponentBoardPanel` text-clipping issue (minion name cut off at the left edge) is still unaddressed.
- Board panel vertical spacing could still use a bit more separation for clarity.
- `MulliganPanel` currently sits in the exact same screen position as `HandPanel` — functionally fine since it's hidden after confirm, but could be visually distinguished later if desired.

**Also worth double-checking next session:** confirm `TestCard_Goblin`'s Mana Cost is holding at `2` (flip-flopped a couple of times across recent sessions — hasn't been re-checked in a couple sessions now).

## Lessons Learned / Gotchas (useful to remember)
**Assembly definitions (.asmdef)**
- Circular references are rejected — keep dependency direction one-way.
- An asmdef's real identity is its **Name** field (Inspector), not its filename.
- `CS0246` errors can mean either a missing asmdef reference OR a missing `using` statement.
- Cross-assembly type usage needs BOTH the `using` directive AND an explicit **Assembly Definition Reference**.

**Unity Editor / UI basics**
- Script filename must exactly match its class name.
- Scene changes aren't saved until `Ctrl+S`.
- For stretch-anchored panels, reposition via Rect Transform `Pos Y`/`Left`/`Right`/`Height`, not free dragging.
- A circular gizmo appearing unexpectedly in Scene view is likely just the `Global Light 2D` gizmo — editor-only, not a bug.
- Editing a runtime `(Clone)` GameObject in Play mode is temporary — use Prefab Edit Mode for permanent changes.
- An empty/unassigned Inspector field for a listener-style hookup fails **silently**.
- Editing a TMP label's `Text Input` box requires clicking in, changing the text, then clicking away to commit.
- When reusing an existing prefab reference for a duplicated controller, check the *original* controller's Inspector for exactly which asset is assigned rather than re-dragging from memory.
- `CS0102` ("type already contains a definition for X") after a manual paste-in almost always means a field/header block got accidentally duplicated during copy-paste — check for a repeated pair near the reported line number first. This is why full-file pastes are now standard (see Working Preferences) — it removes this whole class of manual-merge mistake.
- **New this session**: when a script field expecting a specific Component type (e.g. an `Image` field) needs to reference a component that lives on the *same* GameObject the script is attached to, you can drag that same GameObject onto its own field — Unity finds the matching component automatically, no need to hunt for a separate child object.

**Core architecture principle**
- Keep `Core` types generic and unaware of `CardData`.
- AI-ness isn't a flag on `Player` — `AIController` is simply told which `PlayerHand` it controls at construction time.
- When a display/rendering component is already written generically, the cheapest way to support "two of something" is usually a second *instance* of the same component, not a code change to juggle two lists.
- A callback-based click handler using the null-conditional operator (`onClicked?.Invoke(card)`) gives "free" read-only-mode support.
- `ScriptableObject`-based cards support "multiple copies in a deck" for free via repeated list references.
- Use an in-place Fisher-Yates shuffle for randomizing a `List<T>`.
- A boolean flag on the data itself (`CardData.targetsSelf`) is the right way to encode "who does this affect," rather than special-casing by name/type in game logic.
- A temporary/this-turn-only stat bonus needs no explicit expiry/cleanup code if it modifies a field some other system already resets unconditionally each cycle.
- **New this session**: when a UI component needs two distinct interaction modes that share most of their setup (e.g. "play mode" vs "mulligan-select mode" for the same card visual), pull the shared parts into a private helper (`WriteCardText()`) and give each mode its own public entry point (`SetCard()` vs `SetCardForMulligan()`) rather than adding a mode-flag parameter to one method — keeps each mode's intent explicit at the call site and avoids one method accumulating branchy conditional logic.
- **New this session**: a "confirm/complete" boolean guard flag (like `mulliganComplete`) checked at the top of other interactive handlers (`OnCardClicked()`, `OnEndTurnClicked()`) is a simple, effective way to gate an entire phase of interaction (mulligan) without needing a more complex state machine — appropriate for a small number of phases, though a real state machine would be worth it if more phases get added later (e.g. a full turn-phase system).

**Git / GitHub / environment**
- GitHub requires `gh auth login` or a PAT (repo scope only) for HTTPS git auth.
- `.gitignore` should exclude `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`.
- Unity's crash-recovery prompt (`Assets/_Recovery/`) is safe to accept; delete the folder after.
- .NET SDK (install via Microsoft's apt feed, not Ubuntu's default repo) is only needed for VS Code's C# IntelliSense/debugging.

## Next Steps (in order)
1. Quick re-confirm that End Turn / normal turn-taking still works correctly after a mulligan completes (no regression check).
2. Re-confirm `TestCard_Goblin`'s Mana Cost is `2`.
3. Adjust vertical spacing between the board panels; investigate the minion-text-clipping issue.
4. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
5. Consider upgrading click-to-play to real drag-and-drop, if desired.
6. Longer-term: as more interactive phases get added (e.g. full turn-phase management), consider whether the `mulliganComplete`-style boolean guard pattern should evolve into a proper state machine.

## Git Habits Being Followed
- Simple commit template: `git add .` / `git commit -m "short one-line summary"` / `git push`
- Push regularly to `github.com/craig-middleton/hearthClone`
