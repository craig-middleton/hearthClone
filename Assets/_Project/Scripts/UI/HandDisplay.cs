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
            if (handPanel == null)
            {
                Debug.LogWarning("HandDisplay.RenderHand called with no handPanel assigned.");
                return;
            }

            foreach (Transform child in handPanel)
            {
                Destroy(child.gameObject);
            }

            if (hand == null) return;
            if (cardViewPrefab == null)
            {
                Debug.LogWarning("HandDisplay.RenderHand has no cardViewPrefab assigned.");
                return;
            }

            foreach (CardData card in hand)
            {
                if (card == null) continue;

                GameObject cardObj = Instantiate(cardViewPrefab, handPanel);
                CardView view = cardObj.GetComponent<CardView>();
                if (view == null)
                {
                    Debug.LogWarning("Instantiated card prefab has no CardView component.");
                    Destroy(cardObj);
                    continue;
                }

                view.SetCard(card, onCardClicked);
            }
        }
    }
}