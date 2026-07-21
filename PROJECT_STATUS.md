# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.

_Last updated: 2026-07-22 (session 9 — board visuals fully working, Goblin minion renders on board after being played)_

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
| `EffectTester.cs` | `Scripts/UI/` | Temporary MonoBehaviour bootstrapper; constructs `Player`/`Board`/`GameContext`/`Target`/`PlayerHand`/`TurnManager` in `Start()`, then renders the hand and waits for clicks. `OnCardClicked(CardData)` calls `PlayerHand.PlayCard()` and re-renders the hand on success — this is the click-to-play handler. `playerOneHand`/`context`/`opponentTarget` are class fields (not local vars) so they persist for use after `Start()` returns. **Not permanent** — delete/rename once real gameplay loop exists. |
| `CardView.cs` | `Scripts/UI/` | Thin display component for one card — `SetCard(CardData, Action<CardData> clickCallback)` writes name/cost/stats onto TMP Text fields, stores the card + callback, and wires a `Button` component's `onClick` to invoke the callback with this card. Uses `TMP_Text` (TextMeshPro). Attached to the `CardView` prefab (`Prefabs/Cards/CardView.prefab`), which also needed a Unity `Button` component added manually (see gotchas). |
| `HandDisplay.cs` | `Scripts/UI/` | Manages a collection of card visuals — `RenderHand(List<CardData> hand, Action<CardData> onCardClicked)` clears previously spawned cards under `handPanel`, then instantiates one `CardView` prefab per card, passing the click callback through to each. Purely a pass-through for the callback — doesn't know what a click means. Attached to `HandDisplayController` GameObject in the scene. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with a `Deck`/`Hand` of `CardData`. `DrawCard()` moves a card from deck to hand (index 0, no shuffling yet). `PlayCard()` validates the card is in hand and mana is sufficient, deducts mana, removes from hand, summons a `Minion` if applicable, and fires the card's `onPlayEffect` if a target is given. Returns bool success/failure rather than throwing. |
| `TurnManager.cs` | `Scripts/Core/` | Owns turn order and mana progression. `StartGame()` sets turn 1, Player One first. `EndTurn()` swaps to opponent via `Board.GetOpponent()`, increments turn number, refills mana. Mana ramps +1 per turn for that player, capped at 10, refilling to max each time. Lives in `Core` since it only needs `Player`/`Board` — no card dependency. |
| `MinionView.cs` | `Scripts/UI/` | Display-only component for one board minion — `SetMinion(Minion)` writes name/attack-health onto TMP Text fields. Takes a `Core.Minion` directly (not `CardData`), since board minions are runtime state. No click/Button yet — board minions aren't interactive. Attached to the `MinionView` prefab (`Prefabs/Board/MinionView.prefab`). |
| `BoardDisplay.cs` | `Scripts/UI/` | Manages a collection of minion visuals — `RenderBoard(List<Minion>)` clears previously spawned minions under `boardPanel`, then instantiates one `MinionView` prefab per minion. Structurally identical to `HandDisplay`, same pattern reused deliberately. |

## Test Assets Created
- `TestCard_Fireball.asset` — a Spell card, 4 mana, linked to `Effect_Deal3Damage`
- `Effect_Deal3Damage.asset` — a `DealDamageEffect` instance, damage = 3
- `TestCard_Goblin.asset` — a Minion card, 2 mana (check this is set to 2, not the temporary 1 used for testing), Attack 2, Health 2, no `onPlayEffect`

## Verified Working
Confirmed end-to-end, seven rounds now, most recently: **board visuals work**. Clicking "Goblin" in hand fires the full chain — `PlayerHand.PlayCard()` deducts mana, creates a `Minion`, adds it to `Player.BoardMinions`, `EffectTester.RefreshBoardDisplay()` calls `BoardDisplay.RenderBoard()`, which spawns a `MinionView` showing "Goblin / 2 / 2" in the board area. Confirmed via Console (`"Player One played Goblin. Mana remaining: 0"`, `"Goblin summoned to Player One's board."`) and visually on screen — Goblin disappeared from hand, appeared on board, Fireball remained in hand untouched.

## Current Blocker / Last Thing Worked On
None — **board visuals feature is complete** (see Verified Working above). All of `MinionView`, `BoardDisplay`, `BoardPanel`, `BoardDisplayController` built, wired, and confirmed working with the `TestCard_Goblin` minion.

