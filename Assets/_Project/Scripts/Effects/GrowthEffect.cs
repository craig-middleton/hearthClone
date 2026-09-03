using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.Effects
{
    [CreateAssetMenu(fileName = "GrowthEffect", menuName = "Effects/Growth")]
    public class GrowthEffect : CardEffect
    {
        public int attackBoost = 3;
        public int healthBoost = 3;

        public override void Execute(GameContext context, Target target, Player caster)
        {
            if (target.TargetMinion == null)
            {
                Debug.LogWarning("GrowthEffect: target is not a minion — effect skipped.");
                return;
            }

            Minion minion = target.TargetMinion;
            minion.CurrentAttack += attackBoost;
            minion.MaxHealth += healthBoost;
            minion.CurrentHealth += healthBoost;

            Debug.Log($"{minion.MinionName} grew by +{attackBoost}/+{healthBoost}. Now {minion.CurrentAttack}/{minion.CurrentHealth}.");
        }
    }
}
