using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using HearthstoneClone.Core;

namespace HearthstoneClone.UI
{
    public class FaceView : MonoBehaviour
    {
        public TMP_Text healthText;
        public Button button;
        public Image avatarImage;

        [Header("Idle Animation")]
        public float breathScaleAmount = 0.03f;
        public float breathSpeed = 1.2f;
        public float swayAmount = 3f;
        public float swaySpeed = 0.8f;

        [Header("Spell Reaction")]
        public float reactionDuration = 0.25f;
        public Color damageFlashColor = new Color(1f, 0.3f, 0.3f);

        private Player player;
        private Action<Player> onClicked;

        public Player Player => player;

        private RectTransform avatarRect;
        private Vector3 avatarBaseScale;
        private Vector3 avatarBasePosition;
        private Color avatarBaseColor = Color.white;
        private Coroutine reactionRoutine;

        public void SetPlayer(Player playerData, Action<Player> clickCallback)
        {
            if (playerData == null)
            {
                Debug.LogWarning("FaceView.SetPlayer called with a null Player — skipping.", this);
                return;
            }

            player = playerData;
            onClicked = clickCallback;

            if (healthText != null)
            {
                healthText.text = $"{player.PlayerName}: {player.Health} HP\nMana: {player.CurrentMana}/{player.MaxMana}";
            }
            else
            {
                Debug.LogWarning("FaceView: 'healthText' is not assigned in the Inspector — player name, health and mana will not render.", this);
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClicked?.Invoke(player));
            }
            else
            {
                Debug.LogWarning("FaceView: 'button' is not assigned in the Inspector — this face cannot be clicked to be attacked.", this);
            }

            if (avatarImage != null && avatarRect == null)
            {
                avatarRect = avatarImage.rectTransform;
                avatarBaseScale = avatarRect.localScale;
                avatarBasePosition = avatarRect.localPosition;
                avatarBaseColor = avatarImage.color;
            }
        }

        // Called by SpellAnimationSequencer when a damage spell's travel effect lands.
        // Only tints avatarImage.color - Update() below drives avatarRect's scale/position
        // every frame for the idle sway, so a reaction that touched position/scale directly
        // would just get overwritten the next frame instead of composing with it.
        public void PlayDamageReaction()
        {
            if (avatarImage == null) return;

            if (reactionRoutine != null)
            {
                StopCoroutine(reactionRoutine);
            }
            reactionRoutine = StartCoroutine(FlashRoutine(damageFlashColor));
        }

        private IEnumerator FlashRoutine(Color flashColor)
        {
            float elapsed = 0f;
            while (elapsed < reactionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / reactionDuration);
                float intensity = 1f - Mathf.Abs((t * 2f) - 1f);
                avatarImage.color = Color.Lerp(avatarBaseColor, flashColor, intensity);
                yield return null;
            }

            avatarImage.color = avatarBaseColor;
            reactionRoutine = null;
        }

        void Update()
        {
            if (avatarRect == null) return;

            float breath = 1f + Mathf.Sin(Time.time * breathSpeed) * breathScaleAmount;
            avatarRect.localScale = avatarBaseScale * breath;

            float sway = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
            avatarRect.localPosition = avatarBasePosition + new Vector3(sway, 0f, 0f);
        }
    }
}