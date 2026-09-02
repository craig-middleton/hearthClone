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

        [Header("Drag Visuals")]
        public float dragGhostAlpha = 0.8f;

        private CardData card;
        public CardData Card => card;
        private Action<CardView> onMulliganToggled;
        private Action<CardData, CardView> onDragBegan;
        private Action<CardData, CardView, PointerEventData> onDragEnded;
        private Func<bool> canDrag;
        private bool isSelectedForMulligan;

        private GameObject dragGhost;
        private bool dragActive;

        // Playing a card from hand is drag-only (see CardView's IBeginDrag/IDrag/IEndDrag
        // implementation below) - button stays unwired here since click-to-play no longer
        // exists. SetCardForMulligan below still wires button.onClick for its own toggle flow.
        // canDragPredicate gates drag INITIATION (e.g. "is it this player's turn"), separate
        // from drop resolution's own eligibility check - an ineligible card shouldn't even
        // spawn a ghost when picked up.
        public void SetCard(CardData cardData, Action<CardData, CardView, PointerEventData> dragEndedCallback = null, Action<CardData, CardView> dragBeganCallback = null, Func<bool> canDragPredicate = null)
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

            WriteCardText();

            if (cardBackground != null)
            {
                cardBackground.color = normalColor;
            }
        }

        public void SetCardForMulligan(CardData cardData, Action<CardView> toggleCallback)
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
                    // Pass this CardView, not `card` - two duplicate-copy slots share the same
                    // CardData reference, so a caller keying off CardData can't tell them apart.
                    onMulliganToggled?.Invoke(this);
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
                nameText.text = card.cardName;
            }
            else
            {
                Debug.LogWarning("CardView: 'nameText' is not assigned in the Inspector — card name will not render.", this);
            }

            if (costText != null)
            {
                costText.text = card.manaCost.ToString();
            }
            else
            {
                Debug.LogWarning("CardView: 'costText' is not assigned in the Inspector — card mana cost will not render.", this);
            }

            if (statsText != null)
            {
                statsText.text = card.cardType == CardType.Minion
                    ? $"{card.attack} / {card.health}"
                    : "";
            }
            else
            {
                Debug.LogWarning("CardView: 'statsText' is not assigned in the Inspector — card attack/health will not render.", this);
            }

            if (artworkImage != null)
            {
                if (card.artwork != null)
                {
                    artworkImage.sprite = card.artwork;
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