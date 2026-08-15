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

            foreach (var card in handSnapshot)
            {
                if (card.manaCost >= mulliganThreshold)
                {
                    aiHand.MulliganCard(card);
                }
            }

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
                    if (card.manaCost > aiPlayer.CurrentMana)
                        continue;

                    Target target = null;
                    if (card.onPlayEffect != null)
                    {
                        target = card.targetsSelf ? new Target(aiPlayer) : new Target(opponent);
                    }

                    if (aiHand.PlayCard(card, context, target))
                    {
                        playedSomething = true;
                        break;
                    }
                }
            }

            foreach (var minion in new List<Minion>(aiPlayer.BoardMinions))
            {
                if (opponent.Health <= 0) break;
                if (!minion.CanAttack) continue;

                var opponentTaunts = board.GetTauntMinions(opponent);
                Target attackTarget = opponentTaunts.Count > 0
                    ? new Target(opponentTaunts[0])
                    : new Target(opponent);

                Combat.TryAttack(minion, attackTarget, out _);
                board.RemoveDeadMinions();
            }

            Debug.Log($"--- {aiPlayer.PlayerName} (AI) ends its turn ---");
        }
    }
}