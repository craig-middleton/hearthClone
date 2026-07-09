using UnityEngine;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;

namespace HearthstoneClone.Cards
{
    public class EffectTester : MonoBehaviour
    {
        public CardData cardToTest;

        void Start()
        {
            if (cardToTest == null || cardToTest.onPlayEffect == null)
            {
                Debug.LogWarning("No card or effect assigned to EffectTester.");
                return;
            }

            var context = new GameContext();
            var target = new Target { CurrentHealth = 10 };

            Debug.Log($"Playing card: {cardToTest.cardName}. Target starting health: {target.CurrentHealth}");
            cardToTest.onPlayEffect.Execute(context, target);
        }
    }
}