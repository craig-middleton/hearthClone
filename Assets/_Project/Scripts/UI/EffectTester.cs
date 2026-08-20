using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

        // Set while a hand card is mid-drag (between CardView's OnBeginDrag and OnEndDrag).
        // RefreshHandDisplay destroys/rebuilds every CardView, which would pull the rug out
        // from under Unity's EventSystem mid-gesture - see slice-1 investigation notes.
        // Nothing currently calls RefreshAll/RefreshHandDisplay during that window, but this
        // guards against a future change accidentally introducing one.
        private bool dragInProgress = false;

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

        private void OnCardDragBegan(CardData card, CardView cardView)
        {
            dragInProgress = true;
        }

        // Shared eligibility checks - used both to gate drop resolution (below) and to gate
        // drag initiation itself (passed as CardView's canDrag predicate via RenderHand, so
        // an ineligible card doesn't even spawn a ghost - see HandDisplay/CardView wiring).
        private bool CanPlayerOneDrag()
        {
            return !gameOver && mulliganComplete && turnManager.CurrentPlayer == playerOne;
        }

        private bool CanPlayerTwoDrag()
        {
            return !gameOver && mulliganComplete && manualControlMode && turnManager.CurrentPlayer == playerTwo;
        }

        private void OnCardDragEnd(CardData card, CardView cardView, PointerEventData eventData)
        {
            dragInProgress = false;

            if (!CanPlayerOneDrag()) return;

            ResolveCardDrag(card, cardView, eventData, playerOne);
        }

        private void OnOpponentCardDragEnd(CardData card, CardView cardView, PointerEventData eventData)
        {
            dragInProgress = false;

            if (!CanPlayerTwoDrag()) return;

            ResolveCardDrag(card, cardView, eventData, playerTwo);
        }

        // Shared by OnCardDragEnd and OnOpponentCardDragEnd. Unlike the old click-then-click
        // flow, a drag is a single gesture: BeginDrag/EndDrag happen within the same
        // interaction, so target selection is just "what's under the pointer at drop time" -
        // no cross-frame pending state needed. Minion cards require a friendly-board drop
        // (summoning onto the opponent's board isn't a real thing); Any-target spells need a
        // MinionView or FaceView hit; None/Self spells accept any recognized zone. Anything
        // else (empty space, the hand panel) is an invalid drop and simply does nothing - the
        // CardView itself was never touched, so there's nothing to snap back.
        private void ResolveCardDrag(CardData card, CardView cardView, PointerEventData eventData, Player actingPlayer)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            BoardDisplay friendlyBoard = actingPlayer == playerOne ? boardDisplay : opponentBoardDisplay;
            BoardDisplay enemyBoard = actingPlayer == playerOne ? opponentBoardDisplay : boardDisplay;

            MinionView hitMinionView = null;
            FaceView hitFaceView = null;
            bool hitFriendlyBoard = false;
            bool hitEnemyBoard = false;

            foreach (var result in results)
            {
                if (hitMinionView == null) hitMinionView = result.gameObject.GetComponentInParent<MinionView>();
                if (hitFaceView == null) hitFaceView = result.gameObject.GetComponentInParent<FaceView>();
                if (!hitFriendlyBoard) hitFriendlyBoard = IsUnderPanel(result.gameObject.transform, friendlyBoard);
                if (!hitEnemyBoard) hitEnemyBoard = IsUnderPanel(result.gameObject.transform, enemyBoard);
            }

            Target target = null;
            bool validDrop;

            if (card.cardType == CardType.Minion)
            {
                validDrop = hitFriendlyBoard;
            }
            else if (card.targetRequirement == TargetRequirement.Any)
            {
                if (hitMinionView != null)
                {
                    target = new Target(hitMinionView.Minion);
                    validDrop = true;
                }
                else if (hitFaceView != null)
                {
                    target = new Target(hitFaceView.Player);
                    validDrop = true;
                }
                else
                {
                    validDrop = false;
                }
            }
            else
            {
                validDrop = hitFriendlyBoard || hitEnemyBoard || hitMinionView != null || hitFaceView != null;
                if (validDrop && card.targetRequirement == TargetRequirement.Self)
                {
                    target = new Target(actingPlayer);
                }
            }

            if (!validDrop)
            {
                Debug.Log($"{actingPlayer.PlayerName} dropped '{card.cardName}' on an invalid zone — no action taken.");
                return;
            }

            // Captured before PlayCard/AfterGameAction run: PlayCard removes the card from
            // Hand, and the RefreshHandDisplay() inside AfterGameAction() destroys this exact
            // CardView synchronously, in the same frame. The Vector3 survives that; a live
            // Transform reference would not.
            Vector3 sourcePosition = cardView.transform.position;
            Transform targetViewTransform = target != null ? ResolveViewTransform(target) : null;

            PlayerHand hand = actingPlayer == playerOne ? playerOneHand : playerTwoHand;
            bool success = hand.PlayCard(card, context, target);
            if (success)
            {
                TriggerSpellAnimation(card, sourcePosition, targetViewTransform);
                AfterGameAction();
            }
        }

        private bool IsUnderPanel(Transform hit, BoardDisplay board)
        {
            if (board == null || board.boardPanel == null) return false;
            return hit == board.boardPanel || hit.IsChildOf(board.boardPanel);
        }

        // Resolves a Target to the Transform of the view that represents it on screen.
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
                Player owner = board.GetOwnerOf(target.TargetMinion);
                if (owner == playerOne) return boardDisplay != null ? boardDisplay.GetViewTransform(target.TargetMinion) : null;
                if (owner == playerTwo) return opponentBoardDisplay != null ? opponentBoardDisplay.GetViewTransform(target.TargetMinion) : null;
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
            // See dragInProgress's declaration: a rebuild here mid-drag would destroy the
            // CardView the EventSystem is still actively dragging. Not currently reachable,
            // but cheap to guard against.
            if (dragInProgress) return;

            if (handDisplay != null)
            {
                handDisplay.RenderHand(playerOneHand.Hand, OnCardDragEnd, OnCardDragBegan, CanPlayerOneDrag);
            }

            if (opponentHandDisplay != null)
            {
                opponentHandDisplay.RenderHand(playerTwoHand.Hand, OnOpponentCardDragEnd, OnCardDragBegan, CanPlayerTwoDrag);
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