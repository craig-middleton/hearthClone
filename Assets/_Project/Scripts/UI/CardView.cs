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

        private CardData card;
        private Action<CardData> onClicked;

        public void SetCard(CardData cardData, Action<CardData> clickCallback)
        {
            card = cardData;
            onClicked = clickCallback;

            nameText.text = card.cardName;
            costText.text = card.manaCost.ToString();

            statsText.text = card.cardType == CardType.Minion
                ? $"{card.attack} / {card.health}"
                : "";

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke(card));
        }
    }
}