using UnityEngine;
namespace HearthstoneClone.UI
{
    public class NativeDeckPile : MonoBehaviour
    {
        // Original source states: one card, quarter, half, three quarters, full.
        public GameObject[] states;
        public void SetRemaining(int count)
        {
            int chosen=count<=0?-1:count==1?0:count/26f>.75f?4:count/26f>.5f?3:count/26f>.25f?2:1;
            for(int i=0;i<states.Length;i++)if(states[i]!=null)states[i].SetActive(i==chosen);
        }
    }
}
