using System.Collections.Generic;
using UnityEngine;
using HearthstoneClone.Core;
using HearthstoneClone.Cards;

namespace HearthstoneClone.AI
{
    public class AIController
    {
        private readonly PlayerHand aiHand;
        private readonly GameContext context;
        private readonly Board board;

        public AIController(PlayerHand aiHand, GameContext context, Board board)
        {
            this.aiHand = aiHand;
            this.context = context;
            this.board = board;
        }

        public void PerformMulligan(int mulliganThreshold = 4)
        {
            var handSnapshot = new List<CardInstance>(aiHand.Hand);
            var toMulligan = new List<CardInstance>();

            foreach (var card in handSnapshot)
            {
                if (card == null) continue;

                if (card.Data.manaCost >= mulliganThreshold)
                {
                    toMulligan.Add(card);
                }
            }

            aiHand.MulliganCards(toMulligan);

            Debug.Log($"{aiHand.CorePlayer.PlayerName} (AI) completed mulligan.");
        }

        public void TakeTurn()
        {
            Player aiPlayer = aiHand.CorePlayer;
            Player opponent = board.GetOpponent(aiPlayer);

            Debug.Log($"--- {aiPlayer.PlayerName} (AI) is taking its turn ---");

            bool playedSomething = true;
            while (playedSomething)
            {
                playedSomething = false;

                foreach (var card in new List<CardInstance>(aiHand.Hand))
                {
                    if (card == null) continue;
                    if (card.Data.manaCost > aiPlayer.CurrentMana)
                        continue;

                    Target target = null;
                    if (card.Data.onPlayEffect != null)
                    {
                        if (card.Data.targetRequirement == TargetRequirement.Self)
                        {
                            target = new Target(aiPlayer);
                        }
                        else if (card.Data.targetRequirement == TargetRequirement.AnyMinion)
                        {
                            // No friendly minion to buff - skip this card rather than burning
                            // mana on a play whose target ends up null (GrowthEffect no-ops
                            // without a Minion target, same guard as a human dropping it on a
                            // face - see GrowthEffect.Execute).
                            if (aiPlayer.BoardMinions.Count == 0) continue;
                            target = new Target(aiPlayer.BoardMinions[0]);
                        }
                        else if (card.Data.targetRequirement == TargetRequirement.Friendly)
                        {
                            // A valid Friendly target always exists (the caster's own face, if
                            // nothing else), so this never actually skips today - but prefers a
                            // friendly minion over the AI's own face, matching AnyMinion's
                            // preference above.
                            target = aiPlayer.BoardMinions.Count > 0
                                ? new Target(aiPlayer.BoardMinions[0])
                                : new Target(aiPlayer);
                        }
                        else
                        {
                            target = new Target(opponent);
                        }
                    }

                    if (aiHand.PlayCard(card, context, target))
                    {
                        playedSomething = true;
                        break;
                    }
                }
            }

            board.RemoveDeadMinions();

            bool lethalAvailable = false;
            if (board.GetTauntMinions(opponent).Count == 0)
            {
                int unblockedDamage = 0;
                foreach (var minion in aiPlayer.BoardMinions)
                {
                    if (minion == null || !minion.CanAttack) continue;
                    unblockedDamage += minion.CurrentAttack;
                }
                lethalAvailable = unblockedDamage >= opponent.Health;
            }

            foreach (var minion in new List<Minion>(aiPlayer.BoardMinions))
            {
                if (opponent.Health <= 0) break;
                if (minion == null || !minion.CanAttack) continue;

                var opponentTaunts = board.GetTauntMinions(opponent);
                Target attackTarget;
                if (opponentTaunts.Count > 0)
                {
                    attackTarget = new Target(opponentTaunts[0]);
                }
                else if (lethalAvailable)
                {
                    attackTarget = new Target(opponent);
                }
                else
                {
                    Minion favorableTarget = null;
                    foreach (var enemyMinion in opponent.BoardMinions)
                    {
                        if (enemyMinion == null) continue;
                        if (AICombatEvaluator.IsFavorableTrade(minion, enemyMinion))
                        {
                            favorableTarget = enemyMinion;
                            break;
                        }
                    }

                    // Safety net for future candidate-selection logic (lethal override, board-state
                    // reasoning): never let a genuinely bad trade through, even if some later step
                    // picks favorableTarget by means other than IsFavorableTrade above.
                    if (favorableTarget != null && AICombatEvaluator.IsUnfavorableTrade(minion, favorableTarget))
                    {
                        favorableTarget = null;
                    }

                    attackTarget = favorableTarget != null
                        ? new Target(favorableTarget)
                        : new Target(opponent);
                }

                if (!Combat.TryAttack(minion, attackTarget, board, out string failReason))
                {
                    Debug.Log($"{aiPlayer.PlayerName} (AI) attack failed: {failReason}");
                }
                board.RemoveDeadMinions();
            }

            Debug.Log($"--- {aiPlayer.PlayerName} (AI) ends its turn ---");
        }
    }
}