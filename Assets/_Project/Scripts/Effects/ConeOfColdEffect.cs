using System.Collections.Generic;
using HearthstoneClone.Core;
using UnityEngine;

namespace HearthstoneClone.Effects
{
    [CreateAssetMenu(fileName = "ConeOfCold", menuName = "Effects/Cone of Cold")]
    public class ConeOfColdEffect : CardEffect, IDamageEffect, IAreaDamageEffect
    {
        [Min(0)] public int damageAmount = 1;
        public int DamageAmount => damageAmount;

        public IReadOnlyList<Minion> GetAffectedMinions(GameContext context, Target target, Player caster)
        {
            var result = new List<Minion>();
            var selected = target?.TargetMinion;
            if (selected == null || selected.IsDead || context?.Board == null) return result;
            var owner = context.Board.GetOwnerOf(selected);
            if (owner == null) return result;
            var living = owner.BoardMinions.FindAll(m => m != null && !m.IsDead);
            int center = living.IndexOf(selected);
            if (center < 0) return result;
            for (int i = Mathf.Max(0, center - 1); i <= Mathf.Min(living.Count - 1, center + 1); i++)
                result.Add(living[i]);
            return result;
        }

        public override void Execute(GameContext context, Target target, Player caster)
        {
            // Snapshot adjacency before damage; killing the middle minion must not change
            // which neighbours this same cast hits. Friendly targets are valid, as on the card.
            foreach (var minion in GetAffectedMinions(context, target, caster))
            {
                minion.TakeDamage(damageAmount);
                minion.Freeze();
            }
        }
    }
}
