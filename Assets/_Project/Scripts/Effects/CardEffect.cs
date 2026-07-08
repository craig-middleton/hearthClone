using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.Effects
{
    public abstract class CardEffect : ScriptableObject
    {
        public abstract void Execute(GameContext context, Target target);
    }
}