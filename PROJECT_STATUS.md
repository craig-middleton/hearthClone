# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.

_Last updated: 2026-07-12 (session 4, ended mid-task on Card UI — Scene view/Rect Transform work was frustrating this session)_

## How to use this file
Paste the contents of this file at the start of any new Claude chat to get instant context on the project. Update it at the end of each working session (ask Claude to update it, or do it yourself) so it never goes stale.

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

## Code Written So Far

| File | Location | Purpose |
|---|---|---|
| `CardData.cs` | `Scripts/Cards/` | ScriptableObject: card identity, stats, mana cost, and a link to its `onPlayEffect` |
| `CardEffect.cs` | `Scripts/Effects/` | Abstract base class all effects inherit from; defines `Execute(GameContext, Target)` |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Concrete effect; deals damage to a Target; logs remaining health via `Target.GetCurrentHealth()` |
| `Minion.cs` | `Scripts/Core/` | Runtime instance of a minion on the board (name, attack, health). Deliberately generic — knows nothing about `CardData`, to avoid a circular assembly reference. `Cards` layer will provide a way to construct one from a card asset. |
| `Player.cs` | `Scripts/Core/` | Real game state: health, mana (current/max), list of board minions. No hand/deck yet (that needs `CardData`, which `Core` can't reference). |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent(player)` helper. |
| `GameContext.cs` | `Scripts/Core/` | Now holds a real `Board` reference (was an empty placeholder). |
| `Target.cs` | `Scripts/Core/` | Now points to either a real `Player` or `Minion` (was a fake standalone health tracker). Provides `TakeDamage()` and `GetCurrentHealth()`. |
| `EffectTester.cs` | `Scripts/UI/` | Temporary MonoBehaviour for manual testing; constructs `Player`/`Board`/`GameContext`/`Target`/`PlayerHand`/`TurnManager`, and (once wiring is complete) feeds the hand to `HandDisplay` for rendering. Moved here from `Scripts/Cards/` this session since it needs visibility into both `Cards` and `UI` layers. **Not permanent** — delete once real gameplay loop exists. |
| `CardView.cs` | `Scripts/UI/` | Thin display component for one card — takes a `CardData` via `SetCard()` and writes name/cost/stats onto TMP Text fields. Uses `TMP_Text` (TextMeshPro), not legacy `Text` (project uses Unity 6.5, which favors TMP). Attached to the `CardView` prefab. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with a `Deck`/`Hand` of `CardData`. `DrawCard()` moves a card from deck to hand (index 0, no shuffling yet). `PlayCard()` validates the card is in hand and mana is sufficient, deducts mana, removes from hand, summons a `Minion` if applicable, and fires the card's `onPlayEffect` if a target is given. Returns bool success/failure rather than throwing. |
| `TurnManager.cs` | `Scripts/Core/` | Owns turn order and mana progression. `StartGame()` sets turn 1, Player One first. `EndTurn()` swaps to opponent via `Board.GetOpponent()`, increments turn number, refills mana. Mana ramps +1 per turn for that player, capped at 10, refilling to max each time. Lives in `Core` since it only needs `Player`/`Board` — no card dependency. |

## Test Assets Created
- `TestCard_Fireball.asset` — a Spell card, 4 mana, linked to `Effect_Deal3Damage`
- `Effect_Deal3Damage.asset` — a `DealDamageEffect` instance, damage = 3

## Verified Working
Confirmed end-to-end, four rounds now, most recently:
`TurnManager.StartGame()` correctly sets Player One's turn with 1/1 mana. `PlayerHand.DrawCard()` still works. `PlayerHand.PlayCard()` correctly **rejected** playing `TestCard_Fireball` (4 mana cost) against 1 available mana, logging the expected "not enough mana" message — confirming the mana-gate guard clause works as intended. `TurnManager.EndTurn()` correctly advanced to Turn 2, Player Two, 1/1 mana. Full Console output matched predictions exactly.

## Current Blocker / Last Thing Worked On
**Mid-task, not yet complete.** Card UI build-out, in progress across two sessions now. Confirmed done so far:
- `EffectTester.cs` moved to `Scripts/UI/`, namespace updated — compiles clean.
- `CardView.cs` created in `Scripts/UI/`, using TextMeshPro (`TMP_Text`). Required adding a reference to the `Unity.TextMeshPro` assembly on `UI.asmdef` (Assembly Definition References → search/add `Unity.TextMeshPro`) — this was NOT a missing `using` statement, the asmdef itself needed the package reference. Confirmed working, no compile errors.
- In the Editor: `GameCanvas` created; `CardView` panel created and sized (160x220, centered anchor); three TMP Text children created (`NameText`, `CostText`, `StatsText`), positioned without overlap (NameText: Pos 0/80, Width 140, Height 30; CostText: Pos -55/90, Width 40, Height 30; StatsText: Pos 0/-80, Width 140, Height 30). Auto Size enabled on the text components to avoid wrapping issues.
- `CardView` script component attached to the `CardView` panel GameObject; all three Text fields (Name/Cost/Stats) assigned in the Inspector.
- Scene saved.

**Not yet done (pick up here next session):**
1. Drag `CardView` GameObject into `Assets/_Project/Prefabs/Cards/` to make it a prefab, then delete the scene instance.
2. Create `HandPanel` (UI Panel, bottom-stretch anchor, Height ~250, small Pos Y offset from bottom edge).
3. Add **Horizontal Layout Group** to `HandPanel` (Child Force Expand Width/Height off, Spacing ~20).
4. Create `HandDisplay.cs` in `Scripts/UI/` — **code has been written/shared but the file has not actually been created yet.**
5. Create `HandDisplayController` empty GameObject, attach `HandDisplay`, assign `CardView` prefab + `HandPanel`.
6. Assign `HandDisplayController` into `EffectTester`'s **Hand Display** field.
7. Save, Play, confirm a card panel renders on screen showing "Fireball" / "4" / blank stats — **this would be the first real visual milestone of the project; not yet achieved.**

## Immediate Next Steps (pick up here)
Continue from step 1 in the list directly above ("Not yet done"). All code for `HandDisplay.cs` was already written in a previous chat — if starting a brand new chat, ask Claude to re-provide the `HandDisplay.cs` code (it's a straightforward component: instantiates a `CardView` prefab per card in a `List<CardData>`, parented under a hand panel transform).

## Lessons Learned / Gotchas (useful to remember)
- **Script filename must exactly match the class name** for Unity to allow attaching it as a Component ("script needs to derive from MonoBehaviour" error can actually mean a filename/class mismatch, not a real inheritance problem).
- **Asmdef "Name" field vs filename**: an `.asmdef` file's actual assembly identity is set by the **Name** field in its Inspector, not just the filename — check this if references seem to silently fail.
- **CS0246 "type or namespace not found" has two possible causes**: (1) the `.asmdef` isn't referencing the right assembly, or (2) a missing `using` statement in the file itself even when the asmdef reference is correct. Check both.
- **Circular assembly references are rejected by Unity** — keep dependency direction one-way (see Architecture Decisions above). If a new script needs types from two assemblies that don't currently depend on each other, put the script in whichever assembly already sits "above" both in the dependency chain, rather than adding a new cross-reference.
- **GitHub no longer supports password auth for git over HTTPS** — use `gh auth login` (stores credentials for the terminal) or a Personal Access Token as the password. The PAT only needs the `repo` scope.
- **New repo via Unity Hub's GitHub option** required generating a classic PAT with `repo` scope (all scopes are unchecked by default — must be explicitly selected).
- **.NET SDK is separate from Unity's own Mono/compilation runtime** — needed only so VS Code's C# Dev Kit can do IntelliSense/debugging; install via Microsoft's apt feed (`dotnet-sdk-8.0`), not Ubuntu's default repo.
- **Git doesn't track empty folders** — expected, not a bug; folders show up in git once files are added to them.
- **`.slnx` solution files** (newer XML-based alternative to `.sln`) are auto-generated by the tooling for VS Code IntelliSense/debugging — like `.sln`/`.csproj`, they shouldn't be committed. Add `*.slnx` to `.gitignore` if missing and `git rm --cached` it if already tracked.
- **Unity may prompt to preserve scene backups** (e.g. into `Assets/_Recovery/`) if the Editor didn't close cleanly last session (crash, force-quit, laptop sleep interrupting it). Safe to accept — it just copies files, doesn't overwrite anything. Delete the folder afterward if unneeded, and add `_Recovery/` to `.gitignore` so it doesn't get committed.
- **Keep `Core` types generic, not `CardData`-aware** — e.g. `Minion`/`Player` in `Core` can't hold `List<CardData>` directly since `Core` can't reference `Cards`. Any type that needs both "real game state" and "card asset data" belongs in the `Cards` layer instead, as a wrapper around the `Core` type.
- **Scene changes (new GameObjects, component assignments) must be explicitly saved with `Ctrl+S`** — they're not automatically written to the `.unity` scene file just by existing in the Hierarchy. If Unity closes uncleanly (or a different scene gets opened) before saving, GameObject setup done that session can be lost even though script/asset changes (which live in their own files) are safe. Get in the habit of `Ctrl+S` after any Hierarchy change, not just after script edits.
- **Rect Transform anchor changes need Alt held to also reposition**: clicking an anchor preset (e.g. "center") changes the anchor reference point but does NOT recalculate Pos X/Y to match unless you hold **Alt** while clicking. Without Alt, the object can appear to jump to a nonsensical position even though the anchor itself is "correct." After changing an anchor, retype Pos X/Y explicitly to be sure.
- **New UI Text objects default to a stretch anchor** (filling the parent) — Width/Height fields won't appear in the Inspector until the anchor is changed to a fixed point (e.g. center/middle). If you see Left/Right/Top/Bottom instead of Width/Height, that's the tell.
- **TextMeshPro requires an explicit asmdef reference**, separate from any `using TMPro;` statement — add `Unity.TextMeshPro` under Assembly Definition References on whichever asmdef needs it (e.g. `UI.asmdef`), or you'll get `CS0246: TMPro not found` even with the correct `using` line.
- **TMP Auto Size doesn't prevent wrapping if the placeholder text itself is too long for the box** — e.g. default "New Text" won't fit in a narrow box (like a 40-wide cost label) even at minimum font size, and will wrap/cascade. This is usually a non-issue once real (short) data replaces the placeholder text, not a real layout bug.
- **Scene view can lose track of your work area** if the camera was originally framed elsewhere (e.g. around Main Camera/Global Light 2D) — press **F** with the relevant object selected (e.g. `GameCanvas`) to snap/zoom the Scene view to frame it.
- **Always select the specific GameObject you mean to edit** — clicking a parent (e.g. `GameCanvas`) when you meant to edit a child (e.g. `NameText`) will show a different, larger Rect Transform in the Inspector; double-check the Inspector header shows the expected object name before typing position values.

## Next Steps (after the UI is confirmed working — see "Immediate Next Steps" above for the current task)
1. Wire up basic drag-and-drop or click-to-play from hand to board (visual representation of `PlayerHand.PlayCard()`).
2. Build basic AI opponent logic.
3. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
4. Add real deck shuffling to `PlayerHand.DrawCard()` (currently just takes index 0).

## Git Habits Being Followed
- Commit at each logical checkpoint (not just end-of-day)
- Descriptive commit messages
- Push regularly to `github.com/craig-middleton/hearthClone`
