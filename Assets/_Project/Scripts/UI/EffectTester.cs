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
        public List<CardData> cardPool;             // drag all unique test cards here in Inspector
        public int copiesPerCard = 2;
        public CardData coinCard;                   // The Coin — given directly to whoever goes second

        public HandDisplay handDisplay;
        public HandDisplay opponentHandDisplay;      // AI's hand, read-only
        public BoardDisplay boardDisplay;            // Player One's board
        public BoardDisplay opponentBoardDisplay;    // Player Two's (AI's) board
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
            if (cardPool == null || cardPool.Count == 0)
            {
                Debug.LogWarning("No cards assigned to EffectTester's Card Pool.");
                return;
            }

            playerOne = new Player("Player One");
            playerTwo = new Player("Player Two");
            board = new Board(playerOne, playerTwo);
            context = new GameContext(board);

            turnManager = new TurnManager(board);
            turnManager.StartGame();
            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");

            // Player One goes first: 3-card opening hand, no Coin.
            playerOneHand = new PlayerHand(playerOne, BuildDeck(cardPool));
            playerOneHand.Shuffle();
            playerOneHand.DrawOpeningHand(3);

            // Player Two goes second: 4-card opening hand, plus The Coin.
            playerTwoHand = new PlayerHand(playerTwo, BuildDeck(cardPool));
            playerTwoHand.Shuffle();
            playerTwoHand.DrawOpeningHand(4);
            if (coinCard != null)
            {
                playerTwoHand.AddCardToHand(coinCard);
            }

            aiController = new AIController(playerTwoHand, context, board);

            opponentTarget = new Target(playerTwo);

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            RefreshHandDisplay();
            RefreshBoardDisplay();
        }

        private List<CardData> BuildDeck(List<CardData> pool)
        {
            var deck = new List<CardData>();
            foreach (var card in pool)
            {
                for (int i = 0; i < copiesPerCard; i++)
                {
                    deck.Add(card);
                }
            }
            return deck;
        }

        private void OnCardClicked(CardData card)
        {
            Target target = card.targetsSelf ? new Target(playerOne) : opponentTarget;
            bool success = playerOneHand.PlayCard(card, context, target);
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

            if (opponentHandDisplay != null)
            {
                opponentHandDisplay.RenderHand(playerTwoHand.Hand, null);
            }
        }

        private void RefreshBoardDisplay()
        {
            if (boardDisplay != null)
            {
                boardDisplay.RenderBoard(playerOne.BoardMinions);
            }

            if (opponentBoardDisplay != null)
            {
                opponentBoardDisplay.RenderBoard(playerTwo.BoardMinions);
            }
        }
    }
}