using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.Effects
{
    [CreateAssetMenu(fileName = "GainManaEffect", menuName = "Effects/Gain Mana")]
    public class GainManaEffect : CardEffect
    {
        public int manaAmount = 1;

        public override void Execute(GameContext context, Target target, Player caster)
        {
            // Always the caster's mana, never whoever `target` names - the same way
            // FreezeAllEffect derives its opponent from the caster (Constraint 26). "Self" is
            // enforced here, at the chokepoint, not only by the callers that happen to pass
            // Target(caster) today.
            if (caster == null)
            {
                Debug.LogWarning("GainManaEffect: no caster — effect skipped.");
                return;
            }

            // Routed through Target.GainMana so the TurnManager.MaxManaCap clamp stays in one place.
            int manaBefore = caster.CurrentMana;
            new Target(caster).GainMana(manaAmount);
            int gained = caster.CurrentMana - manaBefore;

            if (gained > 0)
            {
                Debug.Log($"{caster.PlayerName} gained {gained} mana. Mana: {caster.CurrentMana}.");
            }
            else
            {
                Debug.Log($"{caster.PlayerName} gained no mana — already at the {TurnManager.MaxManaCap}-mana cap.");
            }
        }
    }
}
