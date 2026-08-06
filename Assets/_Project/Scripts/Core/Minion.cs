namespace HearthstoneClone.Core
{
    // A runtime instance of a minion on the board.
    // Core knows nothing about CardData - Cards will provide
    // a way to construct a Minion from a card asset.
    public class Minion
    {
        public string MinionName;
        public int CurrentAttack;
        public int CurrentHealth;
        public bool HasSummoningSickness = true;
        public bool HasAttackedThisTurn = false;

        public Minion(string minionName, int attack, int health)
        {
            MinionName = minionName;
            CurrentAttack = attack;
            CurrentHealth = health;
        }

        public void TakeDamage(int amount)
        {
            CurrentHealth -= amount;
        }

        public bool IsDead => CurrentHealth <= 0;

        public bool CanAttack => !HasSummoningSickness && !HasAttackedThisTurn;

        public void ResetForNewTurn()
        {
            HasSummoningSickness = false;
            HasAttackedThisTurn = false;
        }
    }
}