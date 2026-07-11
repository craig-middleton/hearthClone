# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.

_Last updated: 2026-07-10 (session 3)_

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
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with a `Deck`/`Hand` of `CardData`. `DrawCard()` moves a card from deck to hand (index 0, no shuffling yet). `PlayCard()` validates the card is in hand and mana is sufficient, deducts mana, removes from hand, summons a `Minion` if applicable, and fires the card's `onPlayEffect` if a target is given. Returns bool success/failure rather than throwing. |
| `TurnManager.cs` | `Scripts/Core/` | Owns turn order and mana progression. `StartGame()` sets turn 1, Player One first. `EndTurn()` swaps to opponent via `Board.GetOpponent()`, increments turn number, refills mana. Mana ramps +1 per turn for that player, capped at 10, refilling to max each time. Lives in `Core` since it only needs `Player`/`Board` — no card dependency. |

## Test Assets Created
- `TestCard_Fireball.asset` — a Spell card, 4 mana, linked to `Effect_Deal3Damage`
- `Effect_Deal3Damage.asset` — a `DealDamageEffect` instance, damage = 3

## Verified Working
Confirmed end-to-end, four rounds now, most recently:
`TurnManager.StartGame()` correctly sets Player One's turn with 1/1 mana. `PlayerHand.DrawCard()` still works. `PlayerHand.PlayCard()` correctly **rejected** playing `TestCard_Fireball` (4 mana cost) against 1 available mana, logging the expected "not enough mana" message — confirming the mana-gate guard clause works as intended. `TurnManager.EndTurn()` correctly advanced to Turn 2, Player Two, 1/1 mana. Full Console output matched predictions exactly.

## Current Blocker / Last Thing Worked On
None. Just finished: `TurnManager` (turn order + mana refill) and `PlayerHand.PlayCard()` (mana-gated card playing, minion summoning, effect execution) both built and verified via `EffectTester`. Optional follow-up not yet done: temporarily lowering `TestCard_Fireball`'s mana cost to 1 to confirm the successful-play path (summon/effect execution) logs correctly, then resetting it back to 4.

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
**Plan reordered**: basic Card UI moved ahead of AI logic, to get visuals on screen sooner (Craig's preference).
1. Build basic Card UI — Unity UI Canvas + card prefab (image, name/cost/attack/health text), rendering `Player One`'s hand on screen. Placeholder colors/art is fine for now, no real illustrations needed yet. This is the first real visuals milestone.
2. Wire up basic drag-and-drop or click-to-play from hand to board (visual representation of `PlayerHand.PlayCard()`).
3. Build basic AI opponent logic.
4. Delete `EffectTester` once real play/board interaction replaces it.
5. Add real deck shuffling to `PlayerHand.DrawCard()` (currently just takes index 0).

## Git Habits Being Followed
- Commit at each logical checkpoint (not just end-of-day)
- Descriptive commit messages
- Push regularly to `github.com/craig-middleton/hearthClone`
