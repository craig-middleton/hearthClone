using UnityEngine;
using TMPro;
using HearthstoneClone.Cards;

namespace HearthstoneClone.UI
{
    public class CardView : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text nameText;
        public TMP_Text costText;
        public TMP_Text statsText;

        public void SetCard(CardData card)
        {
            nameText.text = card.cardName;
            costText.text = card.manaCost.ToString();

            statsText.text = card.cardType == CardType.Minion
                ? $"{card.attack} / {card.health}"
                : "";
        }
    }
}