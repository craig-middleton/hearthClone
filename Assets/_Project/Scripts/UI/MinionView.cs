using UnityEngine;
using TMPro;
using HearthstoneClone.Core;

namespace HearthstoneClone.UI
{
    public class MinionView : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text nameText;
        public TMP_Text statsText;

        public void SetMinion(Minion minion)
        {
            nameText.text = minion.MinionName;
            statsText.text = $"{minion.CurrentAttack} / {minion.CurrentHealth}";
        }
    }
}