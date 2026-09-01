using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.Effects
{
    [CreateAssetMenu(fileName = "FrostDamageEffect", menuName = "Effects/Frost Damage")]
    public class FrostDamageEffect : CardEffect
    {
        public int damageAmount = 1;

        public override void Execute(GameContext context, Target target)
        {
            target.TakeDamage(damageAmount);
            target.TargetMinion?.Freeze();
            Debug.Log($"Dealt {damageAmount} frost damage to target. Remaining health: {target.GetCurrentHealth()}");
        }
    }
}
