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
    }
}
