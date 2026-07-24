using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.AI;

namespace HearthstoneClone.UI
{
    public class EffectTester : MonoBehaviour
    {
        public CardData cardToTest;
        public CardData minionCardToTest;
        public HandDisplay handDisplay;
        public BoardDisplay boardDisplay;
        public Button endTurnButton;

        private PlayerHand playerOneHand;
        private PlayerHand playerTwoHand;
        private GameContext context;
        private Target opponentTarget;
        private Player playerOne;
        private Player playerTwo;
        private Board board;
        private TurnManager turnManager;
        private AIController aiController;

        void Start()
        {
            if (cardToTest == null || cardToTest.onPlayEffect == null)
            {
                Debug.LogWarning("No card or effect assigned to EffectTester.");
                return;
            }

            playerOne = new Player("Player One");
            playerTwo = new Player("Player Two");
            board = new Board(playerOne, playerTwo);
            context = new GameContext(board);

            turnManager = new TurnManager(board);
            turnManager.StartGame();
            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");

            var starterDeck = new List<CardData> { cardToTest, minionCardToTest };

            playerOneHand = new PlayerHand(playerOne, starterDeck);
            playerOneHand.DrawCard();
            playerOneHand.DrawCard();

            // AI gets its own copy of the starter deck so its hand is independent of Player One's.
            var aiDeck = new List<CardData> { cardToTest, minionCardToTest };
            playerTwoHand = new PlayerHand(playerTwo, aiDeck);
            playerTwoHand.DrawCard();
            playerTwoHand.DrawCard();

            aiController = new AIController(playerTwoHand, context, board);

            opponentTarget = new Target(playerTwo);

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

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

        private void OnEndTurnClicked()
        {
            turnManager.EndTurn();
            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");

            if (turnManager.CurrentPlayer == playerTwo)
            {
                aiController.TakeTurn();
                turnManager.EndTurn();
                Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");
            }

            RefreshHandDisplay();
            RefreshBoardDisplay();
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
