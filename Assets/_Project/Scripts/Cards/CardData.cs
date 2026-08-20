using UnityEngine;
using HearthstoneClone.Effects;

namespace HearthstoneClone.Cards
{
    public enum CardType
    {
        Minion,
        Spell
    }

    // Controls how a dropped card resolves in EffectTester.ResolveCardDrag: None/Self play
    // immediately on any recognized drop zone, Any requires dropping on a MinionView/FaceView.
    public enum TargetRequirement
    {
        None,
        Self,
        Any
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
        public bool hasTaunt;

        [Header("Behavior")]
        public CardEffect onPlayEffect;
        public TargetRequirement targetRequirement;
    }
}