**One cosmetic loose end, not yet fixed**: the rendered `MinionView` on the board appeared positioned partly off the left edge of the screen (text showed as "oblin" instead of "Goblin", cut off). Likely a Horizontal Layout Group padding/alignment issue on `BoardPanel`, or the panel's own horizontal positioning — not yet investigated. Low priority since functionality is fully confirmed; worth a quick look next time UI polish is on the agenda.

**Also confirm before next session**: `TestCard_Goblin`'s Mana Cost should be reset to `2` (was temporarily `1` for testing with only 1 starting mana).

## Lessons Learned / Gotchas (useful to remember)
**Assembly definitions (.asmdef)**
- Circular references are rejected by Unity — keep dependency direction one-way (`Cards → Effects → Core`; `Core` and `Effects` never reference `Cards`). If a script needs types from two assemblies with no relationship, it belongs in whichever assembly sits "above" both.
- An asmdef's real identity is its **Name** field (Inspector), not its filename — check this if references silently fail.
- `CS0246` errors can mean either a missing asmdef reference OR a missing `using` statement — check both. Unity packages (e.g. TextMeshPro/`Unity.TextMeshPro`) need an explicit asmdef reference too, separate from any `using` line.

**Unity Editor / UI basics**
- Script filename must exactly match its class name, or Unity won't allow attaching it as a Component.
- Scene changes (new GameObjects, component assignments) aren't saved until `Ctrl+S` — get in the habit of saving after Hierarchy changes, not just script edits.
- Always confirm the Inspector is showing the GameObject you actually mean to edit (easy to accidentally edit a parent instead of the intended child).
- Rect Transform anchor presets: hold **Alt** while clicking a preset to also reposition the object — otherwise the anchor changes but the position doesn't recalculate, making things jump oddly. New UI Text/Panels default to a stretch anchor (Width/Height fields only appear after switching to a fixed-point anchor like center).
- TMP Auto Size won't stop wrapping if placeholder text itself is too long for the box — usually resolves once real (shorter) data replaces it, not a real bug.
- Overlapping semi-transparent UI panels of similar default grey color can visually merge into what looks like one oversized element. If something looks wrongly sized, try setting a suspect parent panel's alpha to 0 first to rule out a layering illusion before assuming a real layout bug.
- If the Scene view camera seems "lost," select the relevant object and press **F** to frame it.
- **Editing a runtime `(Clone)` GameObject in the Hierarchy during Play mode is temporary** — changes vanish when Play mode stops. To make a permanent change to a prefab (e.g. adding a missing `Button` component), stop Play mode, then double-click the prefab **asset** in the Project window to enter Prefab Edit Mode (Hierarchy shows the name with no `(Clone)` suffix), make the change there, and exit/save.
- When a script field expects a specific Component type (e.g. a `Button` field) but the target GameObject doesn't have that component yet, add the component first (Add Component → Button), then drag the **GameObject** itself onto the field — Unity finds the right component on it automatically. A `NullReferenceException` on a line touching a component field usually means the field was never assigned in the Inspector.

**Core architecture principle**
- Keep `Core` types generic and unaware of `CardData` — anything needing both real game state and card asset data belongs in the `Cards` layer as a wrapper.

**Git / GitHub / environment**
- GitHub requires `gh auth login` or a PAT (repo scope only) for HTTPS git auth — plain passwords no longer work.
- `.gitignore` should exclude auto-generated files: `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`. Git doesn't track empty folders (expected, not a bug).
- Unity's crash-recovery prompt (`Assets/_Recovery/`) is safe to accept; delete the folder after and gitignore it if unneeded.
- .NET SDK (install via Microsoft's apt feed, not Ubuntu's default repo) is only needed for VS Code's C# IntelliSense/debugging — separate from Unity's own compiler.

## Next Steps (in order)
1. Optional: investigate/fix the `BoardPanel` positioning issue (minion rendering partly off-screen left) — check Horizontal Layout Group padding/alignment on `BoardPanel`.
2. Build basic AI opponent logic (even a simple "play first affordable card" AI).
3. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
4. Add real deck shuffling to `PlayerHand.DrawCard()` (currently just takes index 0).
5. Consider upgrading click-to-play to real drag-and-drop, if desired (click-to-play works fine as an interim/MVP interaction model).

## Git Habits Being Followed
- Commit at each logical checkpoint (not just end-of-day)
- Descriptive commit messages
- Push regularly to `github.com/craig-middleton/hearthClone`
