namespace HearthstoneClone.Core
{
    // Placeholder - will hold a real reference to Minion or Player once those exist
    public class Target
    {
        public bool IsPlayer;
        public int CurrentHealth;

        public void TakeDamage(int amount)
        {
            CurrentHealth -= amount;
        }
    }
}