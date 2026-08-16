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
        private Action<CardData> onClicked;
        private Action<CardData> onMulliganToggled;
        private bool isSelectedForMulligan;

        public void SetCard(CardData cardData, Action<CardData> clickCallback)
        {
            if (cardData == null)
            {
                Debug.LogWarning("CardView.SetCard called with a null CardData — skipping.");
                return;
            }

            card = cardData;
            onClicked = clickCallback;

            WriteCardText();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke(card));
        }

        public void SetCardForMulligan(CardData cardData, Action<CardData> toggleCallback)
        {
            if (cardData == null)
            {
                Debug.LogWarning("CardView.SetCardForMulligan called with a null CardData — skipping.");
                return;
            }

            card = cardData;
            onMulliganToggled = toggleCallback;
            isSelectedForMulligan = false;

            WriteCardText();
            UpdateMulliganVisual();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                isSelectedForMulligan = !isSelectedForMulligan;
                UpdateMulliganVisual();
                onMulliganToggled?.Invoke(card);
            });
        }

        private void WriteCardText()
        {
            nameText.text = card.cardName;
            costText.text = card.manaCost.ToString();

            statsText.text = card.cardType == CardType.Minion
                ? $"{card.attack} / {card.health}"
                : "";

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