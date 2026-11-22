using System.Collections.Generic;
using System.Linq;
using HearthstoneClone.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace HearthstoneClone.UI
{
    // Presentation only. The existing GameManager remains the authority for lethal/draw.
    public class NativeHeroDeathPresentation:MonoBehaviour
    {
        public EffectTester match;
        public Camera boardCamera;
        public Transform boardRoot;
        public NativeHeroDeathLibrary library;
        public CanvasGroup[] playerLabels,opponentLabels;
        public bool IsBusy=>slots.Any(s=>s.pending||s.running);
        public int ExplosionsStarted{get;private set;}
        public int CompletedCount{get;private set;}
        public GameObject[] ActiveEffects=>slots.Where(s=>s.stage!=null).Select(s=>s.stage).ToArray();
        sealed class Slot
        {
            public FaceView face;public Button power;public Player owner;
            public bool pending,running,complete,exploded,powerWasActive;
            public float clock;
            public GameObject stage;
            public Transform actor;
            public Renderer cracks,crater;public Renderer[] debris;
            public ParticleSystem[] particles;public float[] particleSpeeds;
            public AudioSource sound;
            public Light light;
            public CanvasGroup group;public float originalAlpha;
            public CanvasGroup[] labels;public float[] labelAlphas;
            public Material[] materials;public NativeHeroCrackPart crackGraphic;public Transform crackTransform;
        }
        readonly List<Slot> slots=new();
        bool resultReleased;
        void OnEnable()
        {
            if(match==null)return;
            ResetEffects();
            slots.Add(new Slot{face=match.faceView,power=match.heroPowerButton,labels=playerLabels});
            slots.Add(new Slot{face=match.opponentFaceView,power=match.opponentHeroPowerButton,labels=opponentLabels});
        }
        void ResetSlot(Slot s)
        {
            if(s.group!=null)s.group.alpha=s.originalAlpha;
            if(s.labels!=null&&s.labelAlphas!=null)for(int i=0;i<s.labels.Length;i++)if(s.labels[i]!=null)s.labels[i].alpha=s.labelAlphas[i];
            s.labelAlphas=null;
            if(s.power!=null&&s.exploded)s.power.gameObject.SetActive(s.powerWasActive);
            if(s.stage!=null){s.stage.SetActive(false);Destroy(s.stage);}
            if(s.crackGraphic!=null)Destroy(s.crackGraphic.gameObject);
            if(s.materials!=null)foreach(var m in s.materials)if(m!=null)Destroy(m);
            s.stage=null;s.materials=null;s.pending=s.running=s.complete=s.exploded=false;s.clock=0;
        }
        public void ResetEffects()
        {
            foreach(var s in slots)ResetSlot(s);slots.Clear();
            resultReleased=false;ExplosionsStarted=CompletedCount=0;
        }
        void OnDisable()=>ResetEffects();
        void LateUpdate()
        {
            if(library==null||match==null)return;
            if(slots.Any(s=>s.face.Player!=s.owner))
            {
                foreach(var s in slots){ResetSlot(s);s.owner=s.face.Player;}
                resultReleased=false;ExplosionsStarted=CompletedCount=0;
            }
            foreach(var s in slots)
            {
                if(s.owner==null)continue;
                if(s.owner.Health<=0&&!s.running&&!s.complete)s.pending=true;
                if(s.pending&&!match.ActionEffectsBusy){s.pending=false;Begin(s);continue;}
                if(s.running)Tick(s);
            }
            if(IsBusy)
            {
                match.gameOverText.gameObject.SetActive(false);match.playAgainButton.gameObject.SetActive(false);
            }
            else if(!resultReleased&&slots.Any(s=>s.complete))
            {
                match.gameOverText.gameObject.SetActive(true);match.playAgainButton.gameObject.SetActive(true);resultReleased=true;
            }
        }
        void Begin(Slot s)
        {
            s.running=true;s.clock=0;
            s.stage=new GameObject("Original hero death "+s.owner.PlayerName);s.stage.transform.SetParent(boardRoot,false);
            var actor=Instantiate(library.sourcePrefab,s.stage.transform);s.actor=actor.transform;s.actor.localPosition=Vector3.zero;
            foreach(var a in actor.GetComponentsInChildren<Animation>(true)){a.Stop();a.enabled=false;}
            foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
            s.particles=actor.GetComponentsInChildren<ParticleSystem>(true);s.particleSpeeds=s.particles.Select(p=>p.main.simulationSpeed).ToArray();
            foreach(var p in s.particles)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var mats=new List<Material>();
            foreach(var renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                var copies=renderer.sharedMaterials.Select(m=>new Material(m)).ToArray();renderer.sharedMaterials=copies;mats.AddRange(copies);
                foreach(var m in copies)if(m.name.StartsWith("Hero_portrait_template"))
                {
                    var sprite=s.face.avatarImage.sprite;var texture=sprite.texture;m.SetTexture("_MainTex",texture);
                    m.SetTextureScale("_MainTex",new Vector2(sprite.rect.width/texture.width,sprite.rect.height/texture.height));
                    m.SetTextureOffset("_MainTex",new Vector2(sprite.rect.x/texture.width,sprite.rect.y/texture.height));
                }
            }
            s.materials=mats.ToArray();
            s.cracks=s.actor.Find("FX_CrackQuad").GetComponent<Renderer>();s.crater=s.actor.Find("EndGame_CraterPlayer").GetComponent<Renderer>();
            s.debris=s.actor.Find("HeroBombDebris").GetComponentsInChildren<Renderer>(true);
            s.cracks.enabled=true;s.crater.enabled=false;foreach(var r in s.debris)r.enabled=false;
            s.light=s.actor.GetComponentInChildren<Light>(true);if(s.light!=null)s.light.intensity=0;
            s.sound=actor.AddComponent<AudioSource>();s.sound.playOnAwake=false;s.sound.spatialBlend=1;s.sound.pitch=1;
            Position(s);s.sound.minDistance=Vector3.Distance(boardCamera.transform.position,s.sound.transform.position)+.01f;s.sound.PlayOneShot(library.crackSound,.8f);
            Apply(s.actor,library.crack,0);
            s.crackTransform=s.actor.Find("FX_CrackQuad");
            var crackUi=new GameObject("Original hero crack geometry",typeof(RectTransform),typeof(CanvasRenderer),typeof(NativeHeroCrackPart));
            crackUi.transform.SetParent(s.face.avatarImage.transform,false);var rect=(RectTransform)crackUi.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;rect.anchoredPosition=Vector2.zero;
            s.crackGraphic=crackUi.GetComponent<NativeHeroCrackPart>();s.crackGraphic.paint=library.crackPaint;s.crackGraphic.material=library.crackUiMaterial;s.crackGraphic.raycastTarget=false;s.crackGraphic.sourceColors=library.crackColors;s.crackGraphic.uv=library.crackUvs;s.crackGraphic.triangles=library.crackTriangles;
            UpdateCrack(s);
        }
        void Position(Slot s)
        {
            var rect=s.face.avatarImage.rectTransform;Vector3[] corners=new Vector3[4];rect.GetWorldCorners(corners);
            var screen=rect.TransformPoint(rect.rect.center);var ray=boardCamera.ScreenPointToRay(screen);
            var plane=new Plane(Vector3.up,new Vector3(0,2.48f,0));if(!plane.Raycast(ray,out float distance))return;
            var center=ray.GetPoint(distance);float pixels=Vector3.Distance(corners[0],corners[3]);
            float sourcePixels=Vector3.Distance(boardCamera.WorldToScreenPoint(center-Vector3.right*library.debrisWidth*.5f),boardCamera.WorldToScreenPoint(center+Vector3.right*library.debrisWidth*.5f));
            s.stage.transform.localScale=Vector3.one*(pixels/sourcePixels);s.stage.transform.position=center-s.stage.transform.TransformVector(library.debrisCenter);
        }
        void Explode(Slot s)
        {
            s.exploded=true;ExplosionsStarted++;s.cracks.enabled=false;s.crater.enabled=true;
            if(s.crackGraphic!=null){Destroy(s.crackGraphic.gameObject);s.crackGraphic=null;}
            foreach(var r in s.debris)r.enabled=true;
            s.group=s.face.GetComponent<CanvasGroup>();if(s.group==null)s.group=s.face.gameObject.AddComponent<CanvasGroup>();s.originalAlpha=s.group.alpha;s.group.alpha=0;
            s.labelAlphas=s.labels.Select(g=>g.alpha).ToArray();foreach(var group in s.labels)group.alpha=0;
            s.powerWasActive=s.power!=null&&s.power.gameObject.activeSelf;if(s.power!=null)s.power.gameObject.SetActive(false);
            foreach(var name in new[]{"FX_FlareBurst1","FX_Rubble1","FX_DustWave1_new","FX_Sparks2_new","FX_SmokeStay"})
            {
                var p=s.actor.Find(name).GetComponent<ParticleSystem>();if(name=="FX_SmokeStay")
                {int index=System.Array.IndexOf(s.particles,p);s.particleSpeeds[index]=.5f;}
                p.Play(name=="FX_FlareBurst1"||name=="FX_Rubble1"||name=="FX_SmokeStay");
            }
            s.sound.PlayOneShot(library.explodeSound,.9f);Apply(s.actor,library.debris,0);Apply(s.actor,library.light,0);
        }
        void Tick(Slot s)
        {
            float scale;
            if(s.clock<library.crackDuration)scale=library.crackClock.Evaluate(s.clock/library.crackDuration*library.crackClock.keys.Last().time);
            else if(s.clock<library.crackDuration+library.explodeSlowDuration)scale=library.explodeClock.Evaluate((s.clock-library.crackDuration)/library.explodeSlowDuration*library.explodeClock.keys.Last().time);
            else scale=1;
            // Original AnimateFloat/Wait use scaled deltaTime; keep Unity's frame cap.
            s.clock+=Time.deltaTime*scale;
            for(int i=0;i<s.particles.Length;i++){var main=s.particles[i].main;main.simulationSpeed=s.particleSpeeds[i]*scale;}
            if(!s.exploded&&s.clock>=library.crackDuration)Explode(s);
            if(!s.exploded){Apply(s.actor,library.crack,s.clock);UpdateCrack(s);}
            else{Apply(s.actor,library.debris,s.clock-library.crackDuration);Apply(s.actor,library.light,s.clock-library.crackDuration);}
            if(s.clock>=library.crackDuration+library.explodeSlowDuration+library.holdDuration)
            {s.running=false;s.complete=true;CompletedCount++;foreach(var p in s.particles)p.Stop(false,ParticleSystemStopBehavior.StopEmitting);}
        }
        void UpdateCrack(Slot s)
        {
            if(s.crackGraphic==null)return;
            var rect=s.crackGraphic.rectTransform;var canvas=s.face.GetComponentInParent<Canvas>().rootCanvas;
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var points=new Vector2[library.crackVertices.Length];
            for(int i=0;i<points.Length;i++)
            {
                var world=s.crackTransform.TransformPoint(library.crackVertices[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,boardCamera.WorldToScreenPoint(world),camera,out points[i]);
            }
            var visibility=library.crack.FirstOrDefault(c=>c.path=="FX_CrackQuad"&&c.property=="m_Enabled");
            s.crackGraphic.points=points;s.crackGraphic.enabled=!s.exploded&&(visibility==null||visibility.curve.Evaluate(s.clock)>=.5f);s.crackGraphic.SetVerticesDirty();s.cracks.enabled=false;
        }
        static void Apply(Transform root,NativeHeroDeathLibrary.Curve[] curves,float time)
        {
            foreach(var group in curves.GroupBy(c=>c.path))
            {
                var target=string.IsNullOrEmpty(group.Key)?root:root.Find(group.Key);if(target==null)continue;
                Vector3 position=target.localPosition,scale=target.localScale;Quaternion rotation=target.localRotation;
                foreach(var c in group)
                {
                    float value=c.curve.Evaluate(time);var p=c.property;int axis=p.EndsWith("x")?0:p.EndsWith("y")?1:2;
                    if(p.StartsWith("m_LocalPosition."))position[axis]=value;
                    else if(p.StartsWith("m_LocalScale."))scale[axis]=value;
                    else if(p.StartsWith("m_LocalRotation."))rotation[p.EndsWith("w")?3:axis]=value;
                    else if(p=="m_Enabled"){var r=target.GetComponent<Renderer>();if(r!=null)r.enabled=value>=.5f;}
                    else if(p=="m_Intensity"){var light=target.GetComponent<Light>();if(light!=null)light.intensity=value;}
                    else {var r=target.GetComponent<Renderer>();if(r!=null)foreach(var m in r.sharedMaterials)if(m.HasProperty(p))m.SetFloat(p,value);}
                }
                target.localPosition=position;target.localScale=scale;target.localRotation=rotation.normalized;
            }
        }
    }
}
