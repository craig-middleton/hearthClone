namespace HearthstoneClone.Core
{
    // Represents something an effect can act on: either a Player or a Minion.
    public class Target
    {
        public Player TargetPlayer;
        public Minion TargetMinion;

        public Target(Player player)
        {
            TargetPlayer = player;
        }

        public Target(Minion minion)
        {
            TargetMinion = minion;
        }

        public void TakeDamage(int amount)
        {
            if (TargetPlayer != null) TargetPlayer.TakeDamage(amount);
            else if (TargetMinion != null) TargetMinion.TakeDamage(amount);
        }

        public int GetCurrentHealth()
        {
            if (TargetPlayer != null) return TargetPlayer.Health;
            if (TargetMinion != null) return TargetMinion.CurrentHealth;
            return 0;
        }

        public void GainMana(int amount)
        {
            if (TargetPlayer != null)
            {
                // Capped at the same absolute ceiling TurnManager refills mana up to, so a
                // temporary boost (e.g. The Coin) can't push CurrentMana past what the game
                // ever allows a mana crystal count to reach.
                TargetPlayer.CurrentMana = System.Math.Min(TargetPlayer.CurrentMana + amount, TurnManager.MaxManaCap);
            }
        }
    }
}