# HearthstoneClone — Networking Prep Checklist

> Checklist for the networked 2-player phase (two separate devices). An inventory of what exists today, not a design.
> Taken from the post-overhaul audit's Area 3 and re-checked against the source on 2026-09-23 (commit `2048486`). Line references go stale; the method names are the anchors.
> Working assumption: **one host is authoritative**. Clients send action requests; the host validates them, mutates state, and broadcasts the results.

## 1. Prerequisites (needed before anything can go over the wire)

- [ ] **Stable IDs for `CardInstance` and `Minion`.** Both use object-reference identity only. That's correct locally (see `CardInstance.cs`, `Board.GetOwnerOf`), but a reference can't be sent to another device.
- [ ] **ID-based `Target`.** `Target` holds `Player`/`Minion` object references. It needs a wire form: a seat, or a minion ID.
- [ ] **Card-ID registry for `CardData`.** `CardData` is a `ScriptableObject` asset reference, so the devices need a shared ID → asset lookup (for example, keyed by asset GUID or an explicit id field). ⚠️ Adding a field to `CardData` triggers Constraint 12's re-serialization hazard.
- [ ] **Shuffle RNG.** `PlayerHand.Shuffle` uses the global `UnityEngine.Random`, which `EffectTester.Start` seeds from the clock. Make it host-only (clients never shuffle), or use a shared seed. The background and music picks (`SetRandomBoardBackground`/`SetRandomMusic`) can stay local.
- [ ] **Hidden information.** Each device currently builds both `PlayerHand`s in full, including the opponent's `Hand` and `Deck` contents. `faceDown` only hides the view ("hiding something from the view doesn't remove it from state"). A client must receive only the opponent's hand *count*, and must not receive either deck's order.
- [ ] **Game rules out of the UI assembly.** `GameManager` (in `Scripts/UI/`) owns the turn flow, the Hero Power rule, draws and the win check. The host needs these without any UI.

## 2. Every place UI/input mutates game state today

| # | File / method | State changed | Action-request seam needed |
|---|---|---|---|
| 1 | `CardDragResolver.ResolveCardDrag` → `PlayerHand.PlayCard` | Mana, hand, minion creation, effect execution | `PlayCard{seat, cardId, target}`. The drop classification stays on the client; the host re-validates the `TargetRequirement`, the turn and the dead-target check. The `None`/`Self` default target must come from `CardTargeting.DefaultTarget` on the host |
| 2 | `CardDragResolver.OnCardDragEnd` / `OnOpponentCardDragEnd` | Hard-wired P1/P2 split | One handler for the local seat |
| 3 | `CombatInputController.ResolveAttack` → `Combat.TryAttack` | Both health values, `HasAttackedThisTurn` | `Attack{seat, attackerId, target}`. `selectedAttacker` stays client-only UI state |
| 4 | `GameManager.OnHeroPowerClicked` | Mana, `HasUsedHeroPowerThisTurn`, opponent health (rule written inline, cost in `HeroPowerCost`) | `HeroPower{seat}`. Move the rule to the host side |
| 5 | `GameManager.OnEndTurnClicked` | `TurnManager.EndTurn` (turn, mana, minion resets, Hero Power reset), `DrawForCurrentPlayer`, the AI turn, `ClearSelection` | `EndTurn{seat}`. The host checks seat == `CurrentPlayer`; **there's no such check today**. The draw runs on the host, and the AI branch is removed in PvP |
| 6 | `GameManager.BeginFirstTurn` (called from `EffectTester.OnMulliganComplete`) | Player One's turn-1 draw (via `DrawForCurrentPlayer`) | Runs on the host once **both** seats' mulligans are confirmed, not on one client's mulligan callback |
| 7 | `GameManager.AfterGameAction` | `board.RemoveDeadMinions()`, inside what is otherwise a refresh hook | The sweep runs on the host after each resolved action; clients only refresh |
| 8 | `GameManager.CheckWinCondition` | `GameOver`, `winner` | Host only; broadcast a GameOver event |
| 9 | `MulliganController.OnConfirmMulliganClicked` → `PlayerHand.MulliganCards` | Hand, deck, shuffle | `Mulligan{seat, cardIds}`. Both seats mulligan at the same time, and the game starts only when both have confirmed |
| 10 | `MulliganController.ShowMulliganUI` (unwired-UI fallback) | Sets `MulliganComplete` and fires `onMulliganComplete` | Must not bypass the host's mulligan gate |
| 11 | `EffectTester.BeginNewGame` | The whole model: decks, `Shuffle`, opening draws (3 / 4), The Coin, `aiController.PerformMulligan()` for P2 (**runs even under `manualControlMode`**), `turnManager.StartGame()` | Only the host creates a game; clients receive the initial state minus hidden cards |
| 12 | `EffectTester.OnPlayAgainClicked` → `BeginNewGame` | Full rebuild | `Rematch` requires both players' consent |
| 13 | `AIController.TakeTurn` / `PerformMulligan` | Everything, directly | Host only, or disabled in PvP |

## 3. Cross-cutting items

- [ ] **`manualControlMode` becomes one "local seat" value.** It's read in 12 places, 9 of them decisions: `CombatInputController` ×3 (`OnMinionClicked` ×2, `OnFaceClicked`), `CardDragResolver.CanPlayerTwoDrag`, `GameManager` ×2 (`OnHeroPowerClicked`, and the AI branch in `OnEndTurnClicked`), and `EffectTester` ×3 (the opponent hand's `faceDown`/`fanInteractive`, and `showAttackEligibility`). The other 3 are the lambdas that pass it into the controllers. Every one of them really means "this device controls Player Two". Each device drives exactly one seat, and on device 2 the "opponent" displays show Player One. (Constraint 31 already warns that the opponent-side views are dual-purpose.)
- [ ] **Turn ownership isn't enforced at the chokepoint.** Neither `PlayerHand.PlayCard` nor `Combat.TryAttack` checks that it's the acting player's turn; only the UI gates do (`CanPlayerOneDrag`/`CanPlayerTwoDrag`, the `OnMinionClicked`/`OnFaceClicked` checks) plus when the AI gets invoked. The host must check it (the Constraint 26 lesson, applied to turn order).
- [ ] **Spell animation is driven from the input path.** `CardDragResolver.TriggerSpellAnimation` runs only for local human drags, using view positions sampled before `PlayCard`. AI plays never animate today, and remote plays won't either. This needs event-driven animation: the host broadcasts something like `CardPlayed{source, target, school, shape}`, and each client animates it from its own views.
- [ ] **Refreshes can be lost mid-drag.** `RefreshHandDisplay` returns early while `CardDragResolver.DragInProgress` is true, and an invalid drop triggers no refresh afterwards. A remote event that arrives mid-drag would leave the hand stale, so queue a refresh for when the drag ends.
- [ ] **Every draw runs on the host.** That covers the draws in `OnEndTurnClicked`, `BeginFirstTurn`, the opening hands and mulligan redraws. Fatigue and hand-size burns come along with them.
- [ ] **Give The Coin AFTER the mulligan.** `BeginNewGame` currently calls `AddCardToHand(coinCard)` before `PerformMulligan()`. That's harmless against the AI (its `manaCost >= 4` threshold never returns the 0-cost Coin), but a human Player Two would see The Coin on their mulligan screen.
- [ ] **The opponent's mana and deck count aren't shown.** `FaceView`'s crystal row and deck pile are player-only through Inspector wiring. A human opponent's mana and deck count will probably need showing (this touches `FaceView.SetPlayer`'s parameters).
