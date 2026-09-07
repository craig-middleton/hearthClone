using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

        [Header("Hover")]
        [Tooltip("How far the hovered card rises above its arc slot.")]
        public float liftHeight = 60f;

        [Tooltip("Uniform scale applied to the hovered card. Must stay positive.")]
        public float hoverScale = 1.15f;

        [Tooltip("Higher settles onto the hover target faster. Framerate-independent.")]
        public float hoverAnimationSpeed = 12f;

        [Header("Neighbour Spread")]
        [Tooltip("How far the nearest neighbours of the hovered card are pushed aside to open a gap.")]
        public float spreadAmount = 50f;

        [Tooltip("How many cards on each side of the hovered card are affected. Cards farther than this keep their plain arc slot.")]
        public float spreadRange = 2f;

        [Header("Hover Exit Hysteresis")]
        [Tooltip("Extra margin (local rect units) added around the hovered card's current on-screen rect before an exit actually clears hover. Prevents a neighbour sliding sideways under a stationary pointer from oscillating hover on/off.")]
        public float exitHysteresisPadding = 24f;

        [Tooltip("Seconds of continuous no-hover before draw order is restored to hand order. Keeps a fast sweep across the hand from popping sibling order every frame.")]
        public float restoreSiblingOrderDelay = 0.12f;

        // Copied from the caller's list rather than aliased, so nothing outside can mutate what
        // Update is iterating between frames.
        private readonly List<CardView> views = new List<CardView>();

        // Held as a CardView reference, not an index: a rebuild replaces every view, and an
        // index would silently point at a different card afterwards.
        private CardView hoveredView;

        private bool dragActive;

        // Set when hover clears and cleared again if hover resumes before the delay elapses -
        // this is what makes the sibling-order restore lazy (fires once the fan is settled with
        // nothing hovered) instead of popping on every exit during a fast sweep.
        private bool pendingSiblingRestore;
        private float pendingSiblingRestoreTimer;

        // Takes the freshly instantiated views as an explicit list rather than walking this
        // transform's children on purpose: Destroy() is deferred to end-of-frame, so partway
        // through HandDisplay.RenderHand the panel holds BOTH the previous hand's
        // pending-destroy CardViews and the new ones. Indexing off children would fan the
        // wrong count and put every card in the wrong slot for one frame.
        public void ApplyLayout(List<CardView> renderedViews)
        {
            if (renderedViews == null) return;

            WarnIfLayoutGroupEnabled();

            views.Clear();
            for (int i = 0; i < renderedViews.Count; i++)
            {
                if (renderedViews[i] != null) views.Add(renderedViews[i]);
            }

            // The previously hovered card is one of the CardViews RenderHand just destroyed, so
            // carrying the reference over would leave the layout hovering a dead object.
            hoveredView = null;

            // Self-heals the drag flag instead of depending on CardHoverFan.OnEndDrag running
            // after CardView's (component order decides that, and the rebuild CardView triggers
            // destroys the very CardHoverFan still owing us its OnEndDrag). Safe unconditionally:
            // RefreshHandDisplay is gated on CardDragResolver.DragInProgress, so the only rebuild
            // that can reach here during a drag gesture is the one the drop itself triggered -
            // by which point the gesture is already over.
            dragActive = false;

            // Snapped, not lerped: these views were instantiated this frame and still sit at the
            // prefab's origin, so lerping would animate the entire hand in from the centre on
            // every draw and every card played.
            for (int i = 0; i < views.Count; i++)
            {
                ApplyToCard(views[i], i, views.Count, hoveredIndex: -1, snap: true);
            }
        }

        public void SetHovered(CardView view)
        {
            if (view == null) return;

            // Cards the pointer sweeps across while a drag is in flight must not pop up under
            // the cursor - the EventSystem keeps sending enter/exit during a drag, since the
            // ghost has blocksRaycasts off and never covers them.
            if (dragActive) return;

            if (hoveredView == view) return;
            hoveredView = view;

            // Hover resumed before the lazy restore fired - cancel it so draw order stays put.
            pendingSiblingRestore = false;

            // Draw order only. Deliberately NOT the nested-Canvas approach from Constraint 23:
            // a Canvas re-registers its descendants' Graphics to itself (Graphic.CacheCanvas
            // resolves to the nearest enabled Canvas ancestor) and the root GraphicRaycaster
            // looks up only its OWN canvas, so the card would vanish from raycast results and
            // become unhoverable and undraggable. Safe here because arc slots come from the
            // views list index, never GetSiblingIndex - sibling order is free to mean draw
            // order alone.
            view.transform.SetAsLastSibling();
        }

        // Ignores an exit naming a card that is no longer the hovered one: moving between two
        // adjacent cards can deliver enter for the new card before exit for the old one, and an
        // unconditional clear would drop the hover that just started.
        //
        // eventData is optional (and used) hysteresis input: neighbour spread widens the hover
        // window on the horizontal axis, so a neighbour can slide out from under a stationary
        // pointer and fire a real exit for the hovered card even though the pointer never moved.
        // Before honouring the exit, check whether the pointer is still inside the hovered
        // card's CURRENT on-screen rect (already lifted/scaled/spread by Update) plus a margin -
        // only actually clear once the pointer has left that expanded rect.
        public void ClearHovered(CardView view, PointerEventData eventData = null)
        {
            if (hoveredView != view) return;

            if (eventData != null && StillWithinExpandedRect(view, eventData))
            {
                return;
            }

            hoveredView = null;

            // Lazy: don't pop draw order back to hand order on every exit. A fast sweep across
            // the hand fires exit/enter repeatedly, and restoring immediately each time was
            // visible as a pop; instead the fan settles for a beat with nothing hovered before
            // draw order is restored (see Update).
            pendingSiblingRestore = true;
            pendingSiblingRestoreTimer = 0f;
        }

        private bool StillWithinExpandedRect(CardView view, PointerEventData eventData)
        {
            RectTransform rect = view.transform as RectTransform;
            if (rect == null) return false;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                return false;
            }

            Rect r = rect.rect;
            r.xMin -= exitHysteresisPadding;
            r.xMax += exitHysteresisPadding;
            r.yMin -= exitHysteresisPadding;
            r.yMax += exitHysteresisPadding;
            return r.Contains(localPoint);
        }

        public void SetDragActive(bool active)
        {
            dragActive = active;

            if (active && hoveredView != null)
            {
                hoveredView = null;
                pendingSiblingRestore = false;
                RestoreSiblingOrder();
            }
        }

        // Eases every card toward its current target. Recomputing the target each frame rather
        // than caching one means a hover change needs no explicit "recompute" call - it just
        // changes where the existing lerp is heading, which is what makes rapid hover changes
        // interruption-safe.
        private void Update()
        {
            int n = views.Count;
            if (n == 0) return;

            int hoveredIndex = hoveredView != null ? views.IndexOf(hoveredView) : -1;

            for (int i = 0; i < n; i++)
            {
                ApplyToCard(views[i], i, n, hoveredIndex, snap: false);
            }

            if (pendingSiblingRestore)
            {
                pendingSiblingRestoreTimer += Time.deltaTime;
                if (pendingSiblingRestoreTimer >= restoreSiblingOrderDelay)
                {
                    pendingSiblingRestore = false;
                    RestoreSiblingOrder();
                }
            }
        }

        private void ApplyToCard(CardView view, int i, int n, int hoveredIndex, bool snap)
        {
            if (view == null) return;

            RectTransform rect = view.transform as RectTransform;
            if (rect == null) return;

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
            float scale = 1f;

            // Neighbour spread: push the cards near the hovered one sideways to open a gap.
            // Falloff hits zero at spreadRange, so cards farther out keep their plain arc slot.
            if (hoveredIndex >= 0 && i != hoveredIndex)
            {
                int distance = i - hoveredIndex;
                float dir = Mathf.Sign(distance);
                float absDistance = Mathf.Abs(distance);
                float falloff = spreadRange > 0f ? Mathf.Clamp01(1f - absDistance / spreadRange) : 0f;
                x += dir * spreadAmount * falloff;
            }

            // spread is already capped at maxTotalWidth regardless of hand size, so this bound
            // does not grow with n - it is a fixed safety net, not a per-hand-size scale-down.
            float maxX = spread * 0.5f + spreadAmount;
            x = Mathf.Clamp(x, -maxX, maxX);

            if (view == hoveredView)
            {
                y += liftHeight;
                angle = 0f;
                scale = Mathf.Max(0.01f, hoverScale);
            }

            Vector2 targetPosition = new Vector2(x, y);

            // Z axis ONLY, and scale stays uniform and positive. Either an X/Y rotation or a
            // negative scale flips the graphic's facing normal, and the GraphicRaycaster has
            // ignoreReversedGraphics enabled - the card would silently drop out of raycast
            // results and become undraggable with no visible cause.
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 targetScale = new Vector3(scale, scale, 1f);

            if (snap)
            {
                rect.anchoredPosition = targetPosition;
                rect.localRotation = targetRotation;
                rect.localScale = targetScale;
                return;
            }

            // Exponential smoothing rather than a raw deltaTime multiply, so the settle rate is
            // the same at 60fps and 144fps.
            float k = 1f - Mathf.Exp(-hoverAnimationSpeed * Time.deltaTime);

            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, k);
            rect.localRotation = Quaternion.Slerp(rect.localRotation, targetRotation, k);
            rect.localScale = Vector3.Lerp(rect.localScale, targetScale, k);
        }

        // Puts draw order back in hand order once nothing is hovered. Without this, the last
        // hovered card keeps rendering above the cards to its right after the pointer leaves,
        // which reads as wrong in a fan where each card should overlap the one before it.
        private void RestoreSiblingOrder()
        {
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i] != null) views[i].transform.SetSiblingIndex(i);
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
