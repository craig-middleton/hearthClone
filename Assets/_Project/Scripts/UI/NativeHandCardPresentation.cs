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
        EffectTester game;

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

            if (game == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                    game = canvas.rootCanvas.GetComponentInChildren<EffectTester>(true);
            }
            if (game == null)
            {
                Hide();
                return;
            }

            Player owner = null;
            if (transform.IsChildOf(game.handDisplay.handPanel))
                owner = game.faceView.Player;
            else if (transform.IsChildOf(game.opponentHandDisplay.handPanel))
                owner = game.opponentFaceView.Player;

            var data = view.Card.Data;
            bool minion = data.cardType == CardType.Minion;
            bool available = owner != null && owner.CurrentMana >= data.manaCost &&
                (!minion || owner.BoardMinions.Count < 7);
            var face = view.nativeFace;
            minionOutline.enabled = available && minion && face != null && face.minionFace.activeInHierarchy;
            spellOutline.enabled = available && !minion && face != null && face.spellFace.activeInHierarchy;
        }
    }
}
