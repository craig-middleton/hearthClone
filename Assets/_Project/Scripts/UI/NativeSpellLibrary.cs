using System;
using UnityEngine;
namespace HearthstoneClone.UI
{
 public class NativeSpellLibrary:ScriptableObject
 {
  public Entry[] entries;
  [Serializable] public class Entry {public string cardId,cardName;public float pathDuration;public int easeType;public Phase missile,impact;}
  [Serializable] public class Phase
  {
   public string name,sourcePrefab,soundPath;public GameObject prefab;public AudioClip sound;
   public float width,height,cameraNear,cameraFar;
   public Vector3 cameraPosition;public Quaternion cameraRotation;
   public Vector2 planeOffset;public float flashRiseDuration,flashRiseScale;
   public int resolution;public Rotation[] rotators;
   public AnimationCurve flashAlpha,lightIntensity;public Key[] flashAlphaKeys,lightKeys;
  }
  [Serializable] public class Key {public float time,value,inSlope,outSlope;}
  [Serializable] public class Rotation {public string path;public Vector3 speed;}
 }
}
