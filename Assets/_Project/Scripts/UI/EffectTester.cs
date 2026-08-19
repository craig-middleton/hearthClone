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
        private Player playerOne;
        private Player playerTwo;
        private Board board;
        private TurnManager turnManager;
        private AIController aiController;

        private HashSet<CardData> mulliganSelections = new HashSet<CardData>();
        private List<GameObject> mulliganCardObjects = new List<GameObject>();
        private bool mulliganComplete = false;

        private Minion selectedAttacker = null;

        // A card with TargetRequirement.Any that's waiting for the player to click a
        // minion or face. pendingSpellCard is the CardData to play; pendingSpellHandIndex
        // identifies WHICH physical card was clicked (its position in Hand at click time),
        // since duplicate copies of a card share the same CardData reference and can't be
        // told apart by it alone - two Fireballs in hand would otherwise look identical to
        // the cancel/switch check. This can't be a live CardView reference either: selecting
        // a card triggers RefreshHandDisplay() to show the pending highlight, which destroys
        // and rebuilds every CardView that same call, including the one just clicked - so the
        // index (stable as long as Hand doesn't reorder while a spell is pending, which it
        // doesn't) is what survives, not the view instance. pendingSpellSourcePosition is
        // likewise captured as a plain Vector3 rather than a Transform, for the same reason.
        private CardData pendingSpellCard = null;
        private int pendingSpellHandIndex = -1;
        private Vector3 pendingSpellSourcePosition;
        private Player pendingSpellCaster = null;

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

        private void OnCardClicked(CardData card, CardView cardView, int handIndex)
        {
            if (gameOver) return;
            if (!mulliganComplete) return;
            if (turnManager.CurrentPlayer != playerOne) return;

            HandleHandCardClicked(card, cardView, handIndex, playerOne);
        }

        private void OnOpponentCardClicked(CardData card, CardView cardView, int handIndex)
        {
            if (gameOver) return;
            if (!mulliganComplete || !manualControlMode) return;
            if (turnManager.CurrentPlayer != playerTwo) return;

            HandleHandCardClicked(card, cardView, handIndex, playerTwo);
        }

        // Shared by OnCardClicked and OnOpponentCardClicked (manual control mode). A
        // TargetRequirement.Any card enters the pending-selection state instead of playing
        // immediately; None/Self cards resolve and play right away, same as before targeting
        // existed. Identity for the cancel/switch check is (actingPlayer, handIndex), not the
        // CardData reference - see the pendingSpellHandIndex field comment for why.
        private void HandleHandCardClicked(CardData card, CardView cardView, int handIndex, Player actingPlayer)
        {
            if (pendingSpellCaster == actingPlayer && pendingSpellHandIndex == handIndex)
            {
                pendingSpellCard = null;
                pendingSpellCaster = null;
                pendingSpellHandIndex = -1;
                RefreshHandDisplay();
                return;
            }

            if (card.targetRequirement == TargetRequirement.Any)
            {
                selectedAttacker = null;
                pendingSpellCard = card;
                pendingSpellCaster = actingPlayer;
                pendingSpellHandIndex = handIndex;
                pendingSpellSourcePosition = cardView.transform.position;
                RefreshAll();
                return;
            }

            Target target = card.targetRequirement == TargetRequirement.Self ? new Target(actingPlayer) : null;

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

        // Resolves a spell that's pending target selection (TargetRequirement.Any) against
        // the minion or face the player just clicked. Called from OnMinionClicked/OnFaceClicked.
        private void ResolveSpell(Target target)
        {
            CardData card = pendingSpellCard;
            Vector3 sourcePosition = pendingSpellSourcePosition;
            Player caster = pendingSpellCaster;
            PlayerHand hand = caster == playerOne ? playerOneHand : playerTwoHand;

            Transform targetViewTransform = ResolveViewTransform(target);

            bool success = hand.PlayCard(card, context, target);
            if (success)
            {
                // Cleared only on success, same as ResolveAttack clearing selectedAttacker:
                // a rejected play (e.g. mana changed) leaves the card selected so the player
                // can retry a different target instead of losing the selection.
                pendingSpellCard = null;
                pendingSpellCaster = null;
                pendingSpellHandIndex = -1;
                TriggerSpellAnimation(card, sourcePosition, targetViewTransform);
            }

            AfterGameAction();
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

            // A pending spell takes priority over attacker-selection: clicking any minion
            // (either side - see approved plan, no friendly-fire/Taunt restriction on spell
            // targets) while a spell is pending resolves the spell instead of selecting the
            // minion as an attacker.
            if (pendingSpellCard != null)
            {
                ResolveSpell(new Target(minion));
                return;
            }

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

            // Same priority as OnMinionClicked: a pending spell resolves against whichever
            // face was clicked, including the caster's own (no self-targeting restriction).
            if (pendingSpellCard != null)
            {
                ResolveSpell(new Target(owner));
                return;
            }

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
            pendingSpellCard = null;
            pendingSpellCaster = null;
            pendingSpellHandIndex = -1;
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
                int pendingIndex = pendingSpellCaster == playerOne ? pendingSpellHandIndex : -1;
                handDisplay.RenderHand(playerOneHand.Hand, OnCardClicked, pendingIndex);
            }

            if (opponentHandDisplay != null)
            {
                int pendingIndex = pendingSpellCaster == playerTwo ? pendingSpellHandIndex : -1;
                opponentHandDisplay.RenderHand(playerTwoHand.Hand, manualControlMode ? OnOpponentCardClicked : null, pendingIndex);
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