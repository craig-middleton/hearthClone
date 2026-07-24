# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.

_Last updated: 2026-07-24 (session 10 — basic AI opponent turn logic implemented and verified working end-to-end)_

## How to use this file
Paste the contents of this file at the start of any new Claude chat to get instant context on the project. Update it at the end of each working session (ask Claude to update it, or do it yourself) so it never goes stale.

**Source snapshots**: at the end of sessions where Claude has seen full current file contents, a dated folder is saved under `snapshots/sessionN-YYYY-MM-DD/` in this repo, containing the actual `.cs` source (not just descriptions) for whichever files were pasted/uploaded that session, plus a copy of this status file and a README noting what was/wasn't captured. These are point-in-time references only — always treat your live `Assets/` files as the source of truth, not the snapshot.

## Project Goal
A Hearthstone-style card game built in Unity, single-player vs AI, using C# with a data-driven card/effect system (ScriptableObjects). Developed on Ubuntu using Unity + VS Code, version controlled via Git/GitHub (with Git LFS enabled for art/audio).

**Unity version: 6.5 (60000.5.2f1).** Use TextMeshPro (TMP) for all UI text, not legacy `UnityEngine.UI.Text` — legacy Text is largely hidden/deprecated in the Unity 6.x UI menus in favor of TMP.

