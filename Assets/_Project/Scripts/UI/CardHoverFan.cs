using UnityEngine;
using UnityEngine.EventSystems;

namespace HearthstoneClone.UI
{
    // Reports pointer enter/exit (and drag start/end) for one card up to the HandFanLayout on
    // its parent panel. Deliberately holds NO layout maths and no lift state of its own - the
    // layout owns every position, so there is exactly one place that decides where a card sits.
    //
    // Self-disables when its parent has no HandFanLayout, which is what keeps this component
    // safe to sit on the shared CardView prefab. Two callers depend on that:
    //   - Mulligan cards, instantiated into MulliganPanel (HorizontalLayoutGroup, no fan) -
    //     without this guard they would lift on hover during the mulligan screen.
    //   - The drag ghost, which CardView clones into the ROOT CANVAS - a clone of a hovered
    //     card would otherwise keep reporting hover for a card that is no longer in the hand.
    [RequireComponent(typeof(CardView))]
    public class CardHoverFan : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler
    {
        private HandFanLayout fanLayout;
        private CardView cardView;

        // Resolved in OnEnable rather than Awake so it re-resolves if a card is ever pooled or
        // re-activated. Instantiate(prefab, parent) parents the object before OnEnable runs, so
        // the parent lookup here always sees the panel the card was rendered into.
        private void OnEnable()
        {
            cardView = GetComponent<CardView>();
            fanLayout = GetComponentInParent<HandFanLayout>();

            if (fanLayout == null || cardView == null)
            {
                // A disabled Behaviour receives no event-system callbacks at all
                // (ExecuteEvents skips components failing isActiveAndEnabled), so this is a
                // complete no-op, not just a guarded one.
                enabled = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (fanLayout == null) return;
            fanLayout.SetHovered(cardView);
        }

        // Passes the card rather than just clearing: exit for the card being left can arrive
        // after enter for the card being moved onto, and an unconditional clear would then wipe
        // the newly-hovered card. The layout ignores an exit that names a card it no longer
        // considers hovered.
        public void OnPointerExit(PointerEventData eventData)
        {
            if (fanLayout == null) return;
            fanLayout.ClearHovered(cardView);
        }

        // CardView also implements these - ExecuteEvents delivers to every component on the
        // GameObject implementing the interface, so both run. CardView owns the ghost and the
        // play resolution; this only tells the layout to stop hovering, so a card can't sit
        // lifted while its own ghost is being dragged, and so cards the pointer sweeps across
        // mid-drag don't pop up under the cursor.
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (fanLayout == null) return;
            fanLayout.SetDragActive(true);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (fanLayout == null) return;
            fanLayout.SetDragActive(false);
        }
    }
}
