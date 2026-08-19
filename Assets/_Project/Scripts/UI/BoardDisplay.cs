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

        // Rebuilt every RenderBoard call (views are destroyed/recreated each refresh),
        // so a Minion's Transform is only ever valid until the next refresh - callers
        // must resolve it fresh rather than caching it.
        private Dictionary<Minion, Transform> minionViewTransforms = new Dictionary<Minion, Transform>();

        public void RenderBoard(List<Minion> minions, Action<Minion> onMinionClicked = null, Minion selectedAttacker = null, bool showAttackEligibility = false)
        {
            if (boardPanel == null)
            {
                Debug.LogWarning("BoardDisplay.RenderBoard called with no boardPanel assigned.", this);
                return;
            }

            foreach (Transform child in boardPanel)
            {
                Destroy(child.gameObject);
            }

            minionViewTransforms.Clear();

            if (minions == null) return;
            if (minionViewPrefab == null)
            {
                Debug.LogWarning("BoardDisplay.RenderBoard has no minionViewPrefab assigned.", this);
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
                    Debug.LogWarning("Instantiated minion prefab has no MinionView component.", this);
                    Destroy(minionObj);
                    continue;
                }

                bool isSelected = minion == selectedAttacker;
                view.SetMinion(minion, onMinionClicked, isSelected, showAttackEligibility);
                minionViewTransforms[minion] = view.transform;
            }
        }

        // Returns null if the minion isn't currently rendered by this display (e.g. it
        // died, or it belongs to the other player's board).
        public Transform GetViewTransform(Minion minion)
        {
            if (minion == null) return null;
            return minionViewTransforms.TryGetValue(minion, out Transform t) ? t : null;
        }
    }
}