using UnityEngine;
namespace HearthstoneClone.UI
{
    public class NativeMulliganCard : MonoBehaviour
    {
        public GameObject keep,replace,cross;
        public UnityEngine.UI.Graphic readyOutline;
        public void SetSelected(bool selected)
        {if(readyOutline!=null)readyOutline.enabled=!selected;keep.SetActive(!selected);replace.SetActive(selected);cross.SetActive(selected);}
    }
}
