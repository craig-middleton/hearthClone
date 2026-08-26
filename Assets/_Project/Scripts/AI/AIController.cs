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
            var handSnapshot = new List<CardData>(aiHand.Hand);
            var toMulligan = new List<CardData>();

            foreach (var card in handSnapshot)
            {
                if (card == null) continue;

                if (card.manaCost >= mulliganThreshold)
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

                foreach (var card in new List<CardData>(aiHand.Hand))
                {
                    if (card == null) continue;
                    if (card.manaCost > aiPlayer.CurrentMana)
                        continue;

                    Target target = null;
                    if (card.onPlayEffect != null)
                    {
                        target = card.targetRequirement == TargetRequirement.Self ? new Target(aiPlayer) : new Target(opponent);
                    }

                    if (aiHand.PlayCard(card, context, target))
                    {
                        playedSomething = true;
                        break;
                    }
                }
            }

            board.RemoveDeadMinions();

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