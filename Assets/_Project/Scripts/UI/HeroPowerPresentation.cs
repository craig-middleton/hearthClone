using UnityEngine;
using UnityEngine.UI;
using HearthstoneClone.Core;

namespace HearthstoneClone.UI
{
    /// <summary>Reflects the existing power's state without changing its rules or input.</summary>
    public class HeroPowerPresentation : MonoBehaviour
    {
        public Image artwork;
        public Image usedBack;
        public GameObject readyGlow;
        private Player player;

        public void SetPlayer(Player actingPlayer) => player = actingPlayer;

        void LateUpdate()
        {
            if (player == null) return;
            bool used = player.HasUsedHeroPowerThisTurn;
            if (artwork != null)
            {
                artwork.enabled = !used;
                artwork.color = player.CurrentMana >= 2 ? Color.white : new Color(.7f, .7f, .7f);
            }
            if (usedBack != null) usedBack.enabled = used;
            if (readyGlow != null) readyGlow.SetActive(!used && player.CurrentMana >= 2);
        }
    }
}
