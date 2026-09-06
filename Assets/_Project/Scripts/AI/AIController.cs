using System.Collections.Generic;
using UnityEngine;
using HearthstoneClone.Core;
using HearthstoneClone.Cards;
using HearthstoneClone.Effects;

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
                        target = SelectEffectTarget(card.Data, aiPlayer, opponent);
                        if (target == null && card.Data.targetRequirement == TargetRequirement.AnyMinion)
                            continue;
                    }

                    if (aiHand.PlayCard(card, context, target))
                    {
                        playedSomething = true;
                        break;
                    }
                }
            }

            board.RemoveDeadMinions();

            AttackPhase(aiPlayer, opponent);

            Debug.Log($"--- {aiPlayer.PlayerName} (AI) ends its turn ---");
        }

        // Picks a Target for a card's onPlayEffect by TargetRequirement. Mirrors
        // CardDragResolver.ResolveCardDrag's per-TargetRequirement switch (Constraint 26) -
        // that one decides whether a human's drop is a *valid* target, this one decides which
        // *valid* target the AI prefers. Any new TargetRequirement case needs to be added to
        // both places.
        private Target SelectEffectTarget(CardData cardData, Player aiPlayer, Player opponent)
        {
            if (cardData.targetRequirement == TargetRequirement.Self)
            {
                return new Target(aiPlayer);
            }
            else if (cardData.targetRequirement == TargetRequirement.AnyMinion)
            {
                // No friendly minion to buff - return null so the caller skips this card rather
                // than burning mana on a play whose target ends up null (GrowthEffect no-ops
                // without a Minion target, same guard as a human dropping it on a face - see
                // GrowthEffect.Execute).
                if (aiPlayer.BoardMinions.Count == 0) return null;
                return new Target(aiPlayer.BoardMinions[0]);
            }
            else if (cardData.targetRequirement == TargetRequirement.Friendly)
            {
                // A valid Friendly target always exists (the caster's own face, if nothing
                // else), so this never actually skips today - but prefers a friendly minion
                // over the AI's own face, matching AnyMinion's preference above.
                return aiPlayer.BoardMinions.Count > 0
                    ? new Target(aiPlayer.BoardMinions[0])
                    : new Target(aiPlayer);
            }
            else if (cardData.targetRequirement == TargetRequirement.Any)
            {
                // Prefer a kill over chip damage to the face: if this spell's damage would
                // finish off an enemy minion, take the minion instead of defaulting to face.
                // Simple "can I get a kill" check, no wider value scoring - mirrors the
                // lethal-check pattern already used in AttackPhase below.
                Minion killTarget = FindLethalDamageTarget(cardData, opponent);
                if (killTarget != null) return new Target(killTarget);

                return new Target(opponent);
            }
            else
            {
                return new Target(opponent);
            }
        }

        // Returns an enemy minion that a damage-dealing onPlayEffect (DealDamageEffect or
        // FrostDamageEffect) would kill outright, or null if none exists or the effect isn't a
        // recognized damage type. Reads the effect's damageAmount field directly rather than
        // simulating Execute, since both damage effect types expose the same public field name.
        private Minion FindLethalDamageTarget(CardData cardData, Player opponent)
        {
            int damageAmount;
            if (cardData.onPlayEffect is DealDamageEffect dealDamage)
                damageAmount = dealDamage.damageAmount;
            else if (cardData.onPlayEffect is FrostDamageEffect frostDamage)
                damageAmount = frostDamage.damageAmount;
            else
                return null;

            foreach (var enemyMinion in opponent.BoardMinions)
            {
                if (enemyMinion == null) continue;
                if (damageAmount >= enemyMinion.CurrentHealth) return enemyMinion;
            }

            return null;
        }

        private void AttackPhase(Player aiPlayer, Player opponent)
        {
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

                Target attackTarget = SelectAttackTarget(minion, opponent, lethalAvailable);

                if (!Combat.TryAttack(minion, attackTarget, board, out string failReason))
                {
                    Debug.Log($"{aiPlayer.PlayerName} (AI) attack failed: {failReason}");
                }
                board.RemoveDeadMinions();
            }
        }

        private Target SelectAttackTarget(Minion minion, Player opponent, bool lethalAvailable)
        {
            var opponentTaunts = board.GetTauntMinions(opponent);
            if (opponentTaunts.Count > 0)
            {
                return new Target(opponentTaunts[0]);
            }
            else if (lethalAvailable)
            {
                return new Target(opponent);
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

                return favorableTarget != null
                    ? new Target(favorableTarget)
                    : new Target(opponent);
            }
        }
    }
}