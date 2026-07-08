using UnityEngine;

namespace HearthstoneClone.Cards
{
    public enum CardType
    {
        Minion,
        Spell
    }

    [CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public string cardName;
        [TextArea] public string description;
        public Sprite artwork;

        [Header("Stats")]
        public int manaCost;
        public CardType cardType;

        [Header("Minion Stats (ignored for Spells)")]
        public int attack;
        public int health;
    }
}