using UnityEngine;
namespace HearthstoneClone.UI
{
    public class NativeMulliganCard : MonoBehaviour
    {
        public GameObject keep,replace,cross;
        public void SetSelected(bool selected)
        {keep.SetActive(!selected);replace.SetActive(selected);cross.SetActive(selected);}
    }
}
