using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    // Original preblurred silhouettes follow the hand's existing playability state.
    public class NativeHandCardPresentation : MonoBehaviour
    {
        public Graphic minionOutline;
        public Graphic spellOutline;

        CardView view;
        Player owner;
        Transform handPanel;

        public void Bind(Player player, Transform panel)
        {
            owner = player;
            handPanel = panel;
        }

        void Awake()
        {
            view = GetComponent<CardView>();
            Hide();
        }

        void OnDisable() => Hide();

        void Hide()
        {
            if (minionOutline != null) minionOutline.enabled = false;
            if (spellOutline != null) spellOutline.enabled = false;
        }

        void LateUpdate()
        {
            if (view == null || view.Card == null || !view.CanDragForPresentation ||
                minionOutline == null || spellOutline == null)
            {
                Hide();
                return;
            }

            var data = view.Card.Data;
            bool minion = data.cardType == CardType.Minion;
            bool available = owner != null && handPanel != null && transform.IsChildOf(handPanel) &&
                owner.CurrentMana >= data.manaCost &&
                (!minion || owner.BoardMinions.Count < 7);
            var face = view.nativeFace;
            minionOutline.enabled = available && minion && face != null && face.minionFace.activeInHierarchy;
            spellOutline.enabled = available && !minion && face != null && face.spellFace.activeInHierarchy;
        }
    }
}
