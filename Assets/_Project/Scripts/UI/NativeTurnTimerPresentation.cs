using System;
using System.Linq;
using UnityEngine;
namespace HearthstoneClone.UI
{
 // Original actor/rig/particles in board space. Source motions evaluated safely,
 // never AnimationClip.SampleAnimation on exported mixed actor bindings.
 public sealed class NativeTurnTimerPresentation:MonoBehaviour
 {
  public EffectTester match;
  public NativeTurnTimerLibrary library;
  public Transform boardRoot;
  GameObject actor;Transform sparks,startBone,finishBone;
  Renderer[] rope;ParticleSystem[] burnParticles;ParticleSystem afterSmoke;
  AudioSource loop,sounds,music;Material[] maskMaterials;
  float age,fadeDuration,burnDuration;int turn=-1;bool showing,burning;
  public bool RopeVisible=>showing;
  public float BurnProgress{get;private set;}
  public int TimeoutBursts{get;private set;}
  public GameObject Actor=>actor;
  void OnEnable(){if(match!=null)match.TurnTimerElapsed+=Expire;}
  void OnDisable(){if(match!=null)match.TurnTimerElapsed-=Expire;Clear();}
  void EnsureActor()
  {
   if(actor!=null)return;
   actor=Instantiate(library.prefab,boardRoot,false);actor.name="Original turn rope";
   actor.transform.localPosition=Vector3.zero;actor.transform.localRotation=Quaternion.identity;actor.transform.localScale=Vector3.one;
   foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
   foreach(var a in actor.GetComponentsInChildren<Animation>(true))a.enabled=false;
   sparks=actor.transform.Find(library.sparksPath);startBone=actor.transform.Find(library.startBonePath);finishBone=actor.transform.Find(library.finishBonePath);
   rope=actor.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)).ToArray();
   // Renderer.materials makes per-match burn parameters; shared original textures stay shared.
   maskMaterials=rope.SelectMany(r=>r.materials).Where(m=>m.HasProperty("_Xamount")).ToArray();
   afterSmoke=actor.transform.Find("AfterSmoke").GetComponent<ParticleSystem>();
   burnParticles=sparks.GetComponentsInChildren<ParticleSystem>(true);
   foreach(var p in actor.GetComponentsInChildren<ParticleSystem>(true))p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
   loop=actor.AddComponent<AudioSource>();loop.playOnAwake=false;loop.loop=true;loop.clip=library.burn;loop.spatialBlend=0;loop.volume=0;
   sounds=actor.AddComponent<AudioSource>();sounds.playOnAwake=false;sounds.spatialBlend=0;sounds.pitch=1;
   music=actor.AddComponent<AudioSource>();music.playOnAwake=false;music.spatialBlend=0;music.clip=library.stinger;
   SetRope(false);
  }
  void LateUpdate()
  {
   if(match==null||library==null||!match.TurnTimerActive){Clear();return;}
   EnsureActor();
   float dt=Time.unscaledDeltaTime;
   if(turn!=match.TimedTurnNumber)
   {
    turn=match.TimedTurnNumber;if(showing)CancelRope(.4f);
   }
   if(match.TurnTimeRemaining>0&&match.TurnTimeRemaining<=library.ropeSeconds&&!showing)
   {
    showing=true;burning=false;age=0;SetRope(true);
    Apply(library.rollout,0);Apply(library.spiral,0);
    Sound(library.opens,1);Sound(library.unravels,1);music.Play();
   }
   if(showing)
   {
    age+=dt;
    Apply(library.rollout,age*2);Apply(library.spiral,age);
    var spiral=actor.transform.Find("TurnTimer_Spiral").GetComponent<Renderer>();
    spiral.enabled=library.spiralVisibility==null||library.spiralVisibility.Evaluate(age)>.5f;
    foreach(var material in maskMaterials)if(material.name.Contains("Spiral"))material.SetFloat("_Xamount",library.spiralMask.Evaluate(age));
    float offset;
    if(age<library.rolloutWait)offset=library.rolloutMask.Evaluate(age*2);
    else
    {
     if(!burning){burning=true;burnDuration=Mathf.Max(.001f,match.TurnTimeRemaining);foreach(var p in burnParticles)p.Play(false);loop.Play();}
     float fraction=Mathf.Clamp01(match.TurnTimeRemaining/burnDuration);
     sparks.localPosition=Vector3.Lerp(finishBone.localPosition,startBone.localPosition,fraction);
     offset=Mathf.Lerp(-1.5f,.42f,fraction);BurnProgress=1-fraction;
     loop.volume=.4f*Mathf.Clamp01((age-library.rolloutWait)/.3f);
    }
    foreach(var material in maskMaterials)if(!material.name.Contains("Spiral"))material.SetFloat("_Xamount",offset);
   }
   else if(loop.isPlaying)
   {
    loop.volume=Mathf.MoveTowards(loop.volume,0,dt*.4f/Mathf.Max(.001f,fadeDuration));if(loop.volume<=0)loop.Stop();
   }
  }
  void Expire()
  {
   if(!isActiveAndEnabled||library==null)return;
   EnsureActor();TimeoutBursts++;CancelRope(.1f);
   if(!match.IsOpponentTurn)match.endTurnButton.GetComponent<NativeTurnButtonPresentation>()?.TimerElapsed(library);
   Sound(library.explodes,1);Sound(library.flips,1);
   afterSmoke.Play(false);
  }
  void CancelRope(float fade)
  {
   showing=false;burning=false;BurnProgress=0;fadeDuration=fade;
   if(actor==null)return;
   SetRope(false);music.Stop();
   foreach(var p in burnParticles)p.Stop(false,ParticleSystemStopBehavior.StopEmitting);
  }
  void SetRope(bool visible){foreach(var r in rope)r.enabled=visible;}
  void Sound(AudioClip clip,float volume){if(clip!=null)sounds.PlayOneShot(clip,volume);}
  void Clear()
  {
   showing=false;burning=false;turn=-1;BurnProgress=0;
   if(actor!=null){foreach(var m in maskMaterials)Destroy(m);Destroy(actor);actor=null;}
  }
  void Apply(NativeTurnTimerLibrary.Track[] tracks,float time)=>NativeTurnTimerLibrary.Apply(tracks,actor.transform,time);
 }
}
