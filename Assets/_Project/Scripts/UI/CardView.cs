using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using HearthstoneClone.Cards;

namespace HearthstoneClone.UI
{
    public class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        public TMP_Text nameText;
        public TMP_Text costText;
        public TMP_Text statsText;
        public Button button;
        public Image cardBackground;
        public Image artworkImage;

        [Header("Mulligan Visuals")]
        public Color normalColor = Color.white;
        public Color selectedForMulliganColor = new Color(0.4f, 0.4f, 0.4f);

        [Header("Face-Down Visuals")]
        [SerializeField] private Sprite cardBackSprite;

        [Header("Drag Visuals")]
        public float dragGhostAlpha = 0.8f;

        private CardInstance card;
        public CardInstance Card => card;
        private Action<CardInstance> onMulliganToggled;
        private Action<CardInstance, CardView> onDragBegan;
        private Action<CardInstance, CardView, PointerEventData> onDragEnded;
        private Func<bool> canDrag;
        private bool isSelectedForMulligan;

        private GameObject dragGhost;
        private bool dragActive;

        // Captured once, before ShowFaceDown can ever run, so the face-up path can restore
        // cardBackground's true prefab sprite (frame/background art) instead of guessing at it
        // with a null sprite - the first regression this fixed was exactly that guess being
        // wrong. Deliberately NOT caching/restoring the prefab's original color alongside it:
        // the prefab bakes cardBackground's color at a = 0.392 (a leftover value that was never
        // visible pre-feature because the old code unconditionally overwrote it to normalColor
        // every render) - restoring that raw cached alpha was the second regression (washed-out
        // face-up cards). normalColor (opaque) is the correct face-up restore, same as before
        // this feature existed.
        private Sprite originalCardBackgroundSprite;

        private void Awake()
        {
            if (cardBackground != null)
            {
                originalCardBackgroundSprite = cardBackground.sprite;
            }
        }

        // Playing a card from hand is drag-only (see CardView's IBeginDrag/IDrag/IEndDrag
        // implementation below) - button stays unwired here since click-to-play no longer
        // exists. SetCardForMulligan below still wires button.onClick for its own toggle flow.
        // canDragPredicate gates drag INITIATION (e.g. "is it this player's turn"), separate
        // from drop resolution's own eligibility check - an ineligible card shouldn't even
        // spawn a ghost when picked up.
        // faceDown defaults false so every pre-existing call site (the player's own hand)
        // is unaffected - only the opponent HandDisplay call passes a faceDown predicate's
        // result through. See HandDisplay.RenderHand.
        public void SetCard(CardInstance cardData, Action<CardInstance, CardView, PointerEventData> dragEndedCallback = null, Action<CardInstance, CardView> dragBeganCallback = null, Func<bool> canDragPredicate = null, bool faceDown = false)
        {
            if (cardData == null)
            {
                Debug.LogWarning("CardView.SetCard called with a null CardData — skipping.", this);
                return;
            }

            card = cardData;
            onDragEnded = dragEndedCallback;
            onDragBegan = dragBeganCallback;
            canDrag = canDragPredicate;

            if (faceDown)
            {
                ShowFaceDown();
            }
            else
            {
                WriteCardText();

                // Restores the cached prefab original sprite rather than nulling it - nulling it
                // blanked the card's frame/background art entirely (the regression this fixed).
                // Color is reset to normalColor (opaque), not the prefab's raw cached color -
                // see Awake's comment for why.
                if (cardBackground != null)
                {
                    cardBackground.sprite = originalCardBackgroundSprite;
                    cardBackground.color = normalColor;
                }
            }
        }

        // Opponent-hand concealment: hides every readable field and swaps cardBackground to the
        // real card-back sprite instead of writing the real card - the hand's card COUNT is
        // still correct (one CardView per card), but nothing about which card it is is visible.
        // Deliberately does not touch drag wiring: canDrag already governs whether a drag can
        // start at all (CardDragResolver.CanPlayerTwoDrag), independent of what's drawn here.
        private void ShowFaceDown()
        {
            if (nameText != null) nameText.text = "";
            if (costText != null) costText.text = "";
            if (statsText != null) statsText.text = "";
            if (artworkImage != null) artworkImage.enabled = false;

            if (cardBackground != null)
            {
                cardBackground.sprite = cardBackSprite;
                cardBackground.color = Color.white;
            }
        }

