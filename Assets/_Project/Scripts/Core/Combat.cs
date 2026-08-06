namespace HearthstoneClone.Core
{
    public static class Combat
    {
        public static bool TryAttack(Minion attacker, Target target, out string failReason)
        {
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