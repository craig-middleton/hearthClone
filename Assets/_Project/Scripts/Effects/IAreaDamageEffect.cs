using System.Collections.Generic;
using HearthstoneClone.Core;

namespace HearthstoneClone.Effects
{
    // Pure preview of exactly which living minions this cast affects. Both Execute and
    // presentation use it, so adjacency and caster-relative ownership have one definition.
    // Area-only effects do not implement IDamageEffect, which classifies chosen-target damage.
    public interface IAreaDamageEffect
    {
        int DamageAmount { get; }
        IReadOnlyList<Minion> GetAffectedMinions(GameContext context, Target target, Player caster);
    }
}
