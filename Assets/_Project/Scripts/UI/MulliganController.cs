using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HearthstoneClone.Cards;

namespace HearthstoneClone.UI
{
    // Owns the pre-game mulligan phase: selection UI, confirm handling, and the
    // MulliganComplete flag every other system gates on. Constructed once in
    // EffectTester.Start() and stays alive only so MulliganComplete remains queryable -
    // ShowMulliganUI()/OnConfirmMulliganClicked() each run exactly once per match.
    public class MulliganController
    {
        private readonly PlayerHand playerOneHand;
        private readonly Transform mulliganPanel;
        private readonly GameObject cardViewPrefab;
        private readonly Button confirmMulliganButton;
        private readonly Action onMulliganComplete;

        // Keyed by CardView instance, not CardData - two duplicate-copy slots share the same
        // CardData reference (see PROJECT_STATUS's CardData reference-identity entry), so a
        // HashSet<CardData> can't represent "both copies selected" and a second toggle on the
        // duplicate silently cancels the first (Constraint 17 fixed the same class of bug for
        // the old click-to-target pending-selection state the same way).
        private readonly HashSet<CardView> mulliganSelections = new HashSet<CardView>();
        private readonly List<GameObject> mulliganCardObjects = new List<GameObject>();

        public bool MulliganComplete { get; private set; } = false;

        public MulliganController(PlayerHand playerOneHand, Transform mulliganPanel, GameObject cardViewPrefab, Button confirmMulliganButton, Action onMulliganComplete)
        {
            this.playerOneHand = playerOneHand;
            this.mulliganPanel = mulliganPanel;
            this.cardViewPrefab = cardViewPrefab;
            this.confirmMulliganButton = confirmMulliganButton;
            this.onMulliganComplete = onMulliganComplete;

            if (confirmMulliganButton != null)
            {
                confirmMulliganButton.onClick.AddListener(OnConfirmMulliganClicked);
            }
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

            foreach (CardData card in playerOneHand.Hand)
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
        }

        private void OnMulliganCardToggled(CardView view)
        {
            if (mulliganSelections.Contains(view))
            {
                mulliganSelections.Remove(view);
            }
            else
            {
                mulliganSelections.Add(view);
            }
        }

        private void OnConfirmMulliganClicked()
        {
            if (MulliganComplete) return;

            // One CardData entry per selected CardView, including repeats when both copies of
            // a duplicate card are selected - PlayerHand.MulliganCards only needs the list to
            // have the right count per reference, it doesn't care that two entries are equal.
            var cardsToMulligan = new List<CardData>();
            foreach (CardView view in mulliganSelections)
            {
                cardsToMulligan.Add(view.Card);
            }
            playerOneHand.MulliganCards(cardsToMulligan);

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
