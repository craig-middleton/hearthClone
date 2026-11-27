using System;
using UnityEngine;
namespace HearthstoneClone.UI
{
 public sealed class NativeTurnTimerLibrary:ScriptableObject
 {
  [Serializable]public sealed class Track
  {
   public string path;
   public AnimationCurve px,py,pz,rx,ry,rz,rw,sx,sy,sz;
  }
  public GameObject prefab;
  public Track[] rollout,spiral,timeoutButton;
  public AnimationCurve rolloutMask,spiralMask,spiralVisibility,buttonAtlasX,buttonAtlasY,buttonHighlight;
  public float buttonTimeoutDuration=1;
  public float ropeSeconds=20,rolloutWait=.87f;
  public string sparksPath,startBonePath,finishBonePath,wickPath,shadowPath;
  public AudioClip stinger,burn,opens,unravels,explodes,flips;
  static float Value(AnimationCurve curve,float age,float fallback)=>curve!=null&&curve.length>0?curve.Evaluate(age):fallback;
  public static void Apply(Track[] tracks,Transform root,float time)
  {
   foreach(var track in tracks)
   {
    var target=string.IsNullOrEmpty(track.path)?root:root.Find(track.path);if(target==null)continue;
    var p=target.localPosition;var q=target.localRotation;var s=target.localScale;
    target.localPosition=new Vector3(Value(track.px,time,p.x),Value(track.py,time,p.y),Value(track.pz,time,p.z));
    target.localRotation=new Quaternion(Value(track.rx,time,q.x),Value(track.ry,time,q.y),Value(track.rz,time,q.z),Value(track.rw,time,q.w)).normalized;
    target.localScale=new Vector3(Value(track.sx,time,s.x),Value(track.sy,time,s.y),Value(track.sz,time,s.z));
   }
  }
 }
}
