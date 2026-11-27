using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEngine;
public static class VerifyNativeTurnTimer
{
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool test,string message){if(!test)throw new Exception(message);}
 static async Task Until(Func<bool> condition,string message)
 {
  var wall=System.Diagnostics.Stopwatch.StartNew();while(!condition())
  {if(wall.Elapsed.TotalSeconds>15)throw new Exception(message);await Task.Delay(20);}
 }
 static TurnClock Clock(EffectTester e)=>(TurnClock)typeof(EffectTester).GetField("turnClock",Private).GetValue(e);
 static void Seek(EffectTester e,float remaining)=>Clock(e).Advance(Mathf.Max(0,e.TurnTimeRemaining-remaining));
 static bool ButtonBurst(NativeTurnButtonPresentation button)=>(bool)typeof(NativeTurnButtonPresentation).GetField("timeoutAnimating",Private).GetValue(button);
 public static async Task<string> Main()
 {
  var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();var p=e.frontEnd.tableSurface.GetComponent<NativeTurnTimerPresentation>();
  var button=e.endTurnButton.GetComponent<NativeTurnButtonPresentation>();bool manual=e.manualControlMode;
  try
  {
   Check(p!=null&&p.library!=null&&p.library.ropeSeconds==20,"Original timer library absent");
   Check(p.library.rollout.Length==10&&p.library.timeoutButton.Length>0&&Mathf.Abs(p.library.buttonTimeoutDuration-.85f)<.001f,"Source rig/button curves absent");
   e.frontEnd.ShowMenu();await Task.Delay(50);Check(p.Actor==null,"Rope actor leaked into menu");
   e.manualControlMode=true;e.frontEnd.Play();await Task.Delay(50);Check(p.Actor==null,"Rope visible in mulligan");
   e.confirmMulliganButton.onClick.Invoke();await Task.Delay(50);Check(!p.RopeVisible,"Rope visible before final20seconds");
   Seek(e,19.9f);await Until(()=>p.RopeVisible,"Rope failed to start");
   var actor=p.Actor;Check(actor.transform.localPosition==Vector3.zero&&actor.transform.localScale==Vector3.one,"Original actor mapping changed");
   Check(actor.GetComponentsInChildren<MonoBehaviour>(true).Length==0&&actor.GetComponentsInChildren<Collider>(true).Length==0,"Original scripts or blocking colliders imported");
   var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();Check(skin.bones.Length==10&&skin.sharedMesh.uv2.Length==skin.sharedMesh.vertexCount,"Original skin/UV2 lost");
   await Until(()=>actor.GetComponents<AudioSource>().Any(a=>a.loop&&a.isPlaying),"Source fuse loop never started after rollout");
   await Task.Delay(350);
   Check(p.BurnProgress<.15f&&!actor.transform.Find("TurnTimer_Spiral").GetComponent<Renderer>().enabled,"Rollout clock or spiral visibility wrong");
   Check(actor.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.Where(m=>m!=null).All(m=>m.shader.isSupported)),"Unsupported rope surface");
   var start=actor.transform.Find(p.library.startBonePath);var finish=actor.transform.Find(p.library.finishBonePath);var sparks=actor.transform.Find(p.library.sparksPath);
   float earlier=sparks.localPosition.x;Seek(e,e.TurnTimeRemaining*.5f);await Task.Delay(70);
   Check(p.BurnProgress>.45f&&p.BurnProgress<.65f&&sparks.localPosition.x>earlier,"Burn point did not follow real deadline");
   var expected=Vector3.Lerp(start.localPosition,finish.localPosition,p.BurnProgress);
   Check(Vector3.Distance(sparks.localPosition,expected)<.01f,"Sparks left original bone-marker path");
   float expectedMask=Mathf.Lerp(.42f,-1.5f,p.BurnProgress);
   foreach(var r in actor.GetComponentsInChildren<Renderer>().Where(r=>r.name=="joint1"||r.name=="TurnTimer_ShadowQuad"))
    Check(Mathf.Abs(r.sharedMaterial.GetFloat("_Xamount")-expectedMask)<.02f,"Wick/shadow mask detached from spark deadline");
   int bursts=p.TimeoutBursts;e.endTurnButton.onClick.Invoke();await Until(()=>!p.RopeVisible,"Manual End Turn failed to cancel rope");
   await Task.Delay(450);Check(p.TimeoutBursts==bursts&&!actor.GetComponents<AudioSource>().Any(a=>a.loop&&a.isPlaying),"Manual End Turn exploded or retained fuse loop");
   // Opponent timeout advances normally, without playing friendly-only button explosion.
   Seek(e,0);await Until(()=>e.TimedTurnNumber==3,"Opponent timeout did not advance");Check(!ButtonBurst(button),"Opponent timeout played friendly button explosion");
   await Until(()=>!button.IsTransitioning,"Button never returned from opponent timeout");
   // Friendly expiry uses original timeout animation, already ending in waiting pose.
   Seek(e,19.9f);await Until(()=>p.RopeVisible,"Second rope failed to start");await Task.Delay(1000);
   bursts=p.TimeoutBursts;Seek(e,0);await Until(()=>p.TimeoutBursts==bursts+1,"Expiry smoke/audio event missing");
   Check(ButtonBurst(button),"Friendly source timeout clip absent");
   Check(actor.transform.Find("AfterSmoke").GetComponent<ParticleSystem>().IsAlive(true),"Original expiry smoke absent");
   await Until(()=>!button.IsTransitioning,"Friendly timeout button remained stuck");
   Check(e.TimedTurnNumber==4&&p.TimeoutBursts==bursts+1,"Expiry advanced twice");
   var track=p.library.timeoutButton.Single(t=>string.IsNullOrEmpty(t.path));
   var target=button.animator.transform;var q=new Quaternion(track.rx.Evaluate(p.library.buttonTimeoutDuration),track.ry.Evaluate(p.library.buttonTimeoutDuration),track.rz.Evaluate(p.library.buttonTimeoutDuration),track.rw.Evaluate(p.library.buttonTimeoutDuration)).normalized;
   Check(Mathf.Abs(Quaternion.Dot(target.localRotation,q))>.999f&&Mathf.Abs(target.localPosition.y-track.py.Evaluate(p.library.buttonTimeoutDuration))<.01f,"Source timeout waiting pose overwritten by second flip");
   e.endTurnButton.onClick.Invoke();await Until(()=>!button.IsTransitioning,"Next friendly return stuck");
   Check(e.TimedTurnNumber==5&&Quaternion.Angle(target.localRotation,Quaternion.identity)<1,"Button failed to return upright");
   e.frontEnd.ShowMenu();await Task.Delay(70);Check(p.Actor==null,"Menu retained source actor/materials");
   return "Original10-bone skin/UV2 and safe curves, final20-second visibility, rollout/spiral hide, burn-point/mask agreement, source fuse audio, manual cancel, opponent/friendly expiry distinction, smoke, exactly-once burst, source-duration button timeout/no double flip, next-turn upright return and menu cleanup passed.";
  }
  finally{e.manualControlMode=manual;e.frontEnd.ShowMenu();}
 }
}
