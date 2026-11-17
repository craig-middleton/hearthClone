using TMPro;
using UnityEngine;

namespace HearthstoneClone.UI
{
    /// <summary>Read-only HUD binding; gameplay remains in the existing controllers.</summary>
    public class MatchPresentation : MonoBehaviour
    {
        public FaceView playerFace;
        public FaceView opponentFace;
        public TMP_Text playerMana;
        public TMP_Text opponentMana;
        public GameObject mulliganPanel;
        public GameObject mulliganHeading;
        public RectTransform mulliganShade;

        private void LateUpdate()
        {
            if (playerFace != null && playerFace.Player != null && playerMana != null)
                playerMana.text = $"{playerFace.Player.CurrentMana}/{playerFace.Player.MaxMana}";
            if (opponentFace != null && opponentFace.Player != null && opponentMana != null)
                opponentMana.text = $"{opponentFace.Player.CurrentMana}/{opponentFace.Player.MaxMana}";
            if (mulliganHeading != null && mulliganPanel != null)
                mulliganHeading.SetActive(mulliganPanel.activeSelf);
            if(mulliganShade!=null&&mulliganShade.gameObject.activeInHierarchy)
                mulliganShade.sizeDelta=((RectTransform)mulliganShade.GetComponentInParent<Canvas>().rootCanvas.transform).rect.size;
        }
    }
}
