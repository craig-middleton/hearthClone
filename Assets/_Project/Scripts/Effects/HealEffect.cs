using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.Effects
{
    [CreateAssetMenu(fileName = "HealEffect", menuName = "Effects/Heal")]
    public class HealEffect : CardEffect
    {
        public int healAmount = 6;

        public override void Execute(GameContext context, Target target, Player caster)
        {
            // Authoritative "never heal the opponent's face" enforcement. This can't be a
            // UI-only check (CardDragResolver) because AIController builds Target objects
            // directly and calls PlayerHand.PlayCard without going through the drag resolver
            // at all - this Execute() guard is the one chokepoint both paths share.
            if (target.TargetPlayer != null && target.TargetPlayer != caster)
            {
                Debug.LogWarning("HealEffect: target is the opponent's face — effect skipped.");
                return;
            }

            if (target.TargetMinion != null)
            {
                Minion minion = target.TargetMinion;
                minion.CurrentHealth = Mathf.Min(minion.CurrentHealth + healAmount, minion.MaxHealth);
                Debug.Log($"Healed {minion.MinionName} for {healAmount}. Now {minion.CurrentHealth}/{minion.MaxHealth}.");
            }
            else if (target.TargetPlayer != null)
            {
                Player player = target.TargetPlayer;
                player.Health = Mathf.Min(player.Health + healAmount, player.MaxHealth);
                Debug.Log($"Healed {player.PlayerName} for {healAmount}. Now {player.Health}/{player.MaxHealth}.");
            }
        }
    }
}
