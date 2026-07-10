# HearthstoneClone - Project Status

_Last updated: 2026-07-10 (session 2)_

## How to use this file
Paste the contents of this file at the start of any new Claude chat to get instant context on the project. Update it at the end of each working session (ask Claude to update it, or do it yourself) so it never goes stale.

## Project Goal
A Hearthstone-style card game built in Unity, single-player vs AI, using C# with a data-driven card/effect system (ScriptableObjects). Developed on Ubuntu using Unity + VS Code, version controlled via Git/GitHub (with Git LFS enabled for art/audio).

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
| `EffectTester.cs` | `Scripts/Cards/` | Temporary MonoBehaviour for manual testing; now constructs a real `Player`/`Board`/`GameContext`/`Target`, plus a `PlayerHand` to test drawing. **Not permanent** — delete once real gameplay loop exists. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with a `Deck`/`Hand` of `CardData`. Lives in `Cards` (not `Core`) since it needs to reference `CardData`. `DrawCard()` moves a card from deck to hand (currently takes index 0, no shuffling yet). |

## Test Assets Created
- `TestCard_Fireball.asset` — a Spell card, 4 mana, linked to `Effect_Deal3Damage`
- `Effect_Deal3Damage.asset` — a `DealDamageEffect` instance, damage = 3

## Verified Working
Confirmed end-to-end, three rounds now:
1. Placeholder `Target`/`GameContext` version.
2. Real `Player`/`Board`/`Minion` version.
3. `PlayerHand` draw logic — `EffectTester` builds a starter deck containing `TestCard_Fireball`, draws it into hand (confirmed via log showing hand/deck counts), then still successfully plays the card's effect against the opponent `Player`, reducing their health.

## Current Blocker / Last Thing Worked On
None currently. Just resolved: after a Unity Editor restart, the scene's `EffectTester` GameObject appeared to be missing (Hierarchy showed only default `SampleScene` contents — Main Camera + Global Light 2D). Root cause: the GameObject/component setup was done in a previous session but never saved to the scene file with `Ctrl+S` before closing — it only existed in memory for that session. Fixed by recreating `EffectTester` in the Hierarchy, re-assigning `TestCard_Fireball`, and explicitly saving with `Ctrl+S` this time. Confirmed scene is `Assets/Scenes/SampleScene.unity` (never got moved to `_Project/Scenes/` — left as-is, not required).

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

## Next Steps (in order)
1. Build `TurnManager` (turn order, mana refill each turn, win/loss conditions). This will also be a natural place to add a proper `PlayCard()` method to `PlayerHand` that checks/spends mana.
2. Build basic AI opponent logic.
3. Build Card UI (hand display, drag-and-drop to board) — first real visuals milestone.
4. Delete `EffectTester` once real play/board interaction replaces it.
5. Add real deck shuffling to `PlayerHand.DrawCard()` (currently just takes index 0 — fine for single-test-card scenarios, not for a real deck).

## Git Habits Being Followed
- Commit at each logical checkpoint (not just end-of-day)
- Descriptive commit messages
- Push regularly to `github.com/craig-middleton/hearthClone`
