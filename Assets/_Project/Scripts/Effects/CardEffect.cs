using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.Effects
{
    public abstract class CardEffect : ScriptableObject
    {
        public abstract void Execute(GameContext context, Target target, Player caster);
    }

    // The single "this effect deals N damage to its target" classification. Every consumer
    // that cares whether an effect is damage checks this, never a concrete effect type:
    // CardDragResolver (damage flash + the lethal-hit view hold) and
    // AIController.FindLethalDamageTarget. A new damage effect implements this once and gets
    // all three - before it existed, Frostbolt was recognised by the AI but not by the
    // resolver, so it never flashed and a lethal Frostbolt got no hold.
    public interface IDamageEffect
    {
        int DamageAmount { get; }
    }
}