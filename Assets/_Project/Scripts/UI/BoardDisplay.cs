using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        private void Awake()
        {
            EnsureRaycastCatcher();
        }

        // Empty board space carries no raycastable Graphic by default - a bare RectTransform
        // panel isn't hit-testable, so EventSystem.current.RaycastAll returns zero hits there
        // even though the board is visually right under the pointer. A fully transparent Image
        // with raycastTarget on gives the whole panel bounds a hittable surface without changing
        // how it looks or affecting the visible board background (a separate, decorative
        // Image elsewhere - not reused here on purpose).
        private void EnsureRaycastCatcher()
        {
            if (boardPanel == null) return;

            Image catcherImage = boardPanel.GetComponent<Image>();
            bool justAdded = catcherImage == null;
            if (justAdded)
            {
                catcherImage = boardPanel.gameObject.AddComponent<Image>();
                catcherImage.color = new Color(0f, 0f, 0f, 0f);
            }
            catcherImage.raycastTarget = true;
        }

        public void RenderBoard(List<Minion> minions, Action<Minion> onMinionClicked = null, Minion selectedAttacker = null, bool showAttackEligibility = false)
        {
            if (boardPanel == null)
            {
                Debug.LogWarning("BoardDisplay.RenderBoard called with no boardPanel assigned.", this);
                return;
            }

            // Pinned indices captured BEFORE any destroy/instantiate touches the hierarchy,
            // so they reflect each held child's actual pre-death board slot. Sorted ascending
            // so the merge below can insert survivors around them in order.
            List<Transform> heldChildren = new List<Transform>();
            List<int> heldPinnedIndices = new List<int>();

            foreach (Transform child in boardPanel)
            {
                // A held view (SpellAnimationSequencer, on a lethal hit) still owns its own
                // destruction - see MinionView.BeginHold/EndHold. Its Minion has already been
                // removed from `minions` by the time this runs, so skipping it here can't
                // create a duplicate when the rebuild loop below runs.
                MinionView view = child.GetComponent<MinionView>();
                if (view != null && view.IsHeld)
                {
                    heldChildren.Add(child);
                    heldPinnedIndices.Add(child.GetSiblingIndex());
                    continue;
                }

                Destroy(child.gameObject);
            }

            minionViewTransforms.Clear();

            if (minions == null) return;
            if (minionViewPrefab == null)
            {
                Debug.LogWarning("BoardDisplay.RenderBoard has no minionViewPrefab assigned.", this);
                return;
            }

            List<Transform> survivorViews = new List<Transform>();

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
                survivorViews.Add(view.transform);
            }

            // Held views are no longer ignoreLayout (MinionView.BeginHold now uses an
            // overrideSorting Canvas for draw order instead - see MinionView), so they stay
            // counted HorizontalLayoutGroup participants and their sibling index now controls
            // their actual layout slot, not just draw order. To keep survivors from reflowing,
            // rebuild the sibling order as a merge of the held views (pinned to their pre-death
            // index) and the freshly-instantiated survivors (in board order), inserting each
            // held view back at its original relative position instead of leaving it wherever
            // Destroy/Instantiate happened to put it.
            if (heldChildren.Count > 0)
            {
                List<int> sortedHeldOrder = new List<int>();
                for (int i = 0; i < heldChildren.Count; i++) sortedHeldOrder.Add(i);
                sortedHeldOrder.Sort((a, b) => heldPinnedIndices[a].CompareTo(heldPinnedIndices[b]));

                int survivorCursor = 0;
                int nextSiblingIndex = 0;
                foreach (int heldOrderIdx in sortedHeldOrder)
                {
                    int pinnedIndex = heldPinnedIndices[heldOrderIdx];
                    while (survivorCursor < survivorViews.Count && nextSiblingIndex < pinnedIndex)
                    {
                        survivorViews[survivorCursor].SetSiblingIndex(nextSiblingIndex);
                        survivorCursor++;
                        nextSiblingIndex++;
                    }

                    heldChildren[heldOrderIdx].SetSiblingIndex(nextSiblingIndex);
                    nextSiblingIndex++;
                }

                while (survivorCursor < survivorViews.Count)
                {
                    survivorViews[survivorCursor].SetSiblingIndex(nextSiblingIndex);
                    survivorCursor++;
                    nextSiblingIndex++;
                }
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