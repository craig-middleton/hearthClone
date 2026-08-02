# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.
- **For code changes, always provide the full rewritten class/file, not just a diff/snippet** — Craig pastes a complete file each time rather than manually merging partial additions.

_Last updated: 2026-08-02 (session 13 — real Hearthstone-accurate opening hands (3 vs 4 + The Coin) implemented and fully verified, including a genuine observed multi-card AI turn)_

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
- **Card/effect model**: data-driven via ScriptableObjects rather than hardcoded per-card classes — cards reference reusable `CardEffect` assets (e.g. "Deal 3 Damage", "Gain 1 Mana") instead of each card having bespoke code.
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
| `CardData.cs` | `Scripts/Cards/` | ScriptableObject: `cardName`, `description`, `artwork`, `manaCost`, `cardType` (`Minion`/`Spell`), `attack`, `health`, `onPlayEffect`. **New this session:** `public bool targetsSelf` — declares whether a card's effect should target its own caster (e.g. The Coin) rather than the opponent (the previous default/only behavior). Data-driven, not inferred from card name/type anywhere in logic. |
| `CardEffect.cs` | `Scripts/Effects/` | Abstract `ScriptableObject` base class; defines `Execute(GameContext, Target)`. |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Concrete effect; `[CreateAssetMenu]`; deals damage to a Target via `target.TakeDamage()`, logs remaining health. |
| `GainManaEffect.cs` | `Scripts/Effects/` (**new this session**) | Concrete effect, same pattern as `DealDamageEffect`; calls `target.GainMana(manaAmount)`. Powers The Coin. |
| `Minion.cs` | `Scripts/Core/` | Runtime instance of a minion on the board (name, attack, health). Deliberately generic — knows nothing about `CardData`. |
| `Player.cs` | `Scripts/Core/` | Real game state: health, mana (current/max), list of board minions. No `IsAI` flag. |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent(player)` helper. |
| `GameContext.cs` | `Scripts/Core/` | Holds a real `Board` reference. |
| `Target.cs` | `Scripts/Core/` | Points to either a real `Player` or `Minion`. `TakeDamage()`, `GetCurrentHealth()`. **New this session:** `GainMana(int amount)` — adds directly to `TargetPlayer.CurrentMana` (not `MaxMana`), so it's a one-turn-only bonus that's naturally wiped by `TurnManager.RefillMana()` on that player's next turn — no extra cleanup logic needed. |
| `TurnManager.cs` | `Scripts/Core/` | Owns turn order and mana progression. `StartGame()` sets turn 1, Player One first. `EndTurn()` swaps `CurrentPlayer`, increments turn number, refills mana (+1/turn, capped at 10). |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper MonoBehaviour. `cardPool` (List<CardData>) + `copiesPerCard` build each player's deck via `BuildDeck()`; both hands `.Shuffle()`. **New this session:** opening hands are now asymmetric, matching real Hearthstone — Player One (goes first) draws 3 via `DrawOpeningHand(3)`; Player Two (goes second) draws 4 via `DrawOpeningHand(4)` **and** receives a new public `coinCard` field directly via `playerTwoHand.AddCardToHand(coinCard)` (bypasses the deck entirely — the Coin isn't drawable). `OnCardClicked()` now checks `card.targetsSelf` to decide whether to build a self-targeting `Target` (`new Target(playerOne)`) or reuse the existing `opponentTarget`. Also has `handDisplay`/`opponentHandDisplay` and `boardDisplay`/`opponentBoardDisplay` pairs (from last session) rendering both players' full hand+board state every refresh. **Not permanent** — delete/rename once real gameplay loop exists. |
| `CardView.cs` | `Scripts/UI/` | Displays one card; `Button.onClick` wired via `() => onClicked?.Invoke(card)` — the null-conditional is what makes the opponent's hand safely non-interactive when `null` is passed as the callback. |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — fully generic, reused for both hands. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with a `Deck`/`Hand` of `CardData`. `Shuffle()` (Fisher-Yates), `DrawOpeningHand(int count = 5)`, `DrawCard()` (index 0, correct now the deck is pre-shuffled). **New this session:** `AddCardToHand(CardData)` — adds a card straight to `Hand` without touching `Deck` at all, used for The Coin. `PlayCard(CardData, GameContext, Target)` validates hand membership + mana, deducts mana, summons a `Minion` if applicable, fires `onPlayEffect` if a target given, returns bool. |
| `MinionView.cs` | `Scripts/UI/` | Display-only, no click/Button. |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard(List<Minion>)` — fully generic, reused for both boards. |
| `AIController.cs` | `Scripts/AI/` | Wraps a `PlayerHand` (the AI's) plus `GameContext`/`Board`. `TakeTurn()` loops: scan hand for any card whose `manaCost <= CurrentMana`, play the first one found, repeat until a full pass plays nothing. **Updated this session:** target-building now checks `card.targetsSelf` — `new Target(aiPlayer)` if true, `new Target(opponent)` if false — mirroring the human-side logic in `EffectTester.OnCardClicked()`. Only builds a `Target` at all if `card.onPlayEffect != null`. Lives in its own `AI` assembly. |

## Test Assets Created
- `TestCard_Wisp.asset` — Minion, 1 mana, Attack 1, Health 1, no effect
- `TestCard_Goblin.asset` — Minion, 2 mana, Attack 2, Health 2, no effect
- `TestCard_RiverCroc.asset` ("River Crocodile") — Minion, 3 mana, Attack 2, Health 3, no effect
- `TestCard_Fireball.asset` — Spell, 4 mana, `targetsSelf = false`, linked to `Effect_Deal3Damage`
- `TestCard_Boulderfist.asset` — Minion, 5 mana, Attack 4, Health 4, no effect
- `Effect_Deal3Damage.asset` — `DealDamageEffect`, damage = 3
- **New this session**: `Effect_GainMana1.asset` — `GainManaEffect`, manaAmount = 1
- **New this session**: `TestCard_Coin.asset` ("The Coin") — Spell, 0 mana, `targetsSelf = true`, linked to `Effect_GainMana1`. **Deliberately NOT in `Card Pool`** — only ever granted via `EffectTester`'s dedicated `Coin Card` field, never drawable from a deck.
- All 5 unique deck cards are in `EffectTester`'s `Card Pool` list; `Copies Per Card = 2` gives each player a 10-card deck.

## Verified Working
- **Board visuals** (session 9), **AI turn logic** (session 10), **opponent board visualization** (session 11), **shuffled 10-card decks + 5-card draw + opponent hand display** (session 12) — all previously confirmed, unchanged this session.
- **Asymmetric opening hands (this session)**: Confirmed on screen and via Console — Player One (first) drew exactly 3 cards; Player Two (second) drew 4 cards plus gained The Coin directly (`"Player Two gained The Coin. Hand size: 5"`), for 5 total. Matches real Hearthstone's 3-vs-4(+Coin) rule exactly.
- **The Coin mechanic, full chain (this session)**: Confirmed via Console — AI played The Coin (`targetsSelf` correctly routed the effect at itself, not the opponent), `GainManaEffect` fired (`"Gained 1 mana"`), `Target.GainMana()` correctly bumped `CurrentMana` from 0 back to 1, all within the same turn, and the bonus did not persist into the following turn (consistent with `TurnManager.RefillMana()` resetting `CurrentMana` each turn regardless).
- **Multi-card AI turn, finally observed in real play (this session)**: The Coin gave the AI a legitimate reason to chain two plays in one turn — `"Player Two played The Coin... Gained 1 mana... Player Two played Wisp... Wisp summoned to Player Two's board."` all inside one `--- Player Two (AI) is taking its turn ---` block. This closes out the long-standing "not yet tested" item from sessions 10–12 — `AIController.TakeTurn()`'s re-scan loop is now confirmed correct in actual play, not just by code review.

## Current Blocker / Last Thing Worked On
None — session ended on a strong, fully-verified milestone (Hearthstone-accurate opening hands, working Coin, and a real multi-card AI turn observed for the first time).

**Explicitly deferred / not done this session:**
- **Mulligan system** — researched Hearthstone's actual rules this session (any number of cards, 0 to all, no fixed cap; mulliganed cards shuffle back into the deck before replacements are drawn) and confirmed scope with Craig (no cap, matching real rules). **`PlayerHand.MulliganCard(CardData)` was designed/drafted in conversation but not yet confirmed as pasted into the project** — worth double-checking it's actually in `PlayerHand.cs` next session, since the asymmetric-hands work took priority instead. The click-to-mulligan UI and AI auto-mulligan strategy are still fully unbuilt.
- Turn order is currently fixed (Player One always goes first, Player Two always goes second) — no coin-flip/random determination of who goes first exists yet. Not required by anything discussed so far, just noting it as a gap versus real Hearthstone.

**Cosmetic, still low priority:**
- The known `BoardPanel`/`OpponentBoardPanel` text-clipping issue (minion name cut off at the left edge) is still unaddressed.
- Board panel vertical spacing could still use a bit more separation for clarity.

**Also worth double-checking next session:** confirm `TestCard_Goblin`'s Mana Cost is holding at `2` (flip-flopped a couple of times across recent sessions — hasn't been re-checked since session 12).

