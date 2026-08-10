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

        private Player player;
        private Action<Player> onClicked;

        public void SetPlayer(Player playerData, Action<Player> clickCallback)
        {
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
        }
    }
}