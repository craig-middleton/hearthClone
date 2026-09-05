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
        public Button playAgainButton;

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

            // Wired exactly ONCE here, never inside BeginNewGame(). BeginNewGame() constructs a
            // brand new GameManager/MulliganController per game (including on restart), so if
            // these listeners were added there too, every restart would stack another listener
            // bound to the OLD instance on top of the new one - the old instance still holds
            // readonly references to the previous game's Board/Player/PlayerHand, so a stacked
            // listener would silently mutate a game nobody can see anymore. Each lambda closes
            // over the field, not a value captured at wiring time, so it always resolves against
            // whichever instance BeginNewGame() most recently assigned - restart just changes
            // what the trampoline delegates to, without ever adding a second listener.
            if (heroPowerButton != null)
            {
                heroPowerButton.onClick.AddListener(() => gameManager.OnHeroPowerClicked());
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(() => gameManager.OnEndTurnClicked());
            }

            if (confirmMulliganButton != null)
            {
                confirmMulliganButton.onClick.AddListener(() => mulliganController.OnConfirmMulliganClicked());
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }

            BeginNewGame();
        }

        // Composition root: builds the entire per-game object graph from scratch (players,
        // board, hands, AI, and all four controllers) and wires the mulligan UI to start.
        // Called once from Start() and again from OnPlayAgainClicked() on restart - reusing
        // this exact method rather than a separate reset path guarantees game 2 starts
        // identically to game 1, since it's the same already-playtest-confirmed code, not a
        // second hand-written "return to fresh state" path that could drift. Must also undo
        // every piece of UI teardown a finished game leaves behind (mulligan panel/button
        // hidden at confirm, game-over text/button shown at end) so a restarted game starts
        // from the same visual state Start() does, not wherever the last game left off.
        private void BeginNewGame()
        {
            if (cardPool == null || cardPool.Count == 0)
            {
                Debug.LogWarning("No cards assigned to EffectTester's Card Pool.", this);
                return;
            }

            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(false);
            }

            if (playAgainButton != null)
            {
                playAgainButton.gameObject.SetActive(false);
            }

            if (mulliganPanel != null)
            {
                mulliganPanel.gameObject.SetActive(true);
            }

            if (confirmMulliganButton != null)
            {
                confirmMulliganButton.gameObject.SetActive(true);
            }

            // Clears both hand panels without rendering anything into them - RenderHand(null)
            // destroys existing children first, then returns before instantiating any new ones.
            // Needed because handDisplay/opponentHandDisplay are the SAME persistent
            // MonoBehaviours from the previous game and only ever tear down their rendered
            // CardViews on their own next call - the previous game's final AfterGameAction()
            // rendered whatever each player's hand held at the moment of victory, and nothing
            // has called RenderHand() since. Deliberately passing null rather than the new,
            // correct hand: HandPanel shares MulliganPanel's screen position, so rendering the
            // (correct) new hand here would just swap "stale duplicate cards" for "correct-
            // content duplicate cards" behind the mulligan row instead of fixing the overlap.
            if (handDisplay != null)
            {
                handDisplay.RenderHand(null);
            }

            if (opponentHandDisplay != null)
            {
                opponentHandDisplay.RenderHand(null);
            }

            // Hidden during mulligan, shown again in OnMulliganComplete() - pre-existing gap
            // (nothing ever hid these during mulligan, in this game or any before it), fixed
            // here rather than left as a restart-specific patch, so a fresh game gets the same
            // correct behaviour as every restart from one code path.
            if (endTurnButton != null)
            {
                endTurnButton.gameObject.SetActive(false);
            }

            if (heroPowerButton != null)
            {
                heroPowerButton.gameObject.SetActive(false);
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
                gameOverText, playAgainButton,
                () => mulliganController.MulliganComplete, () => manualControlMode,
                RefreshAll);

            // Tears down whatever MinionViews the previous game's BoardDisplay still holds and
            // re-renders against the now-empty BoardMinions, so a restart's mulligan screen
            // shows an empty board instead of the just-finished game's leftover minions sitting
            // underneath it. Needed here specifically because a restart's boardDisplay/
            // opponentBoardDisplay are the SAME persistent MonoBehaviours from the previous
            // game - unlike every other piece of state, they aren't rebuilt fresh, only
            // re-rendered, and nothing else re-renders them before mulligan is shown.
            RefreshBoardDisplay();

            // Same reasoning as RefreshBoardDisplay() above, for the same reason: faceView/
            // opponentFaceView are persistent MonoBehaviours holding the previous game's final
            // HP/mana text until something calls SetPlayer() again.
            RefreshFaceDisplay();

            mulliganController.ShowMulliganUI();
        }

        // playAgainButton's only listener (wired once in Start()). Restart is a composition-
        // root concern - rebuilding the whole Player/Board/Hand/controller graph - not a
        // GameManager concern, so this calls BeginNewGame() directly rather than routing
        // through GameManager, which has no reason to know how to reconstruct a Board.
        private void OnPlayAgainClicked()
        {
            BeginNewGame();
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
            if (endTurnButton != null)
            {
                endTurnButton.gameObject.SetActive(true);
            }

            if (heroPowerButton != null)
            {
                heroPowerButton.gameObject.SetActive(true);
            }

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