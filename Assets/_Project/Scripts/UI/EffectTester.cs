using UnityEngine;
using System.Collections.Generic;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;

namespace HearthstoneClone.UI
{
    public class EffectTester : MonoBehaviour
    {
        public CardData cardToTest;
        public CardData minionCardToTest;
        public HandDisplay handDisplay;
        public BoardDisplay boardDisplay;

        private PlayerHand playerOneHand;
        private GameContext context;
        private Target opponentTarget;
        private Player playerOne;

        void Start()
        {
            if (cardToTest == null || cardToTest.onPlayEffect == null)
            {
                Debug.LogWarning("No card or effect assigned to EffectTester.");
                return;
            }

            playerOne = new Player("Player One");
            var playerTwo = new Player("Player Two");
            var board = new Board(playerOne, playerTwo);
            context = new GameContext(board);
            var turnManager = new TurnManager(board);

            turnManager.StartGame();
            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");

            var starterDeck = new List<CardData> { cardToTest, minionCardToTest };
            playerOneHand = new PlayerHand(playerOne, starterDeck);
            playerOneHand.DrawCard();
            playerOneHand.DrawCard();

            opponentTarget = new Target(playerTwo);

            RefreshHandDisplay();
            RefreshBoardDisplay();
        }

        private void OnCardClicked(CardData card)
        {
            bool success = playerOneHand.PlayCard(card, context, opponentTarget);
            if (success)
            {
                RefreshHandDisplay();
                RefreshBoardDisplay();
            }
        }

        private void RefreshHandDisplay()
        {
            if (handDisplay != null)
            {
                handDisplay.RenderHand(playerOneHand.Hand, OnCardClicked);
            }
        }

        private void RefreshBoardDisplay()
        {
            if (boardDisplay != null)
            {
                boardDisplay.RenderBoard(playerOne.BoardMinions);
            }
        }
    }
}