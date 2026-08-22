using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.Effects;

namespace HearthstoneClone.UI
{
    // Owns drag-to-play card resolution: eligibility gating, the drop raycast/classification,
    // PlayCard invocation, and triggering the spell-cast animation on success. Constructed
    // once in EffectTester.Start() and lives for the whole match.
    //
    // Not a "finished" component even once extracted (session 33) - Next Steps 17's card
    // lift/zoom and discard-off work will touch ResolveCardDrag/TriggerSpellAnimation again
    // once the Constraint 15 decision (how RenderHand's synchronous CardView rebuild is
    // handled) is made. This extraction only moved the code, it didn't change that open design
    // question.
    public class CardDragResolver
    {
        private readonly Player playerOne;
        private readonly Player playerTwo;
        private readonly Board board;
        private readonly GameContext context;
        private readonly PlayerHand playerOneHand;
        private readonly PlayerHand playerTwoHand;
        private readonly TurnManager turnManager;
        private readonly BoardDisplay boardDisplay;
        private readonly BoardDisplay opponentBoardDisplay;
        private readonly FaceView faceView;
        private readonly FaceView opponentFaceView;
        private readonly SpellAnimationSequencer spellAnimationSequencer;
        private readonly Func<bool> isGameOver;
        private readonly Func<bool> isMulliganComplete;
        private readonly Func<bool> isManualControlMode;
        private readonly Action onAfterAction;

        // Set while a hand card is mid-drag (between CardView's OnBeginDrag and OnEndDrag).
        // RefreshHandDisplay destroys/rebuilds every CardView, which would pull the rug out
        // from under Unity's EventSystem mid-gesture - see slice-1 investigation notes.
        // Nothing currently calls RefreshAll/RefreshHandDisplay during that window, but this
        // guards against a future change accidentally introducing one.
        private bool dragInProgress = false;

        public bool DragInProgress => dragInProgress;

        public CardDragResolver(
            Player playerOne, Player playerTwo,
            Board board, GameContext context,
            PlayerHand playerOneHand, PlayerHand playerTwoHand,
            TurnManager turnManager,
            BoardDisplay boardDisplay, BoardDisplay opponentBoardDisplay,
            FaceView faceView, FaceView opponentFaceView,
            SpellAnimationSequencer spellAnimationSequencer,
            Func<bool> isGameOver, Func<bool> isMulliganComplete, Func<bool> isManualControlMode,
            Action onAfterAction)
        {
            this.playerOne = playerOne;
            this.playerTwo = playerTwo;
            this.board = board;
            this.context = context;
            this.playerOneHand = playerOneHand;
            this.playerTwoHand = playerTwoHand;
            this.turnManager = turnManager;
            this.boardDisplay = boardDisplay;
            this.opponentBoardDisplay = opponentBoardDisplay;
            this.faceView = faceView;
            this.opponentFaceView = opponentFaceView;
            this.spellAnimationSequencer = spellAnimationSequencer;
            this.isGameOver = isGameOver;
            this.isMulliganComplete = isMulliganComplete;
            this.isManualControlMode = isManualControlMode;
            this.onAfterAction = onAfterAction;
        }

        public void OnCardDragBegan(CardData card, CardView cardView)
        {
            dragInProgress = true;
        }

        // Shared eligibility checks - used both to gate drop resolution (below) and to gate
        // drag initiation itself (passed as CardView's canDrag predicate via RenderHand, so
        // an ineligible card doesn't even spawn a ghost - see HandDisplay/CardView wiring).
        public bool CanPlayerOneDrag()
        {
            return !isGameOver() && isMulliganComplete() && turnManager.CurrentPlayer == playerOne;
        }

        public bool CanPlayerTwoDrag()
        {
            return !isGameOver() && isMulliganComplete() && isManualControlMode() && turnManager.CurrentPlayer == playerTwo;
        }

        public void OnCardDragEnd(CardData card, CardView cardView, PointerEventData eventData)
        {
            dragInProgress = false;

            if (!CanPlayerOneDrag()) return;

            ResolveCardDrag(card, cardView, eventData, playerOne);
        }

        public void OnOpponentCardDragEnd(CardData card, CardView cardView, PointerEventData eventData)
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
                onAfterAction?.Invoke();
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
    }
}
