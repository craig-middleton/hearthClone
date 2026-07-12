using UnityEngine;
using System.Collections.Generic;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;

namespace HearthstoneClone.UI
{
    public class EffectTester : MonoBehaviour
    {
        public CardData cardToTest;
        public HandDisplay handDisplay;

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
            var turnManager = new TurnManager(board);

            turnManager.StartGame();
            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");

            var starterDeck = new List<CardData> { cardToTest };
            var playerOneHand = new PlayerHand(playerOne, starterDeck);
            playerOneHand.DrawCard();

            if (handDisplay != null)
            {
                handDisplay.RenderHand(playerOneHand.Hand);
            }

            var target = new Target(playerTwo);
            playerOneHand.PlayCard(cardToTest, context, target);

            turnManager.EndTurn();
            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");
        }
    }
}