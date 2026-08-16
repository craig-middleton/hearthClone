using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using HearthstoneClone.Core;

namespace HearthstoneClone.UI
{
    public class MinionView : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text nameText;
        public TMP_Text statsText;
        public Button button;
        public Image minionBackground;
        public Image artworkImage;

        [Header("Visuals")]
        public Color normalColor = Color.white;
        public Color selectedColor = new Color(0.6f, 0.9f, 0.6f);
        public Color cannotAttackColor = new Color(0.45f, 0.45f, 0.45f);

        private Minion minion;
        private Action<Minion> onClicked;

        public void SetMinion(Minion minionData, Action<Minion> clickCallback = null, bool isSelected = false, bool showAttackEligibility = false)
        {
            if (minionData == null)
            {
                Debug.LogWarning("MinionView.SetMinion called with a null Minion — skipping.");
                return;
            }

            minion = minionData;
            onClicked = clickCallback;

            if (nameText != null)
            {
                nameText.text = minion.HasTaunt ? $"{minion.MinionName} (Taunt)" : minion.MinionName;
            }

            if (statsText != null)
            {
                statsText.text = $"{minion.CurrentAttack} / {minion.CurrentHealth}";
            }

            if (artworkImage != null)
            {
                if (minion.Artwork != null)
                {
                    artworkImage.sprite = minion.Artwork;
                    artworkImage.enabled = true;
                }
                else
                {
                    artworkImage.enabled = false;
                }
            }

            UpdateVisual(isSelected, showAttackEligibility);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClicked?.Invoke(minion));
            }
        }

        private void UpdateVisual(bool isSelected, bool showAttackEligibility)
        {
            if (minionBackground == null) return;

            if (isSelected)
            {
                minionBackground.color = selectedColor;
            }
            else if (showAttackEligibility && !minion.CanAttack)
            {
                minionBackground.color = cannotAttackColor;
            }
            else
            {
                minionBackground.color = normalColor;
            }
        }
    }
}