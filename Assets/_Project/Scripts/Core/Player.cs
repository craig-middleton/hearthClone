using System.Collections.Generic;

namespace HearthstoneClone.Core
{
    public class Player
    {
        public string PlayerName;
        public int Health;
        public int MaxHealth;
        public int CurrentMana;
        public int MaxMana;
        public List<Minion> BoardMinions = new List<Minion>();
        public int FatigueDamage = 0;
        public bool HasUsedHeroPowerThisTurn = false;

        public Player(string playerName, int startingHealth = 30)
        {
            PlayerName = playerName;
            Health = startingHealth;
            MaxHealth = startingHealth;
        }

        public void TakeDamage(int amount)
        {
            Health -= amount;
        }
    }
}