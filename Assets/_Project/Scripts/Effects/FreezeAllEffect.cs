using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.Effects
{
    [CreateAssetMenu(fileName = "FreezeAllEffect", menuName = "Effects/Freeze All")]
    public class FreezeAllEffect : CardEffect
    {
        public override void Execute(GameContext context, Target target, Player caster)
        {
            Player opponent = context.Board.GetOpponent(caster);

            foreach (Minion minion in opponent.BoardMinions)
            {
                minion.Freeze();
            }

            Debug.Log($"Froze {opponent.BoardMinions.Count} of {opponent.PlayerName}'s minions.");
        }
    }
}
