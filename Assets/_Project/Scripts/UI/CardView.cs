using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using HearthstoneClone.Cards;

namespace HearthstoneClone.UI
{
    public class CardView : MonoBehaviour
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

        private CardData card;
        private Action<CardData, CardView> onClicked;
        private Action<CardData> onMulliganToggled;
        private bool isSelectedForMulligan;

        public void SetCard(CardData cardData, Action<CardData, CardView> clickCallback)
        {
            if (cardData == null)
            {
                Debug.LogWarning("CardView.SetCard called with a null CardData — skipping.", this);
                return;
            }

            card = cardData;
            onClicked = clickCallback;

            WriteCardText();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClicked?.Invoke(card, this));
            }
            else
            {
                Debug.LogWarning("CardView: 'button' is not assigned in the Inspector — this card cannot be clicked to play.", this);
            }
        }

        public void SetCardForMulligan(CardData cardData, Action<CardData> toggleCallback)
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
    }
}