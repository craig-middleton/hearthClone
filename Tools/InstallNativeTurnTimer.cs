using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HearthstoneClone.UI;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
public static class InstallNativeTurnTimer
{
 const string Folder="Assets/_Project/Art/NativeTurnTimer/";
 static NativeTurnTimerLibrary.Track[] Bake(string name,string prefix)
  =>BakeClip(AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Source/"+name+".anim"),prefix);
 static NativeTurnTimerLibrary.Track[] BakeClip(AnimationClip clip,string prefix)
 {
  if(clip==null)throw new Exception("Missing original rope clip");
  return AnimationUtility.GetCurveBindings(clip).Where(b=>b.type==typeof(Transform)).GroupBy(b=>b.path).Select(group=>
  {
   var track=new NativeTurnTimerLibrary.Track{path=prefix+(string.IsNullOrEmpty(group.Key)?"":"/"+group.Key)};
   foreach(var b in group)
   {
    string field=b.propertyName.StartsWith("m_LocalPosition.")?"p":b.propertyName.StartsWith("m_LocalRotation.")?"r":b.propertyName.StartsWith("m_LocalScale.")?"s":null;
    if(field==null)throw new Exception("Unmapped source rope transform "+b.propertyName);
    typeof(NativeTurnTimerLibrary.Track).GetField(field+b.propertyName.Last()).SetValue(track,AnimationUtility.GetEditorCurve(clip,b));
   }
   return track;
  }).ToArray();
 }
 static AnimationCurve RendererCurve(string property)
 {
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Source/TurnTimer_SpiralUnwrap.anim");
  var binding=AnimationUtility.GetCurveBindings(clip).Single(b=>b.propertyName==property);
  return AnimationUtility.GetEditorCurve(clip,binding);
 }
 public static string Main()
 {
  if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit required");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var config=JObject.Parse(File.ReadAllText(Folder+"SourceConfiguration.json"));
  var template=AssetDatabase.LoadAssetAtPath<GameObject>((string)config["sourcePrefab"]);
  var actor=UnityEngine.Object.Instantiate(template);actor.name="Original TurnTimer";
  try
  {
   foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
   foreach(var a in actor.GetComponentsInChildren<Animation>(true)){a.enabled=false;a.playAutomatically=false;}
   foreach(var a in actor.GetComponentsInChildren<AudioSource>(true)){a.Stop();a.playOnAwake=false;a.enabled=false;}
   foreach(var ps in actor.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;main.playOnAwake=false;ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
   foreach(var renderer in actor.GetComponentsInChildren<Renderer>(true))
   {
    renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
    renderer.sharedMaterials=renderer.sharedMaterials.Select(original=>
    {
     if(original==null)return null;
     bool rope=original.name.StartsWith("TurnTimerWick");bool shadow=original.name=="TurnTimerWickShadow";
     var record=config["materials"].Single(m=>(string)m["asset"]==AssetDatabase.GetAssetPath(original));
     bool additive=((string)record["shaderName"]).Contains("Additive");
     string path=Folder+original.name+"_Compatibility.mat";
     var shader=Shader.Find(rope?"HearthClone/Compatibility/Original Turn Rope":"HearthClone/Compatibility/Original Spell Surface");
     var material=AssetDatabase.LoadAssetAtPath<Material>(path);
     if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}material.shader=shader;
     string texture=rope?"_Diffuse":"_MainTex";
     var paint=SourceMaterialProperties.Texture(original,texture);if(paint==null)throw new Exception("Original rope paint missing "+original.name);
     material.SetTexture(texture,paint);material.SetTextureScale(texture,SourceMaterialProperties.Scale(original,texture));material.SetTextureOffset(texture,SourceMaterialProperties.Offset(original,texture));
     if(rope)
     {
      material.SetTexture("_Mask",SourceMaterialProperties.Texture(original,"_Mask"));material.SetTextureScale("_Mask",SourceMaterialProperties.Scale(original,"_Mask"));material.SetTextureOffset("_Mask",SourceMaterialProperties.Offset(original,"_Mask"));
      material.SetFloat("_Amount",SourceMaterialProperties.Float(original,"_Amount"));material.SetFloat("_Xamount",SourceMaterialProperties.Float(original,"_Xamount"));
      material.SetColor("_Color",SourceMaterialProperties.Color(original,"_Color"));material.SetFloat("_Multiply",shadow?1:0);
     }
     else
     {
      material.SetColor("_Color",Color.white);material.SetColor("_TintColor",SourceMaterialProperties.Color(original,"_TintColor"));
      material.SetFloat("_ParticleMultiplier",2);material.SetFloat("_Intensity",1);
     }
     material.SetFloat("_SrcBlend",shadow?2:5);material.SetFloat("_DstBlend",shadow?0:additive?1:10);material.SetFloat("_SourceAlpha",1);
     material.renderQueue=shadow?2999:3000;EditorUtility.SetDirty(material);return material;
    }).ToArray();
   }
   var prefab=PrefabUtility.SaveAsPrefabAsset(actor,Folder+"OriginalTurnTimer.prefab");
   // Disposable source actor must leave the scene before the match scene is saved.
   UnityEngine.Object.DestroyImmediate(actor);actor=null;
   var library=AssetDatabase.LoadAssetAtPath<NativeTurnTimerLibrary>(Folder+"NativeTurnTimer.asset");
   if(library==null){library=ScriptableObject.CreateInstance<NativeTurnTimerLibrary>();AssetDatabase.CreateAsset(library,Folder+"NativeTurnTimer.asset");}
   library.prefab=prefab;library.ropeSeconds=(float)config["ropeCapSeconds"];
   library.sparksPath=(string)config["sparksPath"];library.startBonePath=(string)config["startBonePath"];library.finishBonePath=(string)config["finishBonePath"];library.wickPath=(string)config["wickPath"];library.shadowPath=(string)config["shadowPath"];
   var buttonClip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/RecoveredInterface/AssetBundles/essential_base_global-prefab-0.unity3d/ENDTURN_YOUR_TIMER_DONE.anim");
   library.timeoutButton=BakeClip(buttonClip,"");library.buttonTimeoutDuration=buttonClip.length;
   AnimationCurve ButtonCurve(string property)=>AnimationUtility.GetEditorCurve(buttonClip,AnimationUtility.GetCurveBindings(buttonClip).Single(b=>b.propertyName==property));
   library.buttonAtlasX=ButtonCurve("_MainTex.offset.x");library.buttonAtlasY=ButtonCurve("_MainTex.offset.y");library.buttonHighlight=ButtonCurve("m_Enabled");
   library.rollout=Bake("TurnTimer_edit","TurnTimer_Wick");library.spiral=Bake("TurnTimer_SpiralUnwrap","TurnTimer_Spiral");
   library.spiralMask=RendererCurve("material._Xamount");library.spiralVisibility=RendererCurve("m_Enabled");
   library.rolloutMask=new AnimationCurve(config["rolloutFuseKeys"].Select(k=>new Keyframe((float)k["time"],(float)k["value"],(float)k["inSlope"],(float)k["outSlope"])).ToArray());
   AudioClip Sound(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>((string)config["sounds"].Single(s=>(string)s["name"]==name)["asset"]);
   library.stinger=Sound("Pegasus_Stinger_Fuse_Timer");library.burn=Sound("turn_timer_fuse_burns");library.opens=Sound("rollout");library.unravels=Sound("rollout_secondary");library.explodes=Sound("expiry_explosion");library.flips=Sound("expiry_explosion_secondary");
   EditorUtility.SetDirty(library);
   var match=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
   var board=UnityEngine.Object.FindObjectsByType<NativeBoardPresentation>(FindObjectsInactive.Include).Single();
   var presentation=match.frontEnd.tableSurface.GetComponent<NativeTurnTimerPresentation>();if(presentation==null)presentation=match.frontEnd.tableSurface.AddComponent<NativeTurnTimerPresentation>();
   presentation.match=match;presentation.library=library;presentation.boardRoot=board.boardRoot.transform;
   match.turnDurationSeconds=75;
   if(PrefabUtility.IsPartOfPrefabInstance(match))PrefabUtility.RecordPrefabInstancePropertyModifications(match);
   EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(match.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(match.gameObject.scene);
   return "Original skinned TurnTimer rig, safe original rollout curves/controller speeds, source UV2 burn mask, original PS and six standard-mode WAVs installed; 75-second input deadline and final20-second rope phase.";
  }
  finally{if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);}
 }
}
