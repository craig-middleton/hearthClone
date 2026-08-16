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

        private Player player;
        private Action<Player> onClicked;

        private RectTransform avatarRect;
        private Vector3 avatarBaseScale;
        private Vector3 avatarBasePosition;

        public void SetPlayer(Player playerData, Action<Player> clickCallback)
        {
            if (playerData == null)
            {
                Debug.LogWarning("FaceView.SetPlayer called with a null Player — skipping.");
                return;
            }

            player = playerData;
            onClicked = clickCallback;

            if (healthText != null)
            {
                healthText.text = $"{player.PlayerName}: {player.Health} HP\nMana: {player.CurrentMana}/{player.MaxMana}";
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClicked?.Invoke(player));
            }

            if (avatarImage != null && avatarRect == null)
            {
                avatarRect = avatarImage.rectTransform;
                avatarBaseScale = avatarRect.localScale;
                avatarBasePosition = avatarRect.localPosition;
            }
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