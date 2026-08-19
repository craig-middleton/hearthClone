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

        // onCardClicked's third argument is the clicked card's index in `hand` at render
        // time - passed through so callers can distinguish duplicate copies of the same
        // CardData by hand position rather than by a reference two copies both share.
        // pendingSpellIndex (-1 for none) highlights by that same index, for the same reason.
        public void RenderHand(List<CardData> hand, Action<CardData, CardView, int> onCardClicked, int pendingSpellIndex = -1)
        {
            if (handPanel == null)
            {
                Debug.LogWarning("HandDisplay.RenderHand called with no handPanel assigned.", this);
                return;
            }

            foreach (Transform child in handPanel)
            {
                Destroy(child.gameObject);
            }

            if (hand == null) return;
            if (cardViewPrefab == null)
            {
                Debug.LogWarning("HandDisplay.RenderHand has no cardViewPrefab assigned.", this);
                return;
            }

            int index = -1;
            foreach (CardData card in hand)
            {
                index++;
                if (card == null) continue;

                GameObject cardObj = Instantiate(cardViewPrefab, handPanel);
                CardView view = cardObj.GetComponent<CardView>();
                if (view == null)
                {
                    Debug.LogWarning("Instantiated card prefab has no CardView component.", this);
                    Destroy(cardObj);
                    continue;
                }

                int capturedIndex = index;
                view.SetCard(card, (c, v) => onCardClicked?.Invoke(c, v, capturedIndex), index == pendingSpellIndex);
            }
        }
    }
}