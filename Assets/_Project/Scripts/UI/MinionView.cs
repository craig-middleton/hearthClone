using System.Collections;
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

        [Header("Spell Reaction")]
        public float reactionDuration = 0.25f;
        public Color damageFlashColor = new Color(1f, 0.3f, 0.3f);

        private Minion minion;
        private Action<Minion> onClicked;
        private Coroutine reactionRoutine;

        public Minion Minion => minion;

        public void SetMinion(Minion minionData, Action<Minion> clickCallback = null, bool isSelected = false, bool showAttackEligibility = false)
        {
            if (minionData == null)
            {
                Debug.LogWarning("MinionView.SetMinion called with a null Minion — skipping.", this);
                return;
            }

            minion = minionData;
            onClicked = clickCallback;

            if (nameText != null)
            {
                nameText.text = minion.HasTaunt ? $"{minion.MinionName} (Taunt)" : minion.MinionName;
            }
            else
            {
                Debug.LogWarning("MinionView: 'nameText' is not assigned in the Inspector — minion name will not render.", this);
            }

            if (statsText != null)
            {
                statsText.text = $"{minion.CurrentAttack} / {minion.CurrentHealth}";
            }
            else
            {
                Debug.LogWarning("MinionView: 'statsText' is not assigned in the Inspector — minion attack/health will not render.", this);
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
            else
            {
                Debug.LogWarning("MinionView: 'button' is not assigned in the Inspector — this minion cannot be clicked or selected to attack.", this);
            }
        }

        // Called by SpellAnimationSequencer when a damage spell's travel effect lands on
        // this minion. Captures whatever color minionBackground currently shows (selected /
        // cannot-attack / normal) as the flash baseline and restores it afterward, since
        // unlike FaceView there's no continuous per-frame recompute to just let overwrite it.
        public void PlayDamageReaction()
        {
            if (minionBackground == null) return;

            if (reactionRoutine != null)
            {
                StopCoroutine(reactionRoutine);
            }
            reactionRoutine = StartCoroutine(FlashRoutine(damageFlashColor));
        }

        private IEnumerator FlashRoutine(Color flashColor)
        {
            Color baseColor = minionBackground.color;
            float elapsed = 0f;
            while (elapsed < reactionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / reactionDuration);
                float intensity = 1f - Mathf.Abs((t * 2f) - 1f);
                minionBackground.color = Color.Lerp(baseColor, flashColor, intensity);
                yield return null;
            }

            minionBackground.color = baseColor;
            reactionRoutine = null;
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