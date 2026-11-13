using System.Collections.Generic;
using HearthstoneClone.Core;
using UnityEngine;

namespace HearthstoneClone.Effects
{
    [CreateAssetMenu(fileName = "EnemyBoardDamage", menuName = "Effects/Enemy Board Damage")]
    public class EnemyBoardDamageEffect : CardEffect, IAreaDamageEffect
    {
        [Min(0)] public int damageAmount = 1;
        public int DamageAmount => damageAmount;

        public IReadOnlyList<Minion> GetAffectedMinions(GameContext context, Target target, Player caster)
        {
            if (context?.Board == null || caster == null) return new List<Minion>();
            return context.Board.GetOpponent(caster).BoardMinions.FindAll(m => m != null && !m.IsDead);
        }

        public override void Execute(GameContext context, Target target, Player caster)
        {
            foreach (var minion in GetAffectedMinions(context, target, caster))
                minion.TakeDamage(damageAmount);
        }
    }
}
