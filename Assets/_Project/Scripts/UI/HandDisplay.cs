using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using HearthstoneClone.Cards;

namespace HearthstoneClone.UI
{
    public class HandDisplay : MonoBehaviour
    {
        public GameObject cardViewPrefab;
        public Transform handPanel;

        // faceDown defaults null (treated as "never face-down") so the player's own HandDisplay
        // call site is unaffected - only EffectTester's opponentHandDisplay.RenderHand call
        // passes a predicate. Same predicate-injection pattern as canDrag: evaluated once per
        // render rather than cached, so a manualControlMode toggle takes effect on the next
        // natural refresh with no extra plumbing.
        public void RenderHand(List<CardInstance> hand, Action<CardInstance, CardView, PointerEventData> onCardDragEnded = null, Action<CardInstance, CardView> onCardDragBegan = null, Func<bool> canDrag = null, Func<bool> faceDown = null)
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

            // Collected during the instantiate loop and handed to HandFanLayout below, rather
            // than letting the layout walk handPanel's children itself. Destroy() above is
            // deferred to end-of-frame, so right now handPanel still holds the previous hand's
            // pending-destroy CardViews alongside these new ones - only this list is a true
            // picture of the hand being rendered.
            List<CardView> renderedViews = new List<CardView>();

            // Evaluated once for the whole hand, not per card - every card in a given hand is
            // either all concealed or all revealed together, matching manualControlMode's
            // all-or-nothing effect on the opponent's hand.
            bool isFaceDown = faceDown != null && faceDown();

            foreach (CardInstance card in hand)
            {
                if (card == null) continue;

                GameObject cardObj = Instantiate(cardViewPrefab, handPanel);
                CardView view = cardObj.GetComponent<CardView>();
                if (view == null)
                {
                    Debug.LogWarning("Instantiated card prefab has no CardView component.", this);
                    Destroy(cardObj);
                    continue;
                }

                view.SetCard(card, onCardDragEnded, onCardDragBegan, canDrag, isFaceDown);
                renderedViews.Add(view);
            }

            // Resolved per call off handPanel rather than serialized as its own Inspector
            // field: a panel with no HandFanLayout (OpponentHandPanel, which deliberately keeps
            // its HorizontalLayoutGroup) simply falls through to the layout group as before,
            // with nothing to wire up and no null-check branch at the call site.
            HandFanLayout fanLayout = handPanel.GetComponent<HandFanLayout>();
            if (fanLayout != null)
            {
                fanLayout.ApplyLayout(renderedViews);
            }
        }
    }
}