## Lessons Learned / Gotchas (useful to remember)
**Assembly definitions (.asmdef)**
- Circular references are rejected by Unity — keep dependency direction one-way (`Cards → Effects → Core`; `Core` and `Effects` never reference `Cards`).
- An asmdef's real identity is its **Name** field (Inspector), not its filename.
- `CS0246` errors can mean either a missing asmdef reference OR a missing `using` statement — check both.
- When a script in one assembly calls into a type from another assembly, BOTH the `using` directive AND an explicit **Assembly Definition Reference** on the calling assembly's `.asmdef` are needed.

**Unity Editor / UI basics**
- Script filename must exactly match its class name.
- Scene changes aren't saved until `Ctrl+S`.
- Always confirm the Inspector is showing the GameObject you actually mean to edit.
- For stretch-anchored panels, reposition via the Rect Transform's `Pos Y`/`Left`/`Right`/`Height` fields, not by freely dragging in Scene view.
- TMP Auto Size won't stop wrapping if placeholder text itself is too long for the box.
- A circular gizmo appearing unexpectedly in Scene view is likely just the `Global Light 2D` gizmo overlapping your selection — editor-only, not a bug.
- Editing a runtime `(Clone)` GameObject in Play mode is temporary — use Prefab Edit Mode for permanent changes.
- An empty/unassigned Inspector field for a listener-style hookup fails **silently** — no error, just inert behavior.
- Editing a TMP label's `Text Input` box requires clicking in, changing the text, then clicking away to commit — the Hierarchy GameObject's name does NOT reflect the label text.
- When reusing an existing prefab reference for a duplicated controller, check the *original* controller's Inspector for exactly which asset is assigned rather than re-dragging from memory.
- **New this session**: `CS0102` ("type already contains a definition for X") after a manual paste-in almost always means a `[Header(...)]` block or field got accidentally duplicated during copy-paste rather than cleanly appended — check for a repeated field/header pair near the reported line number first, before assuming a deeper problem. This is part of why full-file pastes are now the standard going forward (see Working Preferences) — it removes this whole class of manual-merge mistake.