## Tooling Setup (Done)
- Unity Hub + Unity LTS installed via AppImage
- VS Code with C# Dev Kit + Unity extension
- .NET SDK installed (required for VS Code C# debugging, separate from Unity's own runtime)
- Git + GitHub connected via Unity Hub's built-in integration, authenticated via `gh auth login`
- Git LFS enabled for binary assets (art, audio)
- Repo: `github.com/craig-middleton/hearthClone`

## Architecture Decisions
- **Folder structure** under `Assets/_Project/`: Scripts, ScriptableObjects, Prefabs, Art, Scenes
- **Card/effect model**: data-driven via ScriptableObjects rather than hardcoded per-card classes — cards reference reusable `CardEffect` assets (e.g. "Deal 3 Damage") instead of each card having bespoke code. Keeps ~80% of future cards as pure data, no new code needed.
- **Assembly Definitions** (`.asmdef`) enforce one-way dependency flow to keep architecture clean:
  ```
  Core (no dependencies)
    ↑
  Cards (depends on Core, Effects)
    ↑
  Effects (depends on Core only)
    ↑
  AI, UI (depend on Core, Cards, Effects)
  ```
  Important: `Effects` must NOT reference `Cards` (would create a circular dependency).
  - `AI.asmdef` now exists (created this session) with references to `Core`, `Cards`, `Effects`.
  - `UI.asmdef` now references `AI.asmdef` too, since `EffectTester` calls into `AIController`.

## Code Written So Far

| File | Location | Purpose |
|---|---|---|
| `CardData.cs` | `Scripts/Cards/` | ScriptableObject: card identity, stats, mana cost, and a link to its `onPlayEffect` |
| `CardEffect.cs` | `Scripts/Effects/` | Abstract base class all effects inherit from; defines `Execute(GameContext, Target)` |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Concrete effect; deals damage to a Target; logs remaining health via `Target.GetCurrentHealth()` |
| `Minion.cs` | `Scripts/Core/` | Runtime instance of a minion on the board (name, attack, health). Deliberately generic — knows nothing about `CardData`, to avoid a circular assembly reference. |
| `Player.cs` | `Scripts/Core/` | Real game state: health, mana (current/max), list of board minions. No `IsAI` flag — AI-ness is tracked externally by which `Player`/`PlayerHand` the `AIController` wraps, not on `Player` itself. |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent(player)` helper. |
| `GameContext.cs` | `Scripts/Core/` | Holds a real `Board` reference. |
| `Target.cs` | `Scripts/Core/` | Points to either a real `Player` or `Minion`. Provides `TakeDamage()` and `GetCurrentHealth()`. |
| `TurnManager.cs` | `Scripts/Core/` | Owns turn order and mana progression. `StartGame()` sets turn 1, Player One first. `EndTurn()` swaps `CurrentPlayer` via `Board.GetOpponent()`, increments turn number, refills mana (+1/turn, capped at 10). |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper MonoBehaviour. Constructs `Player`/`Board`/`GameContext`/`TurnManager`/both `PlayerHand`s/`AIController` in `Start()`. `OnCardClicked(CardData)` handles the human's card plays. **New this session:** `OnEndTurnClicked()` wired to a public `endTurnButton` field — ends the human's turn, checks if `TurnManager.CurrentPlayer` is now Player Two, and if so runs `aiController.TakeTurn()` then ends turn again automatically to hand control back to the human in one click. **Not permanent** — delete/rename once real gameplay loop exists. |
| `CardView.cs` | `Scripts/UI/` | Thin display component for one card — writes name/cost/stats onto TMP fields, wires a `Button.onClick` to a click callback. Attached to `CardView` prefab. |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — clears and respawns one `CardView` per hand card. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with a `Deck`/`Hand` of `CardData`. `DrawCard()` takes index 0 (no shuffle yet). `PlayCard(CardData, GameContext, Target)` validates hand membership + mana, deducts mana, summons a `Minion` if applicable, fires `onPlayEffect` if a target given, returns bool. |
| `MinionView.cs` | `Scripts/UI/` | Display-only — `SetMinion(Minion)` writes name/attack-health onto TMP fields. No click/Button (board minions aren't interactive yet). |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard(List<Minion>)` — clears and respawns one `MinionView` per board minion. **Currently only ever called with `playerOne.BoardMinions`** — Player Two's board has no visual representation yet (see Next Steps). |
| `AIController.cs` | `Scripts/AI/` (**new this session**) | Wraps a `PlayerHand` (the AI's) plus `GameContext`/`Board`. `TakeTurn()` loops: scan hand for any card whose `manaCost <= CurrentMana`, play the first one found via `PlayerHand.PlayCard()`, repeat until a full pass plays nothing. For cards with an `onPlayEffect` (e.g. Fireball), builds a `Target` pointing at the opponent player's face — no board-state evaluation yet, so it always burns face. Lives in its own `AI` assembly per the architecture diagram. |

## Test Assets Created
- `TestCard_Fireball.asset` — a Spell card, 4 mana, linked to `Effect_Deal3Damage`
- `Effect_Deal3Damage.asset` — a `DealDamageEffect` instance, damage = 3
- `TestCard_Goblin.asset` — a Minion card, 2 mana (**still needs confirming** — was temporarily 1 for earlier testing), Attack 2, Health 2, no `onPlayEffect`

## Verified Working
- **Board visuals** (prior session): click-to-play chain confirmed end-to-end with visual + Console confirmation.
- **AI opponent turn logic (this session)**: Full turn cycle confirmed via Console —
  ```
  Turn 2: Player Two's turn. Mana: 1/1
  --- Player Two (AI) is taking its turn ---
  Player Two played Goblin. Mana remaining: 0
  Goblin summoned to Player Two's board.
  --- Player Two (AI) ends its turn ---
  Turn 3: Player One's turn. Mana: 2/2
  ```
  AI correctly skipped the unaffordable Fireball (4 mana, only had 1) and played the affordable Goblin instead. Control correctly returned to the human player automatically after the AI's turn resolved. One End Turn button click now fully resolves an AI turn.

## Current Blocker / Last Thing Worked On
None — **AI opponent turn logic is complete and verified** for the "play first affordable card" strategy. Session ended here to take a break.

**Not yet tested / worth checking next session:**
1. **Multi-card AI turns** — only tested with enough mana for one card so far. Confirm the AI correctly chains multiple plays in a single turn once mana is higher (e.g. plays Goblin, has mana left over, plays Fireball too), rather than stopping after one card.
2. **Fireball's targeting in practice** — `AIController` builds `Target` pointing at the opponent's face for any card with an `onPlayEffect`, but this path hasn't actually been exercised yet in a real playtest (AI didn't have enough mana for Fireball in the test run). Confirm Player One's health actually drops when the AI eventually plays it.
3. **AI's board minions have no visual representation** — `BoardDisplay.RenderBoard()` is only ever called with `playerOne.BoardMinions` in `EffectTester`. Player Two's Goblin summoned successfully in Core/game-state terms but isn't shown anywhere on screen. Needs either a second `BoardDisplay`/`BoardPanel` for the opponent's side, or a combined two-row board view.

**Also confirm before next session**: `TestCard_Goblin`'s Mana Cost should be reset to `2` (was temporarily `1` for testing with only 1 starting mana). Still outstanding from before this session.

## Lessons Learned / Gotchas (useful to remember)
**Assembly definitions (.asmdef)**
- Circular references are rejected by Unity — keep dependency direction one-way (`Cards → Effects → Core`; `Core` and `Effects` never reference `Cards`). If a script needs types from two assemblies with no relationship, it belongs in whichever assembly sits "above" both.
- An asmdef's real identity is its **Name** field (Inspector), not its filename — check this if references silently fail.
- `CS0246` errors can mean either a missing asmdef reference OR a missing `using` statement — check both. Unity packages (e.g. TextMeshPro/`Unity.TextMeshPro`) need an explicit asmdef reference too, separate from any `using` line.
- **New this session**: when a script in one assembly (e.g. `UI`) calls into a type from another assembly (e.g. `AI`), BOTH the `using` directive AND an explicit **Assembly Definition Reference** on the calling assembly's `.asmdef` are needed — a missing asmdef reference gives the same `CS0246` error as a missing `using`, so check both whenever a cross-assembly type isn't resolving. Fix via: select the `.asmdef` asset → Inspector → Assembly Definition References → `+` → add the missing one → **Apply**.

**Unity Editor / UI basics**
- Script filename must exactly match its class name, or Unity won't allow attaching it as a Component.
- Scene changes (new GameObjects, component assignments) aren't saved until `Ctrl+S` — get in the habit of saving after Hierarchy changes, not just script edits.
- Always confirm the Inspector is showing the GameObject you actually mean to edit (easy to accidentally edit a parent instead of the intended child).
- Rect Transform anchor presets: hold **Alt** while clicking a preset to also reposition the object — otherwise the anchor changes but the position doesn't recalculate, making things jump oddly. New UI Text/Panels default to a stretch anchor (Width/Height fields only appear after switching to a fixed-point anchor like center).
- TMP Auto Size won't stop wrapping if placeholder text itself is too long for the box — usually resolves once real (shorter) data replaces it, not a real bug.
- Overlapping semi-transparent UI panels of similar default grey color can visually merge into what looks like one oversized element. If something looks wrongly sized, try setting a suspect parent panel's alpha to 0 first to rule out a layering illusion before assuming a real layout bug.
- If the Scene view camera seems "lost," select the relevant object and press **F** to frame it.
- **Editing a runtime `(Clone)` GameObject in the Hierarchy during Play mode is temporary** — changes vanish when Play mode stops. To make a permanent change to a prefab, stop Play mode, double-click the prefab asset to enter Prefab Edit Mode, make the change there, exit/save.
- When a script field expects a specific Component type (e.g. a `Button` field) but the target GameObject doesn't have that component yet, add the component first, then drag the **GameObject** itself onto the field — Unity finds the right component on it automatically. A `NullReferenceException` on a line touching a component field usually means the field was never assigned in the Inspector.
- **New this session**: an empty/unassigned Inspector field for a listener-style hookup (e.g. a `Button` field that gets `.onClick.AddListener(...)` in `Start()`) fails **silently** — no error, the button just does nothing when clicked. If a wired button appears inert, check the field isn't still `None` before suspecting the underlying logic.
- **New this session**: editing a TMP label's `Text Input` box in the Inspector requires clicking into the box, changing the text, then clicking away elsewhere in the Inspector to commit the change — the Hierarchy GameObject's name does NOT reflect the label text (they're independent), so don't use the Hierarchy name as a check for whether the edit "took."

**Core architecture principle**
- Keep `Core` types generic and unaware of `CardData` — anything needing both real game state and card asset data belongs in the `Cards` layer as a wrapper.
- **New this session**: AI-ness isn't a flag on `Player` — `AIController` is simply told which `PlayerHand` it controls at construction time, keeping `Core.Player` free of any AI-specific concept.

**Git / GitHub / environment**
- GitHub requires `gh auth login` or a PAT (repo scope only) for HTTPS git auth — plain passwords no longer work.
- `.gitignore` should exclude auto-generated files: `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`. Git doesn't track empty folders (expected, not a bug).
- Unity's crash-recovery prompt (`Assets/_Recovery/`) is safe to accept; delete the folder after and gitignore it if unneeded.
- .NET SDK (install via Microsoft's apt feed, not Ubuntu's default repo) is only needed for VS Code's C# IntelliSense/debugging — separate from Unity's own compiler.

## Next Steps (in order)
1. Playtest a few more turns to confirm multi-card AI turns and Fireball's face-damage targeting actually work as expected (see "Not yet tested" above).
2. Give the AI's board minions a visual home — second `BoardPanel`/`BoardDisplay`, or a combined two-row board view.
3. Optional: investigate/fix the `BoardPanel` positioning issue (minion rendering partly off-screen left) — check Horizontal Layout Group padding/alignment on `BoardPanel`.
4. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
5. Add real deck shuffling to `PlayerHand.DrawCard()` (currently just takes index 0).
6. Consider upgrading click-to-play to real drag-and-drop, if desired (click-to-play works fine as an interim/MVP interaction model).

## Git Habits Being Followed
- Commit at each logical checkpoint (not just end-of-day)
- Descriptive commit messages
- Push regularly to `github.com/craig-middleton/hearthClone`

---

## Commit message for this session

```
Add basic AI opponent turn logic

- New AIController.cs (Scripts/AI/) with its own AI.asmdef
  (references Core, Cards, Effects)
- TakeTurn() plays first-affordable-card repeatedly until no
  more plays are possible; targets opponent's face for any
  card with an onPlayEffect
- EffectTester: wired End Turn button -> TurnManager.EndTurn(),
  auto-runs AI turn when CurrentPlayer swaps to Player Two,
  then ends turn again to hand control back to the human
- UI.asmdef updated to reference AI.asmdef

Verified end-to-end: AI correctly skips unaffordable cards,
plays what it can afford, and turn control returns to the
human automatically. Console-confirmed full turn cycle.
```
