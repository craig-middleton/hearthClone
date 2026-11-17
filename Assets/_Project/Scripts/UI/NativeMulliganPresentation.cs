using TMPro;
using UnityEngine;
namespace HearthstoneClone.UI
{
    public class NativeMulliganPresentation : MonoBehaviour
    {
        public NativeMulliganArt library;
        public TMP_FontAsset font;
        public Material outlinedText;
        public Vector2 cardSize=new Vector2(300,450);
        public void PrepareCard(RectTransform card){card.sizeDelta=cardSize;}
        public void Decorate(CardView view)
        {
            var marker=view.gameObject.AddComponent<NativeMulliganCard>();
            var badgePosition=new Vector2(0,-cardSize.y*.5f-40);
            marker.keep=Part("Keep",view.transform,"keep",badgePosition,210).gameObject;
            marker.replace=Part("Replace",view.transform,"replace",badgePosition,210).gameObject;
            var label=new GameObject("Replace text",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(marker.replace.transform,false);
            var rect=(RectTransform)label.transform;rect.sizeDelta=new Vector2(160,42);
            var text=label.GetComponent<TextMeshProUGUI>();text.text="REPLACE";text.font=font;text.fontSharedMaterial=outlinedText;text.fontSize=25;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
            marker.cross=Part("Original red X",view.transform,"cross",new Vector2(0,25),cardSize.x*.75f).gameObject;
            marker.SetSelected(false);
        }
        NativeMulliganPart Part(string name,Transform parent,string key,Vector2 position,float width)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(NativeMulliganPart));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.anchoredPosition=position;
            rect.sizeDelta=new Vector2(width,width/library.Get(key).aspect);
            var part=go.GetComponent<NativeMulliganPart>();part.library=library;part.shape=key;part.raycastTarget=false;return part;
        }
    }
}