**Core architecture principle**
- Keep `Core` types generic and unaware of `CardData`.
- AI-ness isn't a flag on `Player` — `AIController` is simply told which `PlayerHand` it controls at construction time.
- When a display/rendering component is already written generically, the cheapest way to support "two of something" is usually a second *instance* of the same component, not a code change to juggle two lists.
- A callback-based click handler using the null-conditional operator (`onClicked?.Invoke(card)`) gives "free" read-only-mode support — passing `null` instead of a real callback makes the UI element inert with zero special-casing needed.
- `ScriptableObject`-based cards support "multiple copies in a deck" for free by referencing the same asset multiple times in a `List<CardData>`.
- Use an in-place Fisher-Yates shuffle for randomizing a `List<T>`, not a naive "pick random index and remove" approach.
- **New this session**: a boolean flag on the data itself (`CardData.targetsSelf`) is the right way to encode "who does this affect," rather than special-casing by card name or effect type anywhere in game logic — keeps the data-driven philosophy consistent even as card behaviors diversify (damage vs. buff vs. self-effect).
- **New this session**: a temporary/this-turn-only stat bonus (like the Coin's mana) needs no explicit expiry/cleanup code if it modifies the same field that some other system already resets unconditionally each cycle (here, `TurnManager.RefillMana()` resetting `CurrentMana = MaxMana` every turn) — worth checking for this kind of "natural expiry" opportunity before writing bespoke duration-tracking logic for future temporary effects.

**Git / GitHub / environment**
- GitHub requires `gh auth login` or a PAT (repo scope only) for HTTPS git auth.
- `.gitignore` should exclude `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`.
- Unity's crash-recovery prompt (`Assets/_Recovery/`) is safe to accept; delete the folder after.
- .NET SDK (install via Microsoft's apt feed, not Ubuntu's default repo) is only needed for VS Code's C# IntelliSense/debugging.

## Next Steps (in order)
1. Confirm `PlayerHand.MulliganCard(CardData)` is actually pasted into the project (drafted this session, may not have made it in before the asymmetric-hands work took priority).
2. Build the click-to-mulligan UI (separate phase before turn 1) for the human player, plus a simple AI auto-mulligan strategy.
3. Re-confirm `TestCard_Goblin`'s Mana Cost is `2`.
4. Adjust vertical spacing between the board panels; investigate the minion-text-clipping issue.
5. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
6. Consider upgrading click-to-play to real drag-and-drop, if desired.

## Git Habits Being Followed
- Simple commit template: `git add .` / `git commit -m "short one-line summary"` / `git push`
- Push regularly to `github.com/craig-middleton/hearthClone`
