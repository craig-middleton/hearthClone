using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HearthstoneClone.Cards;

namespace HearthstoneClone.UI
{
    // Owns the pre-game mulligan phase: selection UI, confirm handling, and the
    // MulliganComplete flag every other system gates on. Constructed once per game in
    // EffectTester.BeginNewGame() (called from Start(), and again on restart) and stays alive
    // only so MulliganComplete remains queryable - ShowMulliganUI()/OnConfirmMulliganClicked()
    // each run exactly once per match.
    public class MulliganController
    {
        private readonly PlayerHand playerOneHand;
        private readonly Transform mulliganPanel;
        private readonly GameObject cardViewPrefab;
        private readonly Button confirmMulliganButton;
        private readonly Action onMulliganComplete;

        // Keyed by CardInstance, which now has real per-copy identity (root fix for the
        // CardData reference-identity issue - see PROJECT_STATUS). Previously this had to key
        // off CardView instead of CardData, since two duplicate-copy slots shared the same
        // CardData reference and a HashSet<CardData> couldn't represent "both copies selected."
        private readonly HashSet<CardInstance> mulliganSelections = new HashSet<CardInstance>();
        private readonly List<GameObject> mulliganCardObjects = new List<GameObject>();

        public bool MulliganComplete { get; private set; } = false;

        // confirmMulliganButton's onClick is NOT wired here - a restart (EffectTester's
        // BeginNewGame) constructs a new MulliganController per game, and wiring the button
        // from inside the constructor would stack a second listener bound to the OLD instance
        // on every restart. EffectTester.Start() wires it exactly once, as a trampoline that
        // calls through the CURRENT mulliganController field, so restart just swaps what the
        // trampoline delegates to instead of adding another listener.
        public MulliganController(PlayerHand playerOneHand, Transform mulliganPanel, GameObject cardViewPrefab, Button confirmMulliganButton, Action onMulliganComplete)
        {
            this.playerOneHand = playerOneHand;
            this.mulliganPanel = mulliganPanel;
            this.cardViewPrefab = cardViewPrefab;
            this.confirmMulliganButton = confirmMulliganButton;
            this.onMulliganComplete = onMulliganComplete;
        }

        public void ShowMulliganUI()
        {
            if (mulliganPanel == null || cardViewPrefab == null)
            {
                Debug.LogWarning("Mulligan UI not wired up — skipping mulligan phase.");
                MulliganComplete = true;
                onMulliganComplete?.Invoke();
                return;
            }

            mulliganSelections.Clear();

            foreach (Transform child in mulliganPanel)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
            mulliganCardObjects.Clear();

            foreach (CardInstance card in playerOneHand.Hand)
            {
                if (card == null) continue;

                GameObject cardObj = UnityEngine.Object.Instantiate(cardViewPrefab, mulliganPanel);
                CardView view = cardObj.GetComponent<CardView>();
                if (view == null)
                {
                    Debug.LogWarning("Instantiated card prefab has no CardView component.");
                    UnityEngine.Object.Destroy(cardObj);
                    continue;
                }

                view.SetCardForMulligan(card, OnMulliganCardToggled);
                mulliganCardObjects.Add(cardObj);
            }

            // Forces mulliganPanel's HorizontalLayoutGroup to recompute immediately rather than
            // waiting on Unity's automatic next-frame rebuild. Needed specifically on restart:
            // EffectTester.BeginNewGame() reactivates mulliganPanel via SetActive(true) and this
            // method populates it with fresh cards in the same call - unlike a fresh scene load,
            // where the panel has been active (and its layout group already settled) since
            // before Start() ever ran. Without this, restart's mulligan cards can render
            // squashed/overlapping until something else happens to trigger a relayout.
            if (mulliganPanel is RectTransform mulliganRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(mulliganRect);
            }
        }

        private void OnMulliganCardToggled(CardInstance card)
        {
            if (mulliganSelections.Contains(card))
            {
                mulliganSelections.Remove(card);
            }
            else
            {
                mulliganSelections.Add(card);
            }
        }

        // Public - EffectTester.Start() wires this as the confirmMulliganButton listener
        // exactly once (see the constructor's comment above).
        public void OnConfirmMulliganClicked()
        {
            if (MulliganComplete) return;

            playerOneHand.MulliganCards(new List<CardInstance>(mulliganSelections));

            mulliganSelections.Clear();

            foreach (GameObject obj in mulliganCardObjects)
            {
                UnityEngine.Object.Destroy(obj);
            }
            mulliganCardObjects.Clear();

            if (mulliganPanel != null)
            {
                mulliganPanel.gameObject.SetActive(false);
            }

            if (confirmMulliganButton != null)
            {
                confirmMulliganButton.gameObject.SetActive(false);
            }

            MulliganComplete = true;

            onMulliganComplete?.Invoke();
        }
    }
}
