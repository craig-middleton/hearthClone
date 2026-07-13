# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.

_Last updated: 2026-07-13 (session 5 — first visual milestone achieved)_

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
| `CardView.cs` | `Scripts/UI/` | Thin display component for one card — takes a `CardData` via `SetCard()` and writes name/cost/stats onto TMP Text fields. Uses `TMP_Text` (TextMeshPro), not legacy `Text` (project uses Unity 6.5, which favors TMP). Attached to the `CardView` prefab (`Prefabs/Cards/CardView.prefab`). |
| `HandDisplay.cs` | `Scripts/UI/` | Manages a collection of card visuals — `RenderHand(List<CardData>)` clears any previously spawned cards under `handPanel`, then instantiates one `CardView` prefab per card and populates it. Attached to `HandDisplayController` GameObject in the scene. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with a `Deck`/`Hand` of `CardData`. `DrawCard()` moves a card from deck to hand (index 0, no shuffling yet). `PlayCard()` validates the card is in hand and mana is sufficient, deducts mana, removes from hand, summons a `Minion` if applicable, and fires the card's `onPlayEffect` if a target is given. Returns bool success/failure rather than throwing. |
| `TurnManager.cs` | `Scripts/Core/` | Owns turn order and mana progression. `StartGame()` sets turn 1, Player One first. `EndTurn()` swaps to opponent via `Board.GetOpponent()`, increments turn number, refills mana. Mana ramps +1 per turn for that player, capped at 10, refilling to max each time. Lives in `Core` since it only needs `Player`/`Board` — no card dependency. |

## Test Assets Created
- `TestCard_Fireball.asset` — a Spell card, 4 mana, linked to `Effect_Deal3Damage`
- `Effect_Deal3Damage.asset` — a `DealDamageEffect` instance, damage = 3

## Verified Working
Confirmed end-to-end, five rounds now, most recently: **first visual milestone** — `TestCard_Fireball` renders as an actual card panel on screen (name "Fireball", cost "4") via `CardView`/`HandDisplay`/`HandPanel`, alongside the existing Console-verified logic (draw, mana-gate rejection at 1/4 mana, turn advance to Player Two). Screenshot confirms visual + log output both correct simultaneously.

## Current Blocker / Last Thing Worked On
None. **First visual milestone achieved this session**: `CardView` (prefab, TMP-based), `HandDisplay`, `HandPanel` (bottom-stretch anchor + Horizontal Layout Group), and `HandDisplayController` all built and wired together. Confirmed in Play mode: a card panel showing "Fireball" (name) and "4" (cost) renders on screen at the bottom of the Game view, alongside the existing Console log output (draw, mana-gate rejection, turn advance) all still working correctly. This is the first real graphical output of the project — everything before this was Console-only.

**Known cosmetic issue, not yet fixed**: the rendered card panel appeared to span nearly the full width of the screen rather than the expected fixed 160-wide card. Likely cause: the Horizontal Layout Group's "Child Force Expand → Width" may not have been unticked, or the `CardView` prefab's own Width isn't holding at 160 once instantiated under the layout group. Worth checking next session if a tidy card-sized (not full-width) visual is wanted.

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

## Next Steps (in order)
1. **Cosmetic fix**: investigate why the rendered card panel spans nearly full screen width instead of a fixed 160-wide card — check Horizontal Layout Group's Child Force Expand Width setting on `HandPanel`, and confirm `CardView` prefab's own Width holds at 160 when instantiated.
2. Wire up basic drag-and-drop or click-to-play from hand to board (visual representation of `PlayerHand.PlayCard()`).
3. Build basic AI opponent logic.
4. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
5. Add real deck shuffling to `PlayerHand.DrawCard()` (currently just takes index 0).

## Git Habits Being Followed
- Commit at each logical checkpoint (not just end-of-day)
- Descriptive commit messages
- Push regularly to `github.com/craig-middleton/hearthClone`
