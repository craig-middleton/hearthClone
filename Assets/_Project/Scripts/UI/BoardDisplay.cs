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

            List<Transform> heldChildren = new List<Transform>();

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

            // A held view keeps its OLD sibling index above (skipped, not destroyed/re-added),
            // while every surviving minion above just got a NEW, higher sibling index from
            // Instantiate appending it last. ignoreLayout (MinionView.BeginHold) only excludes
            // the held view from the HorizontalLayoutGroup's position math - sibling index still
            // independently controls draw order in this single-Canvas (no per-view Canvas
            // override) Screen Space - Overlay setup, so without this the held view silently
            // renders behind its freshly-rebuilt siblings. Runs every call, not just once at
            // BeginHold, since the rebuild loop above re-appends survivors with fresh indices
            // on every refresh a hold spans.
            foreach (Transform heldChild in heldChildren)
            {
                heldChild.SetAsLastSibling();
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