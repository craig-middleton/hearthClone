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

        // Keyed by CardInstance, which now has real per-copy identity (root fix for the
        // CardData reference-identity issue - see PROJECT_STATUS). Previously this had to key
        // off CardView instead of CardData, since two duplicate-copy slots shared the same
        // CardData reference and a HashSet<CardData> couldn't represent "both copies selected."
        private readonly HashSet<CardInstance> mulliganSelections = new HashSet<CardInstance>();
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

        private void OnConfirmMulliganClicked()
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
