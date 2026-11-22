using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using HearthstoneClone.Cards;
namespace HearthstoneClone.UI
{
 // Original phase geometry is captured in the source viewport, as its RenderToTexture setup did.
 // The source shader's AlphaClipBloom compositor is approximated by the existing light composite.
 public class NativeSpellPresentation:MonoBehaviour
 {
  public NativeSpellLibrary library;public RectTransform overlay;public Camera boardCamera;public Material composite;
  readonly List<Phase> phases=new();int serial;
  public int PlayedCount{get;private set;}public int ActiveCount=>phases.Count;
  public string LastCard{get;private set;}
  sealed class Phase
  {
   public NativeSpellLibrary.Phase data;public GameObject root,actor;public Camera camera;public RawImage image;public RenderTexture texture;public AudioSource voice;
   public float age,pixelsPerUnit;public bool impact,stopped,hidden;public Transform flash;public Renderer flashRenderer;public MaterialPropertyBlock props=new();
   public Transform[] rotationTargets;
  }
  public bool TryCreate(CardData card,Vector3 source,Func<Vector3> target,Action impact,out IEnumerator routine)
  {
   var entry=library==null?null:Array.Find(library.entries,e=>e.cardName==card.cardName);
   if(entry==null){routine=null;return false;}routine=Play(entry,source,target,impact);return true;
  }
  IEnumerator Play(NativeSpellLibrary.Entry entry,Vector3 source,Func<Vector3> target,Action onImpact)
  {
   PlayedCount++;LastCard=entry.cardName;var missile=Spawn(entry.missile,false,source);Vector3 endpoint=target();float elapsed=0;
   while(elapsed<entry.pathDuration)
   {
    elapsed+=Time.deltaTime;endpoint=target();float u=Mathf.Clamp01(elapsed/entry.pathDuration);u*=u; // verified source iTween easeInQuad=0
    var delta=endpoint-source;missile.image.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-90);
    Place(missile,Vector3.LerpUnclamped(source,endpoint,u));
    yield return null;
   }
   StopEmitters(missile);var hit=Spawn(entry.impact,true,endpoint);onImpact?.Invoke();
   yield return new WaitForSeconds(entry.cardName=="Fireball"?.5f:.4f); // Original impact SpellFinishAction delay.
  }
  Phase Spawn(NativeSpellLibrary.Phase data,bool impact,Vector3 position)
  {
   var p=new Phase{data=data,impact=impact};
   p.root=new GameObject("Original "+data.name+" capture");p.root.transform.position=new Vector3(18000+(serial++%30)*100,18000,18000);
   p.actor=Instantiate(data.prefab,p.root.transform,false);p.actor.transform.localPosition=Vector3.zero;
   foreach(var t in p.actor.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
   foreach(var animation in p.actor.GetComponentsInChildren<Animation>(true))animation.enabled=false;
   foreach(var animator in p.actor.GetComponentsInChildren<Animator>(true))animator.enabled=false;
   foreach(var r in p.actor.GetComponentsInChildren<Renderer>(true))r.enabled=true;
   p.rotationTargets=(data.rotators??Array.Empty<NativeSpellLibrary.Rotation>()).Select(r=>p.actor.transform.Find(r.path)).ToArray();
   foreach(var t in p.actor.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("Renderer_"))t.gameObject.SetActive(false);
   p.flash=p.actor.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="FlashPlane");
   if(p.flash!=null){p.flash.localScale=Vector3.one*.5f;p.flashRenderer=p.flash.GetComponent<Renderer>();}
   var go=new GameObject("Original spell source viewport camera",typeof(Camera),typeof(UniversalAdditionalCameraData));go.transform.SetParent(p.root.transform,false);go.transform.localPosition=p.actor.transform.TransformPoint(data.cameraPosition)-p.root.transform.position;go.transform.localRotation=p.actor.transform.localRotation*data.cameraRotation;
   p.camera=go.GetComponent<Camera>();p.camera.orthographic=true;p.camera.orthographicSize=data.height*.5f;p.camera.aspect=data.width/data.height;p.camera.nearClipPlane=data.cameraNear;p.camera.farClipPlane=data.cameraFar;
   p.camera.clearFlags=CameraClearFlags.SolidColor;p.camera.backgroundColor=Color.clear;p.camera.cullingMask=1<<31;p.camera.allowHDR=true;p.camera.GetUniversalAdditionalCameraData().SetRenderer(1);
   int width=data.width>data.height?data.resolution:Mathf.Max(1,(int)(data.resolution*data.width/data.height));
   int height=data.width>data.height?Mathf.Max(1,(int)(data.resolution*data.height/data.width)):data.resolution;
   p.texture=new RenderTexture(width,height,16,RenderTextureFormat.ARGBHalf){name="Original "+data.name+" viewport",hideFlags=HideFlags.HideAndDontSave};p.texture.Create();p.camera.targetTexture=p.texture;
   var image=new GameObject("Original "+data.name+" visual",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));image.transform.SetParent(overlay,false);p.image=image.GetComponent<RawImage>();p.image.texture=p.texture;p.image.material=composite;p.image.raycastTarget=false;
   p.image.rectTransform.position=position;
   var world=boardCamera.ScreenToWorldPoint(new Vector3(position.x,position.y,18));float pixels=Vector3.Distance(boardCamera.WorldToScreenPoint(world),boardCamera.WorldToScreenPoint(world+Vector3.right));
   p.pixelsPerUnit=pixels;p.image.rectTransform.sizeDelta=new Vector2(data.width,data.height)*(pixels/p.image.rectTransform.lossyScale.x);Place(p,position);
   var sound=new GameObject("Original "+data.name+" sound",typeof(AudioSource));sound.transform.SetParent(transform,false);sound.transform.position=world;p.voice=sound.GetComponent<AudioSource>();p.voice.playOnAwake=false;p.voice.spatialBlend=1;p.voice.minDistance=Vector3.Distance(boardCamera.transform.position,world)+.01f;p.voice.pitch=1;p.voice.PlayOneShot(data.sound,1);
   foreach(var ps in p.actor.GetComponentsInChildren<ParticleSystem>(true))ps.Play(false);
   phases.Add(p);return p;
  }
  static void Place(Phase phase,Vector3 position)
  {
   // The original output plane is offset from its effect origin. Keeping this
   // offset aligns the captured hot core with the live target at impact.
   var offset=new Vector3(phase.data.planeOffset.x,phase.data.planeOffset.y,0)*phase.pixelsPerUnit;
   phase.image.rectTransform.position=position+phase.image.rectTransform.localRotation*offset;
  }
  void StopEmitters(Phase p)
  {
   if(p.stopped)return;p.stopped=true;
   foreach(var ps in p.actor.GetComponentsInChildren<ParticleSystem>(true))ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);
  }
  void Update()
  {
   foreach(var p in phases.ToArray())
   {
    p.age+=Time.deltaTime;
    for(int i=0;i<p.rotationTargets.Length;i++)if(p.rotationTargets[i]!=null)
    {
     var v=p.data.rotators[i].speed; // Original RotateOverTime Self axes are left, up, forward, in that order.
     p.rotationTargets[i].Rotate(Vector3.left,v.x*Time.deltaTime,Space.Self);p.rotationTargets[i].Rotate(Vector3.up,v.y*Time.deltaTime,Space.Self);p.rotationTargets[i].Rotate(Vector3.forward,v.z*Time.deltaTime,Space.Self);
    }
    if(p.impact)
    {
     if(p.flash!=null)
     {
      float flashTime=Mathf.Max(0,p.age-p.data.flashRiseDuration);
      float scale;
      if(p.data.flashRiseDuration>0&&p.age<p.data.flashRiseDuration)
       scale=Mathf.LerpUnclamped(.5f,p.data.flashRiseScale,p.age/p.data.flashRiseDuration);
      else{float u=Mathf.Clamp01(flashTime/.75f);u=1-(1-u)*(1-u);scale=Mathf.LerpUnclamped(p.data.flashRiseScale,7,u);}
      p.flash.localScale=Vector3.one*scale;
      float alpha=p.data.flashAlpha.Evaluate(flashTime);p.props.SetFloat("_SourceAlpha",alpha);p.flashRenderer.SetPropertyBlock(p.props);
     }
     foreach(var light in p.actor.GetComponentsInChildren<Light>(true))light.intensity=p.age<p.data.flashRiseDuration?1:p.data.lightIntensity.Evaluate(p.age-p.data.flashRiseDuration);
     if(p.age>=1+p.data.flashRiseDuration){StopEmitters(p);if(p.flashRenderer!=null)p.flashRenderer.enabled=false;}
     if(p.age>=5+p.data.flashRiseDuration)Remove(p);
    }
    else if(p.stopped)
    {
     // Source death: .05s then hide core meshes; retain .8s particle tail and final 1s destruction wait.
     if(!p.hidden){p.hidden=true;StartCoroutine(HideMissile(p));}
    }
   }
  }
  IEnumerator HideMissile(Phase p)
  {
   yield return new WaitForSeconds(.05f);if(p.actor==null)yield break;
   foreach(var r in p.actor.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
   foreach(var light in p.actor.GetComponentsInChildren<Light>(true))light.enabled=false;
   yield return new WaitForSeconds(1.8f);if(p.root!=null)Remove(p);
  }
  void Remove(Phase p)
  {
   phases.Remove(p);if(p.camera!=null){p.camera.enabled=false;p.camera.targetTexture=null;}
   if(p.image!=null)Destroy(p.image.gameObject);if(p.voice!=null){p.voice.Stop();Destroy(p.voice.gameObject);}
   if(p.root!=null){p.root.SetActive(false);Destroy(p.root);}if(p.texture!=null){p.texture.Release();Destroy(p.texture);}
  }
  public void ResetEffects(){StopAllCoroutines();foreach(var p in phases.ToArray())Remove(p);PlayedCount=0;LastCard=null;}
  void OnDisable()=>ResetEffects();
 }
}
