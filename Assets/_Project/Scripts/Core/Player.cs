using System.Collections.Generic;

namespace HearthstoneClone.Core
{
    public class Player
    {
        public string PlayerName;
        public int Health = 30;
        public int CurrentMana;
        public int MaxMana;

        public List<Minion> BoardMinions = new List<Minion>();

        public Player(string playerName)
        {
            PlayerName = playerName;
        }

        public void TakeDamage(int amount)
        {
            Health -= amount;
        }
    }
}