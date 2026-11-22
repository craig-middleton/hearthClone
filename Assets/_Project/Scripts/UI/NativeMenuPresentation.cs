using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    // Original TheBox world hierarchy; the existing frontend retains all actions.
    public sealed class NativeMenuPresentation : MonoBehaviour
    {
        [Serializable] public sealed class Channel
        {
            public string property;
            public AnimationCurve curve;
        }
        [Serializable] public sealed class Slot
        {
            public Renderer source;
            // Original mesh points baked by the Edit-mode installer. The source
            // GPU mesh can remain non-readable in Play and standalone builds.
            public Vector3[] sourcePoints;
            public RectTransform hitbox;
            public TMP_Text label;
        }
        public GameObject menuRoot;
        public Camera menuCamera;
        public Camera gameplayCamera;
        public AnimationClip originalCameraClip;
        public Channel[] cameraChannels;
        public Slot[] slots;
        public RectTransform deckLabel;
        public Transform drawer;
        public Vector3 drawerOpenPosition;
        public float sourceFieldOfView=20.7f;
        public Vector3 restingCameraPosition=new Vector3(0,31.171715f,-.9112549f);
        public Quaternion restingCameraRotation=new Quaternion(.7071068f,0,0,.7071068f);
        bool introPlayed;
        float introTime;
        bool runningIntro;
        public bool IntroPlaying=>runningIntro;
        public bool IntroStarted=>introPlayed;
        public float IntroTime=>introTime;

        void Start()
        {
            // Also cover fast-enter-play configurations where an edit-time
            // enabled object does not receive another enable notification.
            if(!introPlayed)BeginIntro();
        }
        void OnEnable()
        {
            if(menuRoot!=null)menuRoot.SetActive(true);
            if(menuCamera!=null)menuCamera.enabled=true;
            if(Application.isPlaying&&!introPlayed)BeginIntro();
            else RestCamera();
        }
        void BeginIntro()
        {
            if(!Application.isPlaying||originalCameraClip==null)return;
            introPlayed=true;introTime=0;runningIntro=true;SampleCamera(0);
        }
        void OnDisable()
        {
            runningIntro=false;
            if(menuRoot!=null)menuRoot.SetActive(false);
        }
        void RestCamera()
        {
            if(menuCamera==null)return;
            menuCamera.transform.localPosition=restingCameraPosition;
            menuCamera.transform.localRotation=restingCameraRotation;
        }
        void SampleCamera(float time)
        {
            if(menuCamera==null||cameraChannels==null)return;
            var p=menuCamera.transform.localPosition;
            var q=menuCamera.transform.localRotation;
            foreach(var c in cameraChannels)
            {
                int axis=c.property.EndsWith("x",StringComparison.Ordinal)?0:c.property.EndsWith("y",StringComparison.Ordinal)?1:c.property.EndsWith("w",StringComparison.Ordinal)?3:2;
                if(c.property.StartsWith("m_LocalPosition.",StringComparison.Ordinal))p[axis]=c.curve.Evaluate(time);
                else if(c.property.StartsWith("m_LocalRotation.",StringComparison.Ordinal))q[axis]=c.curve.Evaluate(time);
            }
            menuCamera.transform.localPosition=p;menuCamera.transform.localRotation=q.normalized;
        }
        void LateUpdate()
        {
            if(menuRoot==null||menuCamera==null)return;
            menuRoot.SetActive(true);
            if(runningIntro)
            {
                introTime=Mathf.Min(originalCameraClip.length,introTime+Time.deltaTime);
                SampleCamera(introTime);
                if(introTime>=originalCameraClip.length){runningIntro=false;RestCamera();}
            }
            else RestCamera();
            RefreshLayout();
        }
        public void RefreshLayout()
        {
            if(menuRoot==null||menuCamera==null)return;
            menuCamera.ResetAspect();
            float fit=1.08f*Mathf.Max(1,(16f/9f)/menuCamera.aspect);
            menuCamera.fieldOfView=2*Mathf.Atan(Mathf.Tan(sourceFieldOfView*Mathf.Deg2Rad*.5f)*fit)*Mathf.Rad2Deg;
            foreach(var slot in slots??Array.Empty<Slot>())Place(slot);
            if(deckLabel!=null)
            {
                // Status belongs to this project's deck chooser; anchor to the
                // original bottom drawer, rather than a separate side panel.
                var world=drawer!=null?drawer.position:menuRoot.transform.position;
                var screen=menuCamera.WorldToScreenPoint(world);
                deckLabel.position=new Vector3(screen.x,screen.y-32,deckLabel.position.z);
            }
        }
        void Place(Slot slot)
        {
            if(slot.source==null||slot.hitbox==null)return;
            var points=slot.sourcePoints;
            if(points==null||points.Length==0)return;
            var lo=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
            var hi=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach(var point in points)
            {
                var screen=menuCamera.WorldToScreenPoint(slot.source.transform.TransformPoint(point));
                lo.x=Mathf.Min(lo.x,screen.x);lo.y=Mathf.Min(lo.y,screen.y);
                hi.x=Mathf.Max(hi.x,screen.x);hi.y=Mathf.Max(hi.y,screen.y);
            }
            // Flat source buttons are separate models; retain their exact screen
            // bounds while input stays in the established UI Button system.
            slot.hitbox.position=(lo+hi)*.5f;
            var canvas=slot.hitbox.GetComponentInParent<Canvas>()?.rootCanvas;
            float scale=canvas!=null?canvas.scaleFactor:1;
            slot.hitbox.sizeDelta=(hi-lo)/Mathf.Max(.001f,scale);
            var button=slot.hitbox.GetComponent<Button>();
            if(button!=null)button.interactable=!runningIntro;
            if(slot.label!=null)
            {
                slot.label.rectTransform.anchorMin=Vector2.zero;
                slot.label.rectTransform.anchorMax=Vector2.one;
                slot.label.rectTransform.offsetMin=Vector2.zero;
                slot.label.rectTransform.offsetMax=Vector2.zero;
                // Match the source button's projected baseline during the original
                // camera movement, rather than leaving overlay text horizontal.
                var origin=menuCamera.WorldToScreenPoint(slot.source.transform.position);
                var right=menuCamera.WorldToScreenPoint(slot.source.transform.TransformPoint(Vector3.right));
                var direction=right-origin;
                float angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
                if(angle>90)angle-=180;if(angle< -90)angle+=180;
                slot.label.rectTransform.localRotation=Quaternion.Euler(0,0,angle);
                slot.label.rectTransform.sizeDelta=Vector2.zero;
            }
        }
    }
}
