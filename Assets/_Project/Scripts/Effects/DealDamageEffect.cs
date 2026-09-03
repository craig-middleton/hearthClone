using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.Effects
{
    [CreateAssetMenu(fileName = "DealDamageEffect", menuName = "Effects/Deal Damage")]
    public class DealDamageEffect : CardEffect
    {
        public int damageAmount = 1;

        public override void Execute(GameContext context, Target target, Player caster)
        {
            target.TakeDamage(damageAmount);
            Debug.Log($"Dealt {damageAmount} damage to target. Remaining health: {target.GetCurrentHealth()}");
        }
    }
}