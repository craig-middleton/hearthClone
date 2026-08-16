using System;
using System.Collections.Generic;
using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.UI
{
    public class BoardDisplay : MonoBehaviour
    {
        public GameObject minionViewPrefab;
        public Transform boardPanel;

        public void RenderBoard(List<Minion> minions, Action<Minion> onMinionClicked = null, Minion selectedAttacker = null, bool showAttackEligibility = false)
        {
            if (boardPanel == null)
            {
                Debug.LogWarning("BoardDisplay.RenderBoard called with no boardPanel assigned.");
                return;
            }

            foreach (Transform child in boardPanel)
            {
                Destroy(child.gameObject);
            }

            if (minions == null) return;
            if (minionViewPrefab == null)
            {
                Debug.LogWarning("BoardDisplay.RenderBoard has no minionViewPrefab assigned.");
                return;
            }

            foreach (Minion minion in minions)
            {
                if (minion == null) continue;
                if (minion.IsDead) continue;

                GameObject minionObj = Instantiate(minionViewPrefab, boardPanel);
                MinionView view = minionObj.GetComponent<MinionView>();
                if (view == null)
                {
                    Debug.LogWarning("Instantiated minion prefab has no MinionView component.");
                    Destroy(minionObj);
                    continue;
                }

                bool isSelected = minion == selectedAttacker;
                view.SetMinion(minion, onMinionClicked, isSelected, showAttackEligibility);
            }
        }
    }
}