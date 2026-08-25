using HearthstoneClone.Core;

namespace HearthstoneClone.AI
{
    public static class AICombatEvaluator
    {
        public static bool IsFavorableTrade(Minion attacker, Minion target)
        {
            if (attacker == null || target == null) return false;

            bool kills = attacker.CurrentAttack >= target.CurrentHealth;
            bool survives = target.CurrentAttack < attacker.CurrentHealth;

            return kills && survives;
        }
    }
}
