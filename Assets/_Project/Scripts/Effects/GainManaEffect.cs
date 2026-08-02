using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.Effects
{
    [CreateAssetMenu(fileName = "GainManaEffect", menuName = "Effects/Gain Mana")]
    public class GainManaEffect : CardEffect
    {
        public int manaAmount = 1;

        public override void Execute(GameContext context, Target target)
        {
            target.GainMana(manaAmount);
            Debug.Log($"Gained {manaAmount} mana.");
        }
    }
}