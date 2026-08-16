using System;
using System.Collections.Generic;
using UnityEngine;
using HearthstoneClone.Cards;

namespace HearthstoneClone.UI
{
    public class HandDisplay : MonoBehaviour
    {
        public GameObject cardViewPrefab;
        public Transform handPanel;

        public void RenderHand(List<CardData> hand, Action<CardData> onCardClicked)
        {
            foreach (Transform child in handPanel)
            {
                Destroy(child.gameObject);
            }

            if (hand == null) return;

            foreach (CardData card in hand)
            {
                if (card == null) continue;

                GameObject cardObj = Instantiate(cardViewPrefab, handPanel);
                CardView view = cardObj.GetComponent<CardView>();
                view.SetCard(card, onCardClicked);
            }
        }
    }
}