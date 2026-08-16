using System.Collections.Generic;

namespace HearthstoneClone.Core
{
    public static class Combat
    {
        public static bool TryAttack(Minion attacker, Target target, Board board, Player defender, out string failReason)
        {
            if (attacker == null || target == null)
            {
                failReason = "Attack rejected: attacker or target was null.";
                return false;
            }

            if (board == null || defender == null)
            {
                failReason = "Attack rejected: board or defending player was null.";
                return false;
            }

            if (target.TargetPlayer == null && target.TargetMinion == null)
            {
                failReason = "Attack rejected: target had neither a player nor a minion.";
                return false;
            }

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
                failReason = attacker.HasSummoningSickness
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