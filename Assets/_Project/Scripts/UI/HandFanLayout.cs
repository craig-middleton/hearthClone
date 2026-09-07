using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    // Curved "fanning hand" layout for HandPanel, replacing its HorizontalLayoutGroup. The
    // group is DISABLED rather than deleted: LayoutGroup.OnDisable clears its
    // DrivenRectTransformTracker, cleanly releasing the anchor/anchoredPosition properties it
    // was driving, and leaves re-ticking one checkbox as a complete revert if the fan
    // misbehaves mid-playtest.
    //
    // No LayoutRebuilder.ForceRebuildLayoutImmediate is needed after a hand rebuild - unlike
    // MulliganController, which does need one. That call exists there because a
    // HorizontalLayoutGroup computes its positions during Unity's deferred canvas layout pass;
    // this script computes positions itself and writes them straight to the RectTransform, so
    // there is nothing deferred to force.
    //
    // All geometry is in canvas units and deliberately NOT derived from handPanel.rect.width.
    // On the restart path (EffectTester.BeginNewGame) a panel is SetActive(true) and populated
    // in the same call, and reading a not-yet-settled rect there is exactly what produced the
    // mulligan panel's squashed-card bug. The CanvasScaler is ScaleWithScreenSize at 1920x1080
    // matching width, so fixed canvas-unit geometry is already resolution-independent.
    public class HandFanLayout : MonoBehaviour
    {
        [Header("Arc Geometry (canvas units)")]
        [Tooltip("Horizontal spacing per card, before the Max Total Width clamp applies.")]
        public float perCardStep = 110f;

        [Tooltip("Widest the fan may spread. Larger hands compress inward instead of overflowing.")]
        public float maxTotalWidth = 900f;

        [Tooltip("How far the outermost cards sit below the centre card.")]
        public float arcDrop = 40f;

        [Tooltip("Tilt of the outermost cards, in degrees. Z axis only.")]
        public float maxTiltDegrees = 12f;

        // Takes the freshly instantiated views as an explicit list rather than walking this
        // transform's children on purpose: Destroy() is deferred to end-of-frame, so partway
        // through HandDisplay.RenderHand the panel holds BOTH the previous hand's
        // pending-destroy CardViews and the new ones. Indexing off children would fan the
        // wrong count and put every card in the wrong slot for one frame.
        public void ApplyLayout(List<CardView> views)
        {
            if (views == null) return;

            WarnIfLayoutGroupEnabled();

            int n = views.Count;
            for (int i = 0; i < n; i++)
            {
                CardView view = views[i];
                if (view == null) continue;

                RectTransform rect = view.transform as RectTransform;
                if (rect == null) continue;

                // Set explicitly rather than trusting the prefab's own values: LayoutGroup's
                // SetChildAlongAxis force-writes anchorMin/anchorMax to (0,1) on every child it
                // lays out, and those values persist on the RectTransform after the group is
                // disabled. The prefab says (0.5, 0.5); the runtime object may not.
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                float t = n == 1 ? 0.5f : i / (float)(n - 1);
                float offset = t - 0.5f;

                float spread = Mathf.Min(maxTotalWidth, n * perCardStep);
                float x = offset * spread;
                float y = -arcDrop * Mathf.Pow(offset * 2f, 2f);
                float angle = -offset * 2f * maxTiltDegrees;

                rect.anchoredPosition = new Vector2(x, y);

                // Z axis ONLY, and scale is left untouched (never negative). Either an X/Y
                // rotation or a negative scale flips the graphic's facing normal, and the
                // GraphicRaycaster has ignoreReversedGraphics enabled - the card would silently
                // drop out of raycast results and become undraggable with no visible cause.
                rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        // The fan and an enabled LayoutGroup on the same panel cannot coexist: the group
        // re-drives anchoredPosition on the canvas layout pass (after Update, before render),
        // so the cards would snap back to a flat row every frame with nothing on screen
        // explaining why. Warn rather than force-disabling it in code, so that unticking this
        // component stays a complete one-checkbox revert to the old behaviour.
        private void WarnIfLayoutGroupEnabled()
        {
            LayoutGroup group = GetComponent<LayoutGroup>();
            if (group != null && group.enabled)
            {
                Debug.LogWarning($"HandFanLayout on '{name}' found an enabled {group.GetType().Name} on the same GameObject — it will overwrite the fan's positions on the next layout pass. Untick it in the Inspector.", this);
            }
        }
    }
}
