# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.

_Last updated: 2026-08-01 (session 12 — real deck (5 unique cards, shuffled, 5-card opening hands) for both players; AI's hand now visible on screen)_

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
| `CardData.cs` | `Scripts/Cards/` | ScriptableObject: `cardName`, `description`, `artwork`, `manaCost`, `cardType` (`Minion`/`Spell`), `attack`, `health`, `onPlayEffect`. |
| `CardEffect.cs` | `Scripts/Effects/` | Abstract base class all effects inherit from; defines `Execute(GameContext, Target)` |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Concrete effect; deals damage to a Target; logs remaining health via `Target.GetCurrentHealth()` |
| `Minion.cs` | `Scripts/Core/` | Runtime instance of a minion on the board (name, attack, health). Deliberately generic — knows nothing about `CardData`. |
| `Player.cs` | `Scripts/Core/` | Real game state: health, mana (current/max), list of board minions. No `IsAI` flag — AI-ness is tracked externally by which `Player`/`PlayerHand` the `AIController` wraps. |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent(player)` helper. |
| `GameContext.cs` | `Scripts/Core/` | Holds a real `Board` reference. |
| `Target.cs` | `Scripts/Core/` | Points to either a real `Player` or `Minion`. Provides `TakeDamage()` and `GetCurrentHealth()`. |
| `TurnManager.cs` | `Scripts/Core/` | Owns turn order and mana progression. `StartGame()` sets turn 1, Player One first. `EndTurn()` swaps `CurrentPlayer` via `Board.GetOpponent()`, increments turn number, refills mana (+1/turn, capped at 10). |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper MonoBehaviour. **Rewritten this session**: replaced the old individual `CardData` test fields with a single `public List<CardData> cardPool` (drag all unique test cards in via Inspector) plus `public int copiesPerCard` (default 2). `BuildDeck(pool)` expands the pool into a full deck list by repeating each card `copiesPerCard` times. In `Start()`, both `playerOneHand` and `playerTwoHand` are built via `BuildDeck()`, then `.Shuffle()`, then `.DrawOpeningHand()` (5 cards). Added a second `public HandDisplay opponentHandDisplay` field alongside the existing `opponentBoardDisplay`; `RefreshHandDisplay()` now renders both hands every call — the human's via `handDisplay.RenderHand(playerOneHand.Hand, OnCardClicked)`, the AI's via `opponentHandDisplay.RenderHand(playerTwoHand.Hand, null)` (read-only, non-clickable). `OnEndTurnClicked()` and card-play logic otherwise unchanged from last session. **Not permanent** — delete/rename once real gameplay loop exists. |
| `CardView.cs` | `Scripts/UI/` | Thin display component for one card. `SetCard(CardData, Action<CardData> clickCallback)` writes name/cost/stats onto TMP fields, wires `Button.onClick` via `AddListener(() => onClicked?.Invoke(card))`. The `?.` null-conditional is what makes passing `null` as the callback safe (clicking silently does nothing) — this is what allows the AI's hand to be displayed read-only with zero code changes to this file. |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — clears and respawns one `CardView` per hand card, passing the callback straight through to each. Fully generic — reused as-is for the opponent's hand, same pattern as `BoardDisplay`. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with a `Deck`/`Hand` of `CardData`. **New this session**: `Shuffle()` — in-place Fisher-Yates shuffle of `Deck`. `DrawOpeningHand(int count = 5)` — calls `DrawCard()` `count` times. `DrawCard()` itself unchanged (still takes index 0 — correct now that the deck is pre-shuffled). `PlayCard(CardData, GameContext, Target)` validates hand membership + mana, deducts mana, summons a `Minion` if applicable, fires `onPlayEffect` if a target given, returns bool. |
| `MinionView.cs` | `Scripts/UI/` | Display-only — `SetMinion(Minion)` writes name/attack-health onto TMP fields. No click/Button (board minions aren't interactive yet). |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard(List<Minion>)` — clears and respawns one `MinionView` per board minion. Fully generic — reused for both boards via two separate instances. |
| `AIController.cs` | `Scripts/AI/` | Wraps a `PlayerHand` (the AI's) plus `GameContext`/`Board`. `TakeTurn()` loops: scan hand for any card whose `manaCost <= CurrentMana`, play the first one found via `PlayerHand.PlayCard()`, repeat until a full pass plays nothing. For cards with an `onPlayEffect` (e.g. Fireball), builds a `Target` pointing at the opponent player's face. Lives in its own `AI` assembly. |

## Scene/Editor Setup
- `BoardPanel` (Player One's board) + `OpponentBoardPanel` (duplicate, repositioned via Rect Transform `Pos Y`) — each with their own `BoardDisplay` controller (`BoardDisplayController` / `OpponentBoardDisplayController`), both wired to `EffectTester`'s `boardDisplay` / `opponentBoardDisplay` fields.
- **New this session**: `HandPanel` (Player One's hand) + `OpponentHandPanel` (duplicate, repositioned to sit near the top of the screen) — each with their own `HandDisplay` controller (`HandDisplayController` / `OpponentHandDisplayController`), both using the same `CardView` prefab. Wired to `EffectTester`'s `handDisplay` / `opponentHandDisplay` fields.
- `EffectTester`'s old individual card fields (`Card To Test`, `Minion Card To Test`, `Third Card To Test`) are gone — replaced by a single `Card Pool` list (drag all 5 unique cards in) and a `Copies Per Card` int field.

## Test Assets Created
- `TestCard_Wisp.asset` — Minion, 1 mana, Attack 1, Health 1, no effect
- `TestCard_Goblin.asset` — Minion, 2 mana, Attack 2, Health 2, no effect
- `TestCard_RiverCroc.asset` ("River Crocodile") — Minion, 3 mana, Attack 2, Health 3, no effect
- `TestCard_Fireball.asset` — Spell, 4 mana, linked to `Effect_Deal3Damage`
- `TestCard_Boulderfist.asset` — Minion, 5 mana, Attack 4, Health 4, no effect
- `Effect_Deal3Damage.asset` — a `DealDamageEffect` instance, damage = 3
- All 5 unique cards are dragged into `EffectTester`'s `Card Pool` list; `Copies Per Card = 2` gives each player a 10-card deck to draw a 5-card opening hand from.

## Verified Working
- **Board visuals** (session 9): click-to-play chain confirmed end-to-end.
- **AI opponent turn logic** (session 10): full turn cycle confirmed via Console — AI skips unaffordable cards, plays what it can afford, hands control back to the human automatically.
- **Opponent board visualization** (session 11): both players' board minions render simultaneously in separate panels.
- **Fireball face-damage targeting** (session 12, earlier in this session before the deck rework): confirmed via Console — `"Player Two played Fireball... Dealt 3 damage to target. Remaining health: 27"` — Player One's health correctly dropped 30 → 27.
- **Real deck + shuffled 5-card opening hands (this session)**: Confirmed via Console and on-screen — both players draw 5 cards each from a shuffled 10-card deck (5 unique cards × 2 copies), with visibly different draw orders/compositions between the two hands each playthrough (duplicates included, e.g. two Wisps in one hand, two River Crocodiles + two Boulderfists in another).
- **Opponent hand visualization (this session)**: Both hands now render on screen simultaneously — the human's hand (bottom, clickable) and the AI's hand (top, read-only). Confirmed the AI's cards are visible but non-interactive, achieved via passing `null` as `CardView`'s click callback (safe due to the existing `?.Invoke()` null-conditional — no code changes needed to `CardView`/`HandDisplay` themselves).

## Current Blocker / Last Thing Worked On
None — session ended on a clean, visible milestone: both players' hands and boards fully visible on screen, drawn from real shuffled decks.

**Explicitly deferred / not done this session:**
- **Multi-card AI turns still not observed in actual play.** Investigated at length this session: with the *old* fixed 2-3 card, fully-drawn-at-start test deck, this was structurally near-impossible to trigger (see reasoning below) — but the deck rework this session (5 unique cards, 10-card deck, shuffled 5-card hands) may make it possible to observe now, simply because hands are bigger and more varied. **Not yet re-tested post-rework** — worth checking next session by playing a few turns and watching for two `"Player Two played..."` lines within one `--- Player Two (AI) is taking its turn ---` block.
  - *Prior reasoning, kept for context*: with a small, fully-pre-drawn hand and linear +1/turn mana, the cheapest unplayed card in hand is almost always snapped up the instant it's affordable, which typically leaves too little mana left that same turn to also afford a second card — so single-card turns were the structurally likely outcome, not a bug in `AIController.TakeTurn()`'s loop itself (the loop logic — scan, play, re-scan, repeat until nothing affordable — is correct and was verified by code review even when not observed in play).
- **Mulligan system** — scoped and agreed with Craig (both players get a 5-card draw + can mulligan up to 2 cards for new random ones from their deck, via a click-to-mulligan UI similar to the existing play-card interaction, with a separate mulligan phase before turn 1) but **not yet built**. This session only completed the prerequisite work (real shuffled deck, 5-card draw). Mulligan logic itself (`PlayerHand` swap-and-redraw method + UI) is the next planned step.
- Snapshot-folder handoff convention (dated source-file folders in the repo) was tried for two sessions and then **dropped** at Craig's request — going forward, only `PROJECT_STATUS.md` is updated at end of session, no separate source snapshot.

**Cosmetic, still low priority:**
- The known `BoardPanel`/`OpponentBoardPanel` text-clipping issue (minion name cut off at the left edge) is still unaddressed.
- Board panel vertical spacing could still use a bit more separation for clarity.

**Also worth double-checking next session:** confirm `TestCard_Goblin`'s Mana Cost is holding at `2` (this flip-flopped a couple of times across recent sessions).

## Lessons Learned / Gotchas (useful to remember)
**Assembly definitions (.asmdef)**
- Circular references are rejected by Unity — keep dependency direction one-way (`Cards → Effects → Core`; `Core` and `Effects` never reference `Cards`).
- An asmdef's real identity is its **Name** field (Inspector), not its filename — check this if references silently fail.
- `CS0246` errors can mean either a missing asmdef reference OR a missing `using` statement — check both.
- When a script in one assembly calls into a type from another assembly, BOTH the `using` directive AND an explicit **Assembly Definition Reference** on the calling assembly's `.asmdef` are needed. Fix via: select the `.asmdef` asset → Inspector → Assembly Definition References → `+` → add the missing one → **Apply**.

**Unity Editor / UI basics**
- Script filename must exactly match its class name, or Unity won't allow attaching it as a Component.
- Scene changes (new GameObjects, component assignments) aren't saved until `Ctrl+S`.
- Always confirm the Inspector is showing the GameObject you actually mean to edit.
- Rect Transform anchor presets: hold **Alt** while clicking a preset to also reposition the object.
- For stretch-anchored panels, reposition via the Rect Transform's `Pos Y`/`Left`/`Right`/`Height` fields, not by freely dragging in Scene view. Easiest approach: roughly drag into place with the Move tool (`W`) first — Unity auto-updates `Pos Y` to match — then fine-tune the number directly.
- TMP Auto Size won't stop wrapping if placeholder text itself is too long for the box — usually resolves once real (shorter) data replaces it.
- Overlapping semi-transparent UI panels of similar default grey color can visually merge into what looks like one oversized element — try setting a suspect parent panel's alpha to 0 to rule out a layering illusion.
- If the Scene view camera seems "lost," select the relevant object and press **F** to frame it.
- A circular gizmo appearing unexpectedly in Scene view is likely just the `Global Light 2D` gizmo overlapping your selection — editor-only, not a bug.
- Editing a runtime `(Clone)` GameObject in Play mode is temporary — for permanent prefab changes, stop Play mode, double-click the prefab asset to enter Prefab Edit Mode.
- When a script field expects a specific Component type, add the component first, then drag the **GameObject** itself onto the field.
- An empty/unassigned Inspector field for a listener-style hookup (e.g. a `Button` field with `.onClick.AddListener(...)` in `Start()`) fails **silently** — no error, just inert behavior. Check the field isn't `None` before suspecting the underlying logic.
- Editing a TMP label's `Text Input` box requires clicking in, changing the text, then clicking away elsewhere in the Inspector to commit — the Hierarchy GameObject's name does NOT reflect the label text.
- **New this session**: when reusing an existing prefab reference for a duplicated controller (e.g. `OpponentHandDisplayController`'s `Card View Prefab` field), don't guess/re-drag from the Project window from memory — instead check the *original* controller's Inspector for exactly which asset is assigned, and use that same one, to guarantee no accidental mismatch between two supposedly-identical setups.

**Core architecture principle**
- Keep `Core` types generic and unaware of `CardData`.
- AI-ness isn't a flag on `Player` — `AIController` is simply told which `PlayerHand` it controls at construction time.
- When a display/rendering component is already written generically (takes a plain list/data with zero player-specific logic, like `BoardDisplay` or `HandDisplay`), the cheapest way to support "two of something" is usually a second *instance* of the same component pointed at a second data source and a second UI panel — not a code change to make the component juggle two lists itself.
- **New this session**: a callback-based click handler that uses the null-conditional operator (`onClicked?.Invoke(card)`) is "free" read-only-mode support — passing `null` instead of a real callback makes the UI element inert without any special-casing, disabling, or hiding logic needed. Worth designing future interactive display components (e.g. anything with a `Button` + `Action` callback) this way from the start.
- **New this session (deck-building)**: `ScriptableObject`-based cards support "multiple copies in a deck" for free by simply referencing the same asset multiple times in a `List<CardData>` — no need to duplicate the asset itself. A small pool of unique `CardData` assets plus a `copiesPerCard` multiplier is enough to build a realistic-sized deck without maintaining N separate near-identical assets.
- **New this session (shuffling)**: use an in-place Fisher-Yates shuffle for randomizing a `List<T>` — swapping each element with a random earlier-or-equal index, iterating from the end — rather than a naive "pick random index, remove it" approach, which is easy to get subtly biased.

**Git / GitHub / environment**
- GitHub requires `gh auth login` or a PAT (repo scope only) for HTTPS git auth.
- `.gitignore` should exclude `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`.
- Unity's crash-recovery prompt (`Assets/_Recovery/`) is safe to accept; delete the folder after.
- .NET SDK (install via Microsoft's apt feed, not Ubuntu's default repo) is only needed for VS Code's C# IntelliSense/debugging.

## Next Steps (in order)
1. Re-test whether multi-card AI turns now occur naturally with the bigger, shuffled 5-card hands (no code change needed to check this — just playtest a few turns).
2. Build the mulligan system: `PlayerHand` method(s) to swap a chosen hand card back into the deck and draw a new random replacement, then a click-to-mulligan UI (separate phase before turn 1) for the human, plus a simple AI auto-mulligan strategy.
3. Re-confirm `TestCard_Goblin`'s Mana Cost is `2`.
4. Adjust vertical spacing between the board panels; investigate the minion-text-clipping issue.
5. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
6. Consider upgrading click-to-play to real drag-and-drop, if desired.

## Git Habits Being Followed
- Simple commit template: `git add .` / `git commit -m "short one-line summary"` / `git push`
- Push regularly to `github.com/craig-middleton/hearthClone`
