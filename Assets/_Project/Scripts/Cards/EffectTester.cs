using UnityEngine;
using System.Collections.Generic;
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

            // Test hand/deck
            var starterDeck = new List<CardData> { cardToTest };
            var playerOneHand = new PlayerHand(playerOne, starterDeck);
            playerOneHand.DrawCard();

            // Test effect execution (existing test)
            var target = new Target(playerTwo);
            Debug.Log($"Playing card: {cardToTest.cardName}. Target starting health: {target.GetCurrentHealth()}");
            cardToTest.onPlayEffect.Execute(context, target);
        }
    }
}