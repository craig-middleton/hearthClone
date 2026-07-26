# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.

_Last updated: 2026-07-26 (session 11 — opponent (AI) board minions now have a visual home; both players' boards render simultaneously)_

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
  - `AI.asmdef` exists with references to `Core`, `Cards`, `Effects`.
  - `UI.asmdef` references `AI.asmdef` too, since `EffectTester` calls into `AIController`.

## Code Written So Far

| File | Location | Purpose |
|---|---|---|
| `CardData.cs` | `Scripts/Cards/` | ScriptableObject: card identity, stats, mana cost, and a link to its `onPlayEffect` |
| `CardEffect.cs` | `Scripts/Effects/` | Abstract base class all effects inherit from; defines `Execute(GameContext, Target)` |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Concrete effect; deals damage to a Target; logs remaining health via `Target.GetCurrentHealth()` |
| `Minion.cs` | `Scripts/Core/` | Runtime instance of a minion on the board (name, attack, health). Deliberately generic — knows nothing about `CardData`. |
| `Player.cs` | `Scripts/Core/` | Real game state: health, mana (current/max), list of board minions. No `IsAI` flag — AI-ness is tracked externally by which `Player`/`PlayerHand` the `AIController` wraps. |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent(player)` helper. |
| `GameContext.cs` | `Scripts/Core/` | Holds a real `Board` reference. |
| `Target.cs` | `Scripts/Core/` | Points to either a real `Player` or `Minion`. Provides `TakeDamage()` and `GetCurrentHealth()`. |
| `TurnManager.cs` | `Scripts/Core/` | Owns turn order and mana progression. `StartGame()` sets turn 1, Player One first. `EndTurn()` swaps `CurrentPlayer` via `Board.GetOpponent()`, increments turn number, refills mana (+1/turn, capped at 10). |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper MonoBehaviour. Constructs `Player`/`Board`/`GameContext`/`TurnManager`/both `PlayerHand`s/`AIController` in `Start()`. `OnCardClicked(CardData)` handles the human's card plays. `OnEndTurnClicked()` wired to a public `endTurnButton` field — ends the human's turn, auto-runs the AI's turn if `CurrentPlayer` swaps to Player Two, then ends turn again to hand control back. **New this session:** added a second public `opponentBoardDisplay` field; `RefreshBoardDisplay()` now renders both `playerOne.BoardMinions` (existing `boardDisplay`) AND `playerTwo.BoardMinions` (new `opponentBoardDisplay`) every time it's called. **Not permanent** — delete/rename once real gameplay loop exists. |
| `CardView.cs` | `Scripts/UI/` | Thin display component for one card — writes name/cost/stats onto TMP fields, wires a `Button.onClick` to a click callback. Attached to `CardView` prefab. |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — clears and respawns one `CardView` per hand card. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with a `Deck`/`Hand` of `CardData`. `DrawCard()` takes index 0 (no shuffle yet). `PlayCard(CardData, GameContext, Target)` validates hand membership + mana, deducts mana, summons a `Minion` if applicable, fires `onPlayEffect` if a target given, returns bool. |
| `MinionView.cs` | `Scripts/UI/` | Display-only — `SetMinion(Minion)` writes name/attack-health onto TMP fields. No click/Button (board minions aren't interactive yet). |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard(List<Minion>)` — clears and respawns one `MinionView` per board minion. Fully generic (no player-specific logic) — this is why adding opponent board visualization only needed a second *instance* of this same component, not a code change to the component itself. |
| `AIController.cs` | `Scripts/AI/` | Wraps a `PlayerHand` (the AI's) plus `GameContext`/`Board`. `TakeTurn()` loops: scan hand for any card whose `manaCost <= CurrentMana`, play the first one found via `PlayerHand.PlayCard()`, repeat until a full pass plays nothing. For cards with an `onPlayEffect` (e.g. Fireball), builds a `Target` pointing at the opponent player's face — no board-state evaluation yet, so it always burns face. Lives in its own `AI` assembly. |

## Scene/Editor Setup (new this session)
- Duplicated `BoardPanel` → `OpponentBoardPanel`, repositioned via Rect Transform `Pos Y: 600` (stretch-anchored, same Left/Right/Height as original) to sit near the top of the play area, visually separate from the player's board and the End Turn button.
- Duplicated `BoardDisplayController` → `OpponentBoardDisplayController`, with its `BoardDisplay` component's `Board Panel` field pointed at `OpponentBoardPanel` and `Minion View Prefab` set to the same `MinionView` prefab.
- `OpponentBoardDisplayController` dragged onto `EffectTester`'s new `Opponent Board Display` field.

## Test Assets Created
- `TestCard_Fireball.asset` — a Spell card, 4 mana, linked to `Effect_Deal3Damage`
- `Effect_Deal3Damage.asset` — a `DealDamageEffect` instance, damage = 3
- `TestCard_Goblin.asset` — a Minion card, Attack 2, Health 2, no `onPlayEffect`. **Mana Cost needs re-confirming** — Craig said he reset it to `2` last session, but the Inspector was observed showing `1` again mid-session this time. Worth a fresh check next session before assuming it's correct.

## Verified Working
- **Board visuals** (session 9): click-to-play chain confirmed end-to-end with visual + Console confirmation.
- **AI opponent turn logic** (session 10): full turn cycle confirmed via Console — AI correctly skips unaffordable cards, plays what it can afford, hands control back to the human automatically.
- **Opponent board visualization (this session)**: Confirmed visually — after a full turn cycle (Player One plays Goblin → End Turn → AI plays Goblin → control returns), **both** Goblins are now rendered on screen simultaneously: Player One's in the original `BoardPanel`, Player Two's (AI's) in the new `OpponentBoardPanel` above it. Matches Console log showing both `"Goblin summoned to Player One's board."` and `"Goblin summoned to Player Two's board."`.

## Current Blocker / Last Thing Worked On
None — **opponent board visualization is complete and verified**. Session ended here on a clean, visible milestone (both players' boards rendering side by side).

**Not yet tested / worth checking next session:**
1. **Multi-card AI turns** — still only tested with enough mana for one card. Confirm the AI correctly chains multiple plays in a single turn once mana is higher.
2. **Fireball's targeting in practice** — `AIController` builds a `Target` at the opponent's face for any card with an `onPlayEffect`, but this still hasn't been exercised in a real playtest — the AI hasn't had 4 mana yet in any test run. Session ended at Turn 3 (Player One, 2/2 mana) — a few more End Turn clicks should get both sides to 4 mana.
3. **Re-confirm `TestCard_Goblin`'s Mana Cost is actually `2`** — see Test Assets Created note above; this flipped back to `1` in the Inspector at some point this session and wasn't re-verified before ending.

**Cosmetic, still low priority:**
- The known `BoardPanel` text-clipping issue (minion name cut off at the left edge, e.g. "oblin" instead of "Goblin") is now visible on **both** boards, not just the player's. Still not investigated — likely a Horizontal Layout Group padding/alignment issue.
- The two board panels are currently fairly close together vertically (slightly cramped visual separation) — worth nudging `OpponentBoardPanel`'s `Pos Y` a bit higher, or the player's `BoardPanel` a bit lower, for clearer separation between "your board" and "opponent's board."

## Lessons Learned / Gotchas (useful to remember)
**Assembly definitions (.asmdef)**
- Circular references are rejected by Unity — keep dependency direction one-way (`Cards → Effects → Core`; `Core` and `Effects` never reference `Cards`). If a script needs types from two assemblies with no relationship, it belongs in whichever assembly sits "above" both.
- An asmdef's real identity is its **Name** field (Inspector), not its filename — check this if references silently fail.
- `CS0246` errors can mean either a missing asmdef reference OR a missing `using` statement — check both. Unity packages (e.g. TextMeshPro/`Unity.TextMeshPro`) need an explicit asmdef reference too, separate from any `using` line.
- When a script in one assembly (e.g. `UI`) calls into a type from another assembly (e.g. `AI`), BOTH the `using` directive AND an explicit **Assembly Definition Reference** on the calling assembly's `.asmdef` are needed. Fix via: select the `.asmdef` asset → Inspector → Assembly Definition References → `+` → add the missing one → **Apply**.

**Unity Editor / UI basics**
- Script filename must exactly match its class name, or Unity won't allow attaching it as a Component.
- Scene changes (new GameObjects, component assignments) aren't saved until `Ctrl+S` — get in the habit of saving after Hierarchy changes, not just script edits.
- Always confirm the Inspector is showing the GameObject you actually mean to edit (easy to accidentally edit a parent instead of the intended child).
- Rect Transform anchor presets: hold **Alt** while clicking a preset to also reposition the object. New UI Text/Panels default to a stretch anchor (Width/Height fields only appear after switching to a fixed-point anchor like center).
- **New this session**: for stretch-anchored panels, repositioning is done via the Rect Transform's `Pos Y`/`Left`/`Right`/`Height` fields in the Inspector, not by freely dragging in Scene view (dragging can fight the anchor/stretch setup). Easiest approach: roughly drag into place with the Move tool (`W`) first — Unity auto-updates `Pos Y` to match — then fine-tune the number directly.
- TMP Auto Size won't stop wrapping if placeholder text itself is too long for the box — usually resolves once real (shorter) data replaces it, not a real bug.
- Overlapping semi-transparent UI panels of similar default grey color can visually merge into what looks like one oversized element. If something looks wrongly sized, try setting a suspect parent panel's alpha to 0 first to rule out a layering illusion before assuming a real layout bug.
- If the Scene view camera seems "lost," select the relevant object and press **F** to frame it.
- **New this session**: a circular gizmo appearing in the Scene view unexpectedly is likely just the `Global Light 2D` range/direction gizmo overlapping your selection — it's editor-only, doesn't appear in Game view or builds, and isn't attached to whatever UI element you're inspecting. Toggle off 2D light gizmos via the Scene view's Gizmos dropdown if it's distracting.
- **Editing a runtime `(Clone)` GameObject in the Hierarchy during Play mode is temporary** — changes vanish when Play mode stops. To make a permanent change to a prefab, stop Play mode, double-click the prefab asset to enter Prefab Edit Mode, make the change there, exit/save.
- When a script field expects a specific Component type but the target GameObject doesn't have that component yet, add the component first, then drag the **GameObject** itself onto the field — Unity finds the right component on it automatically. A `NullReferenceException` on a line touching a component field usually means the field was never assigned in the Inspector.
- An empty/unassigned Inspector field for a listener-style hookup (e.g. a `Button` field that gets `.onClick.AddListener(...)` in `Start()`) fails **silently** — no error, the button just does nothing when clicked. If a wired button appears inert, check the field isn't still `None` before suspecting the underlying logic.
- Editing a TMP label's `Text Input` box in the Inspector requires clicking into the box, changing the text, then clicking away elsewhere in the Inspector to commit the change — the Hierarchy GameObject's name does NOT reflect the label text, so don't use the Hierarchy name as a check for whether the edit "took."

**Core architecture principle**
- Keep `Core` types generic and unaware of `CardData` — anything needing both real game state and card asset data belongs in the `Cards` layer as a wrapper.
- AI-ness isn't a flag on `Player` — `AIController` is simply told which `PlayerHand` it controls at construction time, keeping `Core.Player` free of any AI-specific concept.
- **New this session**: when a display/rendering component is already written generically (takes a plain list/data with zero player-specific logic, like `BoardDisplay`), the cheapest way to support "two of something" (two boards, potentially two hands later) is usually a second *instance* of the same component pointed at a second data source and a second UI panel — not a code change to make the component juggle two lists itself. Keeps the "one component, one job" pattern consistent.

**Git / GitHub / environment**
- GitHub requires `gh auth login` or a PAT (repo scope only) for HTTPS git auth — plain passwords no longer work.
- `.gitignore` should exclude auto-generated files: `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`. Git doesn't track empty folders (expected, not a bug).
- Unity's crash-recovery prompt (`Assets/_Recovery/`) is safe to accept; delete the folder after and gitignore it if unneeded.
- .NET SDK (install via Microsoft's apt feed, not Ubuntu's default repo) is only needed for VS Code's C# IntelliSense/debugging — separate from Unity's own compiler.

## Next Steps (in order)
1. Re-confirm `TestCard_Goblin`'s Mana Cost is `2`, not `1`.
2. Playtest further (a few more End Turn clicks) to reach 4 mana on both sides — confirm multi-card AI turns and Fireball's face-damage targeting actually work as expected.
3. Adjust vertical spacing between `BoardPanel` and `OpponentBoardPanel` for clearer visual separation.
4. Optional: investigate/fix the minion-text-clipping issue on both board panels — check Horizontal Layout Group padding/alignment.
5. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
6. Add real deck shuffling to `PlayerHand.DrawCard()` (currently just takes index 0).
7. Consider upgrading click-to-play to real drag-and-drop, if desired (click-to-play works fine as an interim/MVP interaction model).

## Git Habits Being Followed
- Simple commit template: `git add .` / `git commit -m "short one-line summary"` / `git push`
- Push regularly to `github.com/craig-middleton/hearthClone`
