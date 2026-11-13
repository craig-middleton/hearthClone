using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace HearthstoneClone.UI
{
    // A curved strip samples the native red targeting texture; it never receives input.
    [RequireComponent(typeof(CanvasRenderer))]
    public class TargetingRibbon : MaskableGraphic
    {
        public Sprite ribbon;
        public Image cursor;
        public Transform source;
        public GameObject hint;
        public Action CancelRequested;
        public override Texture mainTexture => ribbon==null?base.mainTexture:ribbon.texture;
        public bool Visible => enabled;
        [NonSerialized] public Vector2? PreviewPointer;
        InputAction cancelAction;
        Vector2 start,end;
        protected override void OnEnable()
        {
            base.OnEnable();if(!Application.isPlaying)return;
            if(cancelAction==null)
            {
                cancelAction=new InputAction("Cancel Hero Power",InputActionType.Button);
                cancelAction.AddBinding("<Mouse>/rightButton");cancelAction.AddBinding("<Keyboard>/escape");
                cancelAction.performed+=_=>CancelRequested?.Invoke();
            }
            cancelAction.Enable();
        }
        protected override void OnDisable(){if(cancelAction!=null)cancelAction.Disable();base.OnDisable();}
        protected override void OnDestroy(){if(cancelAction!=null)cancelAction.Dispose();base.OnDestroy();}
        public void Show(Transform origin){source=origin;enabled=true;if(cursor!=null)cursor.enabled=true;if(hint!=null)hint.SetActive(true);SetVerticesDirty();}
        public void Hide(){enabled=false;source=null;PreviewPointer=null;if(cursor!=null)cursor.enabled=false;if(hint!=null)hint.SetActive(false);}
        void Update()
        {
            if(!enabled||source==null)return;
            Vector2 pointer=PreviewPointer??(Mouse.current!=null?Mouse.current.position.ReadValue():(Vector2)source.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,source.position,null,out start);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,pointer,null,out end);
            if(cursor!=null)cursor.rectTransform.position=pointer;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(source==null||ribbon==null)return;
            Vector2 delta=end-start;if(delta.sqrMagnitude<400)return;
            Vector2 normal=new Vector2(-delta.y,delta.x).normalized;
            Vector2 control=(start+end)*.5f+normal*Mathf.Min(80,delta.magnitude*.15f);
            Vector4 uv=DataUtility.GetOuterUV(ribbon);
            const int steps=32;
            for(int i=0;i<=steps;i++)
            {
                float t=i/(float)steps;Vector2 point=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;
                Vector2 tangent=(2*(1-t)*(control-start)+2*t*(end-control)).normalized;
                Vector2 side=new Vector2(-tangent.y,tangent.x)*(6+4*t);
                vh.AddVert(point-side,color,new Vector2(Mathf.Lerp(uv.x,uv.z,t),uv.y));
                vh.AddVert(point+side,color,new Vector2(Mathf.Lerp(uv.x,uv.z,t),uv.w));
                if(i>0){int n=i*2;vh.AddTriangle(n-2,n-1,n);vh.AddTriangle(n-1,n+1,n);}
            }
            Vector2 direction=(end-control).normalized;Vector2 wing=new Vector2(-direction.y,direction.x)*22;
            int head=vh.currentVertCount;vh.AddVert(end+direction*18,color,new Vector2(uv.x,uv.y));
            vh.AddVert(end-direction*20+wing,color,new Vector2(uv.z,uv.y));vh.AddVert(end-direction*20-wing,color,new Vector2(uv.z,uv.w));vh.AddTriangle(head,head+1,head+2);
        }
    }
}