        public void SetCardForMulligan(CardInstance cardData, Action<CardInstance> toggleCallback)
        {
            if (cardData == null)
            {
                Debug.LogWarning("CardView.SetCardForMulligan called with a null CardData — skipping.", this);
                return;
            }

            card = cardData;
            onMulliganToggled = toggleCallback;
            isSelectedForMulligan = false;

            WriteCardText();
            UpdateMulliganVisual();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    isSelectedForMulligan = !isSelectedForMulligan;
                    UpdateMulliganVisual();
                    // Pass the CardInstance directly - it now has real per-copy identity (root
                    // fix for the CardData reference-identity issue), so the caller no longer
                    // needs to key off this CardView as an identity proxy.
                    onMulliganToggled?.Invoke(card);
                });
            }
            else
            {
                Debug.LogWarning("CardView: 'button' is not assigned in the Inspector — this card cannot be toggled for mulligan.", this);
            }
        }

        private void WriteCardText()
        {
            if (nameText != null)
            {
                nameText.text = card.Data.cardName;
            }
            else
            {
                Debug.LogWarning("CardView: 'nameText' is not assigned in the Inspector — card name will not render.", this);
            }

            if (costText != null)
            {
                costText.text = card.Data.manaCost.ToString();
            }
            else
            {
                Debug.LogWarning("CardView: 'costText' is not assigned in the Inspector — card mana cost will not render.", this);
            }

            if (statsText != null)
            {
                statsText.text = card.Data.cardType == CardType.Minion
                    ? $"{card.Data.attack} / {card.Data.health}"
                    : "";
            }
            else
            {
                Debug.LogWarning("CardView: 'statsText' is not assigned in the Inspector — card attack/health will not render.", this);
            }

            if (artworkImage != null)
            {
                if (card.Data.artwork != null)
                {
                    artworkImage.sprite = card.Data.artwork;
                    artworkImage.enabled = true;
                }
                else
                {
                    artworkImage.enabled = false;
                }
            }
        }

        private void UpdateMulliganVisual()
        {
            if (cardBackground != null)
            {
                cardBackground.color = isSelectedForMulligan ? selectedForMulliganColor : normalColor;
            }
        }

        // --- Drag-to-play: ghost-follows-cursor here; drop resolution lives in EffectTester ---

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (card == null) return;
            if (canDrag != null && !canDrag()) return;

            dragActive = true;
            onDragBegan?.Invoke(card, this);

            Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (rootCanvas == null) return;

            dragGhost = Instantiate(gameObject, rootCanvas.transform);
            dragGhost.name = $"{name} (DragGhost)";

            // Instantiate(original, parent) keeps the source's LOCAL rotation and scale
            // relative to the new parent, so the clone inherits whatever HandFanLayout gave the
            // source card - once the hand fans, an untouched ghost would drag around tilted at
            // its slot's arc angle. Harmless before the fan existed (rotation was always
            // identity), wrong the moment it doesn't. The ghost is a free-floating cursor
            // follower and should read as upright and unscaled no matter where in the arc it
            // was picked up from.
            RectTransform ghostRect = dragGhost.transform as RectTransform;
            if (ghostRect != null)
            {
                ghostRect.localRotation = Quaternion.identity;
                ghostRect.localScale = Vector3.one;
            }

            // Ghost must never be a raycast target itself, or OnEndDrag's RaycastAll would
            // just hit the ghost sitting under the pointer instead of the real drop target.
            CanvasGroup ghostGroup = dragGhost.GetComponent<CanvasGroup>();
            if (ghostGroup == null) ghostGroup = dragGhost.AddComponent<CanvasGroup>();
            ghostGroup.blocksRaycasts = false;
            ghostGroup.interactable = false;
            ghostGroup.alpha = dragGhostAlpha;

            MoveGhostTo(eventData, rootCanvas);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost == null) return;

            Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (rootCanvas == null) return;

            MoveGhostTo(eventData, rootCanvas);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost);
                dragGhost = null;
            }

            // EventSystem still calls OnEndDrag even when OnBeginDrag declined the drag
            // (canDrag false) - dragActive tells us this drag never actually started, so
            // there's nothing to resolve.
            if (!dragActive) return;
            dragActive = false;

            onDragEnded?.Invoke(card, this, eventData);
        }

        private void MoveGhostTo(PointerEventData eventData, Canvas rootCanvas)
        {
            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            RectTransform ghostRect = dragGhost.transform as RectTransform;
            if (canvasRect == null || ghostRect == null) return;

            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, eventData.position, cam, out Vector3 worldPoint))
            {
                ghostRect.position = worldPoint;
            }
        }

        private void OnDestroy()
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost);
                dragGhost = null;
            }
        }
    }
}