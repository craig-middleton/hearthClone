using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.AI;
using HearthstoneClone.Effects;

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

        private const int HeroPowerCost = 2;

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
            opponentTarget = new Target(playerTwo);

            aiController.PerformMulligan();

            if (confirmMulliganButton != null)
            {
                confirmMulliganButton.onClick.AddListener(OnConfirmMulliganClicked);
            }

            if (heroPowerButton != null)
            {
                heroPowerButton.onClick.AddListener(OnHeroPowerClicked);
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(false);
            }

            ShowMulliganUI();
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

        private void ShowMulliganUI()
        {
            if (mulliganPanel == null || cardViewPrefab == null)
            {
                Debug.LogWarning("Mulligan UI not wired up — skipping mulligan phase.", this);
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
                if (card == null) continue;

                GameObject cardObj = Instantiate(cardViewPrefab, mulliganPanel);
                CardView view = cardObj.GetComponent<CardView>();
                if (view == null)
                {
                    Debug.LogWarning("Instantiated card prefab has no CardView component.", this);
                    Destroy(cardObj);
                    continue;
                }

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
            if (mulliganComplete) return;

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

            if (confirmMulliganButton != null)
            {
                confirmMulliganButton.gameObject.SetActive(false);
            }

            mulliganComplete = true;

            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");

            RefreshAll();
        }

        private void OnCardClicked(CardData card, CardView cardView)
        {
            if (gameOver) return;
            if (!mulliganComplete) return;
            if (turnManager.CurrentPlayer != playerOne) return;

            Target target = card.targetsSelf ? new Target(playerOne) : opponentTarget;

            // Captured before PlayCard/AfterGameAction run: PlayCard removes the card from
            // Hand, and the RefreshHandDisplay() inside AfterGameAction() destroys this exact
            // CardView synchronously, in the same frame. The Vector3 survives that; a live
            // Transform reference would not.
            Vector3 sourcePosition = cardView.transform.position;
            Transform targetViewTransform = ResolveViewTransform(target);

            bool success = playerOneHand.PlayCard(card, context, target);
            if (success)
            {
                TriggerSpellAnimation(card, sourcePosition, targetViewTransform);
                AfterGameAction();
            }
        }

        private void OnOpponentCardClicked(CardData card, CardView cardView)
        {
            if (gameOver) return;
            if (!mulliganComplete || !manualControlMode) return;
            if (turnManager.CurrentPlayer != playerTwo) return;

            Target target = card.targetsSelf ? new Target(playerTwo) : new Target(playerOne);

            Vector3 sourcePosition = cardView.transform.position;
            Transform targetViewTransform = ResolveViewTransform(target);

            bool success = playerTwoHand.PlayCard(card, context, target);
            if (success)
            {
                TriggerSpellAnimation(card, sourcePosition, targetViewTransform);
                AfterGameAction();
            }
        }

        // Resolves a Target to the Transform of the view that represents it on screen.
        // Only the Player branch is reachable today — spells can only ever target a face
        // (see OnCardClicked/OnOpponentCardClicked above). The Minion branch is written now,
        // ahead of need, so the animation system needs no rework once spell targeting can
        // reach a minion: BoardDisplay doesn't yet track a Minion -> MinionView mapping, so
        // there is nothing to resolve to yet, but the shape of this method won't change.
        private Transform ResolveViewTransform(Target target)
        {
            if (target.TargetPlayer != null)
            {
                if (target.TargetPlayer == playerOne) return faceView != null ? faceView.transform : null;
                if (target.TargetPlayer == playerTwo) return opponentFaceView != null ? opponentFaceView.transform : null;
                return null;
            }

            if (target.TargetMinion != null)
            {
                return null;
            }

            return null;
        }

        private void TriggerSpellAnimation(CardData card, Vector3 sourcePosition, Transform targetViewTransform)
        {
            if (spellAnimationSequencer == null) return;
            if (card.onPlayEffect == null) return;
            if (targetViewTransform == null) return;

            bool isDamage = card.onPlayEffect is DealDamageEffect;
            spellAnimationSequencer.PlayTravelAndReaction(sourcePosition, targetViewTransform, isDamage);
        }

        private void OnMinionClicked(Minion minion, Player owner)
        {
            if (gameOver) return;
            if (!mulliganComplete) return;
            if (minion == null) return;
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

            ResolveAttack(selectedAttacker, new Target(minion));
        }

        private void OnFaceClicked(Player owner)
        {
            if (gameOver) return;
            if (!mulliganComplete) return;
            if (!manualControlMode && turnManager.CurrentPlayer == playerTwo) return;
            if (selectedAttacker == null) return;
            if (owner == turnManager.CurrentPlayer) return;

            ResolveAttack(selectedAttacker, new Target(owner));
        }

        private void OnHeroPowerClicked()
        {
            if (gameOver) return;
            if (!mulliganComplete) return;
            if (!manualControlMode && turnManager.CurrentPlayer == playerTwo) return;

            // Everything below resolves through whoever is actually taking the turn,
            // so the single Hero Power button works for Player Two under manual
            // control mode instead of silently charging Player One.
            Player actingPlayer = turnManager.CurrentPlayer;
            Player opposingPlayer = board.GetOpponent(actingPlayer);

            if (actingPlayer.HasUsedHeroPowerThisTurn) return;
            if (actingPlayer.CurrentMana < HeroPowerCost) return;

            actingPlayer.CurrentMana -= HeroPowerCost;
            actingPlayer.HasUsedHeroPowerThisTurn = true;
            opposingPlayer.TakeDamage(1);

            Debug.Log($"{actingPlayer.PlayerName} used Hero Power. Dealt 1 damage to {opposingPlayer.PlayerName}.");

            AfterGameAction();
        }

        private void ResolveAttack(Minion attacker, Target target)
        {
            bool success = Combat.TryAttack(attacker, target, board, out string failReason);
            if (success)
            {
                Debug.Log($"{attacker.MinionName} attacked.");
                selectedAttacker = null;
            }
            else
            {
                Debug.LogWarning(failReason);
            }

            AfterGameAction();
        }

        private void AfterGameAction()
        {
            board.RemoveDeadMinions();

            // A rejected attack keeps the attacker selected so it can be retargeted,
            // but a minion that just died must not stay selected - it is no longer
            // rendered, so there would be no way to deselect it.
            if (selectedAttacker != null && selectedAttacker.IsDead)
            {
                selectedAttacker = null;
            }

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
            CheckWinCondition();

            if (!gameOver && !manualControlMode && turnManager.CurrentPlayer == playerTwo)
            {
                aiController.TakeTurn();
                CheckWinCondition();

                if (!gameOver)
                {
                    turnManager.EndTurn();
                    DrawForCurrentPlayer();
                    Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");
                    CheckWinCondition();
                }
            }

            AfterGameAction();
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