# HearthstoneClone - Project Status

_Last updated: 2026-07-09_

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
| `DealDamageEffect.cs` | `Scripts/Effects/` | First concrete effect; deals damage to a Target |
| `Target.cs` | `Scripts/Core/` | Placeholder representing something an effect acts on (currently just health); will be replaced/expanded once real `Minion`/`Player` classes exist |
| `GameContext.cs` | `Scripts/Core/` | Placeholder for shared game state passed to effects; will hold references to Board/Players/TurnManager once built |
| `EffectTester.cs` | `Scripts/Cards/` | Temporary MonoBehaviour used to manually test the card → effect → execution pipeline via the Inspector + Play mode. **Not permanent** — delete once real gameplay loop exists. |

## Test Assets Created
- `TestCard_Fireball.asset` — a Spell card, 4 mana, linked to `Effect_Deal3Damage`
- `Effect_Deal3Damage.asset` — a `DealDamageEffect` instance, damage = 3

## Verified Working
Not yet confirmed end-to-end. The effect pipeline (`CardData` → `CardEffect` → `Execute`) compiles, but the in-scene test via `EffectTester` has not been successfully run in Play mode yet — still resolving setup issues (see below).

## Current Blocker / Last Thing Worked On
`EffectTester.cs` was originally created in `Scripts/Core/`, which caused a `CS0246: 'Cards' does not exist` error — `Core` can't reference `Cards` without creating a circular assembly dependency (`Core → Cards → Effects → Core`). Fix in progress: move `EffectTester.cs` to `Scripts/Cards/` and change its namespace to `HearthstoneClone.Cards`. Not yet confirmed working after the move — this is the very next thing to verify.

## Lessons Learned / Gotchas (useful to remember)
- **Script filename must exactly match the class name** for Unity to allow attaching it as a Component ("script needs to derive from MonoBehaviour" error can actually mean a filename/class mismatch, not a real inheritance problem).
- **Asmdef "Name" field vs filename**: an `.asmdef` file's actual assembly identity is set by the **Name** field in its Inspector, not just the filename — check this if references seem to silently fail.
- **CS0246 "type or namespace not found" has two possible causes**: (1) the `.asmdef` isn't referencing the right assembly, or (2) a missing `using` statement in the file itself even when the asmdef reference is correct. Check both.
- **Circular assembly references are rejected by Unity** — keep dependency direction one-way (see Architecture Decisions above). If a new script needs types from two assemblies that don't currently depend on each other, put the script in whichever assembly already sits "above" both in the dependency chain, rather than adding a new cross-reference.
- **GitHub no longer supports password auth for git over HTTPS** — use `gh auth login` (stores credentials for the terminal) or a Personal Access Token as the password. The PAT only needs the `repo` scope.
- **New repo via Unity Hub's GitHub option** required generating a classic PAT with `repo` scope (all scopes are unchecked by default — must be explicitly selected).
- **.NET SDK is separate from Unity's own Mono/compilation runtime** — needed only so VS Code's C# Dev Kit can do IntelliSense/debugging; install via Microsoft's apt feed (`dotnet-sdk-8.0`), not Ubuntu's default repo.
- **Git doesn't track empty folders** — expected, not a bug; folders show up in git once files are added to them.

## Next Steps (in order)
1. Confirm `EffectTester` works after moving it to `Scripts/Cards/` — attach to a GameObject, assign `TestCard_Fireball`, Play, check Console for the two expected log lines (starting health, then damage dealt).
2. Commit checkpoint: `"Add EffectTester and verify CardData -> CardEffect execution pipeline"`
3. Build real `Player` and `Board` classes in `Core` — will replace placeholder `Target`/`GameContext` with real game state (hand, mana, board minions, turn tracking).
4. Build `TurnManager` (turn order, mana refill each turn, win/loss conditions).
5. Build basic AI opponent logic.
6. Build Card UI (hand display, drag-and-drop to board).
7. Delete `EffectTester` once real play/board interaction replaces it.

## Git Habits Being Followed
- Commit at each logical checkpoint (not just end-of-day)
- Descriptive commit messages
- Push regularly to `github.com/craig-middleton/hearthClone`
