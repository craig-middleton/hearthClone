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
        public Button heroPowerButton;

        [Header("Face UI")]
        public FaceView faceView;
        public FaceView opponentFaceView;

        [Header("Spell Animation")]
        public SpellAnimationSequencer spellAnimationSequencer;

        [Header("Mulligan UI")]
        public Transform mulliganPanel;
        public GameObject cardViewPrefab;
        public Button confirmMulliganButton;

        [Header("Game Over UI")]
        public TMPro.TMP_Text gameOverText;

        [Header("Board Background")]
        public Image boardBackgroundImage;
        public List<Sprite> boardBackgrounds;

        [Header("Music")]
        public AudioSource musicSource;
        public List<AudioClip> musicTracks;
        [Range(0f, 1f)] public float musicVolume = 0.5f;

        private PlayerHand playerOneHand;
        private PlayerHand playerTwoHand;
        private GameContext context;
        private Player playerOne;
        private Player playerTwo;
        private Board board;
        private TurnManager turnManager;
        private AIController aiController;

        private MulliganController mulliganController;
        private CombatInputController combatInputController;
        private CardDragResolver cardDragResolver;
        private GameManager gameManager;

        void Start()
        {
            Random.InitState(System.DateTime.Now.Millisecond + System.Environment.TickCount);

            SetRandomBoardBackground();
            SetRandomMusic();

            if (cardPool == null || cardPool.Count == 0)
            {
                Debug.LogWarning("No cards assigned to EffectTester's Card Pool.", this);
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

            aiController.PerformMulligan();

            mulliganController = new MulliganController(playerOneHand, mulliganPanel, cardViewPrefab, confirmMulliganButton, OnMulliganComplete);

            combatInputController = new CombatInputController(
                board, turnManager, playerOne, playerTwo,
                () => gameManager.GameOver, () => mulliganController.MulliganComplete, () => manualControlMode,
                RefreshBoardDisplay, () => gameManager.AfterGameAction());

            cardDragResolver = new CardDragResolver(
                playerOne, playerTwo, board, context,
                playerOneHand, playerTwoHand, turnManager,
                boardDisplay, opponentBoardDisplay,
                faceView, opponentFaceView, spellAnimationSequencer,
                () => gameManager.GameOver, () => mulliganController.MulliganComplete, () => manualControlMode,
                () => gameManager.AfterGameAction());

            gameManager = new GameManager(
                board, turnManager, playerOne, playerTwo,
                playerOneHand, playerTwoHand, aiController, combatInputController,
                gameOverText,
                () => mulliganController.MulliganComplete, () => manualControlMode,
                RefreshAll);

            if (heroPowerButton != null)
            {
                heroPowerButton.onClick.AddListener(gameManager.OnHeroPowerClicked);
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(gameManager.OnEndTurnClicked);
            }

            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(false);
            }

            mulliganController.ShowMulliganUI();
        }

        private void SetRandomBoardBackground()
        {
            if (boardBackgroundImage == null || boardBackgrounds == null || boardBackgrounds.Count == 0)
            {
                return;
            }

            int index = Random.Range(0, boardBackgrounds.Count);
            boardBackgroundImage.sprite = boardBackgrounds[index];

            Debug.Log($"Board background selected: index {index} ({boardBackgrounds[index].name})");
        }

        private void SetRandomMusic()
        {
            if (musicSource == null || musicTracks == null || musicTracks.Count == 0)
            {
                return;
            }

            int index = Random.Range(0, musicTracks.Count);
            musicSource.clip = musicTracks[index];
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();

            Debug.Log($"Music track selected: index {index} ({musicTracks[index].name})");
        }

        private List<CardData> BuildDeck(List<CardData> pool)
        {
            var deck = new List<CardData>();
            foreach (var card in pool)
            {
                if (card == null) continue;

                for (int i = 0; i < copiesPerCard; i++)
                {
                    deck.Add(card);
                }
            }
            return deck;
        }

        private void OnMulliganComplete()
        {
            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshHandDisplay();
            RefreshBoardDisplay();
            RefreshFaceDisplay();
        }

        private void RefreshHandDisplay()
        {
            // See CardDragResolver.DragInProgress's declaration: a rebuild here mid-drag would
            // destroy the CardView the EventSystem is still actively dragging. Not currently
            // reachable, but cheap to guard against.
            if (cardDragResolver.DragInProgress) return;

            if (handDisplay != null)
            {
                handDisplay.RenderHand(playerOneHand.Hand, cardDragResolver.OnCardDragEnd, cardDragResolver.OnCardDragBegan, cardDragResolver.CanPlayerOneDrag);
            }

            if (opponentHandDisplay != null)
            {
                opponentHandDisplay.RenderHand(playerTwoHand.Hand, cardDragResolver.OnOpponentCardDragEnd, cardDragResolver.OnCardDragBegan, cardDragResolver.CanPlayerTwoDrag);
            }
        }

        private void RefreshBoardDisplay()
        {
            if (boardDisplay != null)
            {
                boardDisplay.RenderBoard(
                    playerOne.BoardMinions,
                    m => combatInputController.OnMinionClicked(m, playerOne),
                    combatInputController.SelectedAttacker,
                    showAttackEligibility: turnManager.CurrentPlayer == playerOne);
            }

            if (opponentBoardDisplay != null)
            {
                opponentBoardDisplay.RenderBoard(
                    playerTwo.BoardMinions,
                    m => combatInputController.OnMinionClicked(m, playerTwo),
                    combatInputController.SelectedAttacker,
                    showAttackEligibility: turnManager.CurrentPlayer == playerTwo && manualControlMode);
            }
        }

        private void RefreshFaceDisplay()
        {
            if (faceView != null)
            {
                faceView.SetPlayer(playerOne, combatInputController.OnFaceClicked);
            }

            if (opponentFaceView != null)
            {
                opponentFaceView.SetPlayer(playerTwo, combatInputController.OnFaceClicked);
            }
        }
    }
}