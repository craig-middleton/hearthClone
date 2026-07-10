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

            var playerOne = new Player("Player One");
            var playerTwo = new Player("Player Two");
            var board = new Board(playerOne, playerTwo);
            var context = new GameContext(board);

            var target = new Target(playerTwo); // test: damage the opponent player

            Debug.Log($"Playing card: {cardToTest.cardName}. Target starting health: {target.GetCurrentHealth()}");
            cardToTest.onPlayEffect.Execute(context, target);
        }
    }
}