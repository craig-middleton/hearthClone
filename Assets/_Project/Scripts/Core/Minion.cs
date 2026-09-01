using UnityEngine;

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
        public bool HasTaunt;
        public bool IsFrozen;
        public bool IsNewlyFrozen;
        public Sprite Artwork;

        public Minion(string minionName, int attack, int health, bool hasTaunt = false, Sprite artwork = null)
        {
            MinionName = minionName;
            CurrentAttack = attack;
            CurrentHealth = health;
            HasTaunt = hasTaunt;
            Artwork = artwork;
        }

        public void TakeDamage(int amount)
        {
            CurrentHealth -= amount;
        }

        public bool IsDead => CurrentHealth <= 0;

        public bool CanAttack => !IsDead && !HasSummoningSickness && !HasAttackedThisTurn && !IsFrozen;

        public void Freeze()
        {
            IsFrozen = true;
            IsNewlyFrozen = true;
        }

        public void ResetForNewTurn()
        {
            HasSummoningSickness = false;
            HasAttackedThisTurn = false;

            if (IsFrozen)
            {
                if (IsNewlyFrozen)
                    IsNewlyFrozen = false;
                else
                    IsFrozen = false;
            }
        }
    }
}