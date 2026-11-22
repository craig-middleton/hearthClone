using System.Linq;
using UnityEngine;
using TMPro;
using HearthstoneClone.Core;
namespace HearthstoneClone.UI
{
    public class NativeHeroPowerPresentation : MonoBehaviour
    {
        public RectTransform button;
        public Transform stage,actor,gem;
        public Camera boardCamera;
        [System.Serializable] public class Curve {public string path,property;public AnimationCurve curve;}
        public AnimationClip usedClip,restoreClip;
        public Curve[] usedCurves,restoreCurves;
        public Renderer highlight;
        public TMP_Text cost;
        public Vector3 sourceCenter;
        public float sourceDiameter;
        Player player;bool available,previousUsed,initialized;
        AnimationClip motion;float started;
        public void SetPlayer(Player owner,bool canUse){player=owner;available=canUse;}
        void OnEnable(){initialized=false;motion=null;}
        void OnDisable(){if(stage!=null)stage.gameObject.SetActive(false);}
        void Sample(Curve[] curves,float time)
        {
            if(curves==null)return;
            foreach(var group in curves.GroupBy(b=>b.path))
            {
                var t=string.IsNullOrEmpty(group.Key)?actor:actor.Find(group.Key);if(t==null)continue;
                var position=t.localPosition;var scale=t.localScale;var rotation=t.localRotation;
                foreach(var b in group)
                {
                    float v=b.curve.Evaluate(time);string p=b.property;int axis=p.EndsWith("x")?0:p.EndsWith("y")?1:2;
                    if(p.StartsWith("m_LocalPosition."))position[axis]=v;
                    else if(p.StartsWith("m_LocalScale."))scale[axis]=v;
                    else if(p.StartsWith("m_LocalRotation."))rotation[p.EndsWith("w")?3:axis]=v;
                    else if(p=="m_Enabled"){var r=t.GetComponent<Renderer>();if(r!=null)r.enabled=v>=.5f;}
                }
                t.localPosition=position;t.localScale=scale;t.localRotation=rotation.normalized;
            }
        }

        void LateUpdate()
        {
            if(stage==null||player==null)return;
            stage.gameObject.SetActive(true);
            Vector3[] corners=new Vector3[4];button.GetWorldCorners(corners);
            Vector3 screen=button.TransformPoint(button.rect.center);
            var plane=new Plane(Vector3.up,new Vector3(0,2.48f,0));var ray=boardCamera.ScreenPointToRay(screen);
            if(!plane.Raycast(ray,out float distance))return;
            var center=ray.GetPoint(distance);
            float pixels=Vector3.Distance(corners[0],corners[3]);
            float projected=Vector3.Distance(boardCamera.WorldToScreenPoint(center-Vector3.right*sourceDiameter*.5f),boardCamera.WorldToScreenPoint(center+Vector3.right*sourceDiameter*.5f));
            stage.localScale=Vector3.one*(pixels/projected);stage.position=center-stage.TransformVector(sourceCenter);
            bool used=player.HasUsedHeroPowerThisTurn;
            if(!initialized){Sample(usedCurves,used?usedClip.length:0);previousUsed=used;initialized=true;}
            else if(used!=previousUsed){motion=used?usedClip:restoreClip;started=Time.time;previousUsed=used;}
            if(motion!=null){float time=Mathf.Min(motion.length,Time.time-started);Sample(motion==usedClip?usedCurves:restoreCurves,time);if(time>=motion.length)motion=null;}
            highlight.enabled=available&&!used;
            cost.rectTransform.position=boardCamera.WorldToScreenPoint(gem.position);cost.text="2";
        }
    }
}
