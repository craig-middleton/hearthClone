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
        public List<CardData> cardPool;
        public int copiesPerCard = 2;
        public CardData coinCard;

        [Header("Testing")]
        public bool manualControlMode = false;

        public HandDisplay handDisplay;
        public HandDisplay opponentHandDisplay;
        public BoardDisplay boardDisplay;
        public BoardDisplay opponentBoardDisplay;
        public Button endTurnButton;

        [Header("Face UI")]
        public FaceView faceView;
        public FaceView opponentFaceView;

        [Header("Mulligan UI")]
        public Transform mulliganPanel;
        public GameObject cardViewPrefab;
        public Button confirmMulliganButton;

        [Header("Game Over UI")]
        public TMPro.TMP_Text gameOverText;

        private PlayerHand playerOneHand;
        private PlayerHand playerTwoHand;
        private GameContext context;
        private Target opponentTarget;
        private Player playerOne;
        private Player playerTwo;
        private Board board;
        private TurnManager turnManager;
        private AIController aiController;

        private HashSet<CardData> mulliganSelections = new HashSet<CardData>();
        private List<GameObject> mulliganCardObjects = new List<GameObject>();
        private bool mulliganComplete = false;

        private Minion selectedAttacker = null;

        private bool gameOver = false;
        private Player winner = null;

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

            playerOneHand = new PlayerHand(playerOne, BuildDeck(cardPool));
            playerOneHand.Shuffle();
            playerOneHand.DrawOpeningHand(3);

            playerTwoHand = new PlayerHand(playerTwo, BuildDeck(cardPool));
            playerTwoHand.Shuffle();
            playerTwoHand.DrawOpeningHand(4);
            if (coinCard != null)
            {
                playerTwoHand.AddCardToHand(coinCard);
            }

            aiController = new AIController(playerTwoHand, context, board);
            opponentTarget = new Target(playerTwo);

            aiController.PerformMulligan();

            if (confirmMulliganButton != null)
            {
                confirmMulliganButton.onClick.AddListener(OnConfirmMulliganClicked);
            }

            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(false);
            }

            ShowMulliganUI();
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

        private void ShowMulliganUI()
        {
            if (mulliganPanel == null || cardViewPrefab == null)
            {
                Debug.LogWarning("Mulligan UI not wired up — skipping mulligan phase.");
                mulliganComplete = true;
                RefreshAll();
                return;
            }

            mulliganSelections.Clear();

            foreach (Transform child in mulliganPanel)
            {
                Destroy(child.gameObject);
            }
            mulliganCardObjects.Clear();

            foreach (CardData card in playerOneHand.Hand)
            {
                GameObject cardObj = Instantiate(cardViewPrefab, mulliganPanel);
                CardView view = cardObj.GetComponent<CardView>();
                view.SetCardForMulligan(card, OnMulliganCardToggled);
                mulliganCardObjects.Add(cardObj);
            }
        }

        private void OnMulliganCardToggled(CardData card)
        {
            if (mulliganSelections.Contains(card))
            {
                mulliganSelections.Remove(card);
            }
            else
            {
                mulliganSelections.Add(card);
            }
        }

        private void OnConfirmMulliganClicked()
        {
            foreach (CardData card in new List<CardData>(mulliganSelections))
            {
                playerOneHand.MulliganCard(card);
            }

            mulliganSelections.Clear();

            foreach (GameObject obj in mulliganCardObjects)
            {
                Destroy(obj);
            }
            mulliganCardObjects.Clear();

            if (mulliganPanel != null)
            {
                mulliganPanel.gameObject.SetActive(false);
            }

            mulliganComplete = true;

            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");

            RefreshAll();

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }
        }

        private void OnCardClicked(CardData card)
        {
            if (gameOver) return;
            if (!mulliganComplete) return;
            if (turnManager.CurrentPlayer != playerOne) return;

            Target target = card.targetsSelf ? new Target(playerOne) : opponentTarget;
            bool success = playerOneHand.PlayCard(card, context, target);
            if (success)
            {
                RefreshAll();
            }
        }

        private void OnOpponentCardClicked(CardData card)
        {
            if (gameOver) return;
            if (!mulliganComplete || !manualControlMode) return;
            if (turnManager.CurrentPlayer != playerTwo) return;

            Target target = card.targetsSelf ? new Target(playerTwo) : new Target(playerOne);
            bool success = playerTwoHand.PlayCard(card, context, target);
            if (success)
            {
                RefreshAll();
            }
        }

        private void OnMinionClicked(Minion minion, Player owner)
        {
            if (gameOver) return;
            if (!mulliganComplete) return;
            if (!manualControlMode && turnManager.CurrentPlayer == playerTwo) return;

            bool ownerIsActingPlayer = owner == turnManager.CurrentPlayer && (owner == playerOne || manualControlMode);

            if (ownerIsActingPlayer)
            {
                if (selectedAttacker == minion)
                {
                    selectedAttacker = null;
                }
                else if (minion.CanAttack)
                {
                    selectedAttacker = minion;
                }
                RefreshBoardDisplay();
                return;
            }

            if (selectedAttacker == null) return;

            var defenderTaunts = board.GetTauntMinions(owner);
            if (defenderTaunts.Count > 0 && !minion.HasTaunt)
            {
                Debug.LogWarning($"{owner.PlayerName} has a Taunt minion — you must attack it first.");
                return;
            }

            ResolveAttack(selectedAttacker, new Target(minion));
        }

        private void OnFaceClicked(Player owner)
        {
            if (gameOver) return;
            if (!mulliganComplete) return;
            if (!manualControlMode && turnManager.CurrentPlayer == playerTwo) return;
            if (selectedAttacker == null) return;
            if (owner == turnManager.CurrentPlayer) return;

            var defenderTaunts = board.GetTauntMinions(owner);
            if (defenderTaunts.Count > 0)
            {
                Debug.LogWarning($"{owner.PlayerName} has a Taunt minion — you must attack it first.");
                return;
            }

            ResolveAttack(selectedAttacker, new Target(owner));
        }

        private void ResolveAttack(Minion attacker, Target target)
        {
            bool success = Combat.TryAttack(attacker, target, out string failReason);
            if (success)
            {
                board.RemoveDeadMinions();
                Debug.Log($"{attacker.MinionName} attacked.");
            }
            else
            {
                Debug.LogWarning(failReason);
            }

            selectedAttacker = null;
            RefreshAll();
            CheckWinCondition();
        }

        private void OnEndTurnClicked()
        {
            if (gameOver) return;
            if (!mulliganComplete) return;

            selectedAttacker = null;
            turnManager.EndTurn();
            DrawForCurrentPlayer();
            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");

            if (!manualControlMode && turnManager.CurrentPlayer == playerTwo)
            {
                aiController.TakeTurn();
                turnManager.EndTurn();
                DrawForCurrentPlayer();
                Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");
            }

            RefreshAll();
            CheckWinCondition();
        }

        private void DrawForCurrentPlayer()
        {
            if (turnManager.CurrentPlayer == playerOne)
            {
                playerOneHand.DrawCard();
            }
            else
            {
                playerTwoHand.DrawCard();
            }
        }

        private void CheckWinCondition()
        {
            if (gameOver) return;

            if (playerOne.Health <= 0)
            {
                winner = playerTwo;
            }
            else if (playerTwo.Health <= 0)
            {
                winner = playerOne;
            }

            if (winner != null)
            {
                gameOver = true;
                Debug.Log($"*** {winner.PlayerName} wins! ***");

                if (gameOverText != null)
                {
                    gameOverText.gameObject.SetActive(true);
                    gameOverText.text = $"{winner.PlayerName} wins!";
                }
            }
        }

        private void RefreshAll()
        {
            RefreshHandDisplay();
            RefreshBoardDisplay();
            RefreshFaceDisplay();
        }

        private void RefreshHandDisplay()
        {
            if (handDisplay != null)
            {
                handDisplay.RenderHand(playerOneHand.Hand, OnCardClicked);
            }

            if (opponentHandDisplay != null)
            {
                opponentHandDisplay.RenderHand(playerTwoHand.Hand, manualControlMode ? OnOpponentCardClicked : null);
            }
        }

        private void RefreshBoardDisplay()
        {
            if (boardDisplay != null)
            {
                boardDisplay.RenderBoard(
                    playerOne.BoardMinions,
                    m => OnMinionClicked(m, playerOne),
                    selectedAttacker,
                    showAttackEligibility: turnManager.CurrentPlayer == playerOne);
            }

            if (opponentBoardDisplay != null)
            {
                opponentBoardDisplay.RenderBoard(
                    playerTwo.BoardMinions,
                    m => OnMinionClicked(m, playerTwo),
                    selectedAttacker,
                    showAttackEligibility: turnManager.CurrentPlayer == playerTwo && manualControlMode);
            }
        }

        private void RefreshFaceDisplay()
        {
            if (faceView != null)
            {
                faceView.SetPlayer(playerOne, OnFaceClicked);
            }

            if (opponentFaceView != null)
            {
                opponentFaceView.SetPlayer(playerTwo, OnFaceClicked);
            }
        }
    }
}