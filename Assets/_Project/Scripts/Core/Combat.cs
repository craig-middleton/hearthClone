using System.Collections.Generic;

namespace HearthstoneClone.Core
{
    public static class Combat
    {
        public static bool TryAttack(Minion attacker, Target target, Board board, out string failReason)
        {
            if (attacker == null || target == null)
            {
                failReason = "Attack rejected: attacker or target was null.";
                return false;
            }

            if (board == null)
            {
                failReason = "Attack rejected: board was null.";
                return false;
            }

            if (target.TargetPlayer == null && target.TargetMinion == null)
            {
                failReason = "Attack rejected: target had neither a player nor a minion.";
                return false;
            }

            // The defending player is derived from the target, never passed in. A caller
            // cannot hand this method the wrong side of the board, so the Taunt rule below
            // is checked against the actual owner of whatever is being attacked.
            Player defender = target.TargetPlayer != null
                ? target.TargetPlayer
                : board.GetOwnerOf(target.TargetMinion);

            if (attacker.IsDead)
            {
                failReason = $"{attacker.MinionName} is already dead and cannot attack.";
                return false;
            }

            if (target.TargetMinion != null && target.TargetMinion.IsDead)
            {
                failReason = $"{target.TargetMinion.MinionName} is already dead and cannot be attacked.";
                return false;
            }

            if (defender == null)
            {
                failReason = "Target minion is not on the board.";
                return false;
            }

            if (board.GetOwnerOf(attacker) == defender)
            {
                failReason = "Cannot attack your own side of the board.";
                return false;
            }

            List<Minion> defenderTaunts = board.GetTauntMinions(defender);
            if (defenderTaunts.Count > 0)
            {
                bool targetingTaunt = target.TargetMinion != null && defenderTaunts.Contains(target.TargetMinion);
                if (!targetingTaunt)
                {
                    failReason = $"{defender.PlayerName} has a Taunt minion — you must attack it first.";
                    return false;
                }
            }

            if (!attacker.CanAttack)
            {
                failReason = attacker.IsFrozen
                    ? $"{attacker.MinionName} is frozen and cannot attack."
                    : attacker.HasSummoningSickness
                        ? $"{attacker.MinionName} has summoning sickness and cannot attack yet."
                        : $"{attacker.MinionName} has already attacked this turn.";
                return false;
            }

            target.TakeDamage(attacker.CurrentAttack);

            if (target.TargetMinion != null)
            {
                attacker.TakeDamage(target.TargetMinion.CurrentAttack);
            }

            attacker.HasAttackedThisTurn = true;
            failReason = null;
            return true;
        }
    }
}