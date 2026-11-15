using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    public class NativeTargetingPresentation : MonoBehaviour
    {
        public RectTransform head, ring;
        public RectTransform[] dashes;
        public void Clear(){head.gameObject.SetActive(false);ring.gameObject.SetActive(false);foreach(var d in dashes)d.gameObject.SetActive(false);}
        public void Draw(Vector2 start, Vector2 end)
        {
            var delta=end-start;float length=delta.magnitude;if(length<35){Clear();return;}
            var control=(start+end)*.5f+new Vector2(-delta.y,delta.x).normalized*Mathf.Min(65,length*.12f);
            int count=Mathf.Clamp(Mathf.FloorToInt((length-35)/27),1,dashes.Length);
            for(int i=0;i<dashes.Length;i++)
            {
                dashes[i].gameObject.SetActive(i<count);if(i>=count)continue;
                float t=(i+.5f)/(count+1);
                var p=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;
                var v=2*(1-t)*(control-start)+2*t*(end-control);
                Place(dashes[i],p,Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg-90, new Vector3(26,21,1));
            }
            var direction=(end-control).normalized;
            head.gameObject.SetActive(true);ring.gameObject.SetActive(true);
            Place(head,end-direction*13,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg+90,Vector3.one*34);
            Place(ring,end,0,Vector3.one*43);
        }
        static void Place(RectTransform rect,Vector2 point,float angle,Vector3 scale)
        {rect.anchoredPosition=point;rect.localRotation=Quaternion.Euler(0,0,angle);rect.localScale=scale;}
    }
}
