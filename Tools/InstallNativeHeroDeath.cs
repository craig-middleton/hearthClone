using System;
using System.IO;
using System.Linq;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Newtonsoft.Json.Linq;
public static class InstallNativeHeroDeath
{
 const string Source="Assets/_Project/Art/NativeHeroEffects/",Folder="Assets/_Project/Art/NativeHeroDeath/";
 static CanvasGroup[] Labels(FaceView face)
 {
  var so=new SerializedObject(face);return new[]{"healthGemImage","healthNumberText","armorDisplay"}.Select(name=>so.FindProperty(name).objectReferenceValue).Where(o=>o!=null).Select(o=>o is GameObject g?g:((Component)o).gameObject).Distinct().Select(g=>{var group=g.GetComponent<CanvasGroup>();return group!=null?group:g.AddComponent<CanvasGroup>();}).ToArray();
 }
 static NativeHeroDeathLibrary.Curve[] Bake(string name,string prefix)
 {
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Source+name+".anim");if(clip==null)throw new Exception("Missing original "+name);
  return AnimationUtility.GetCurveBindings(clip).Select(b=>new NativeHeroDeathLibrary.Curve{path=prefix+(string.IsNullOrEmpty(b.path)?"":"/"+b.path),property=b.propertyName.Replace("material.",""),type=b.type.FullName,curve=AnimationUtility.GetEditorCurve(clip,b)}).ToArray();
 }
 static AnimationCurve Clock(JToken machine,string state)
 {
  var action=machine["states"].Single(s=>(string)s["name"]==state)["actions"].Single(a=>(string)a["type"]=="AnimateFloatV2");
  var curve=action["params"].Single(p=>(string)p["name"]=="animCurve")["value"]["curve"];
  return new AnimationCurve(curve["m_Curve"].Select(k=>new Keyframe((float)k["time"],(float)k["value"],(float)k["inSlope"],(float)k["outSlope"]){inWeight=(float)k["inWeight"],outWeight=(float)k["outWeight"],weightedMode=(WeightedMode)(int)k["weightedMode"]}).ToArray());
 }
 public static string Main()
 {
  if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var e=UnityEngine.Object.FindObjectsByType<EffectTester>(FindObjectsInactive.Include).Single();
  var board=UnityEngine.Object.FindObjectsByType<NativeBoardPresentation>(FindObjectsInactive.Include).Single();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Card_Play_Hero_EndGame_Lose_Impact.prefab");
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);instance.name="Original hero death";instance.transform.position=Vector3.zero;
  try
  {
  foreach(var a in instance.GetComponentsInChildren<Animation>(true)){a.playAutomatically=false;a.enabled=false;}
  foreach(var a in instance.GetComponentsInChildren<Animator>(true))a.enabled=false;
  foreach(var p in instance.GetComponentsInChildren<ParticleSystem>(true)){var main=p.main;main.playOnAwake=false;p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
  var debris=instance.transform.Find("HeroBombDebris").GetComponentsInChildren<Renderer>(true);
  Bounds bounds=debris.First().bounds;foreach(var r in debris)bounds.Encapsulate(r.bounds);
  var materialCount=0;
  foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
  {
   var materials=renderer.sharedMaterials.Where(m=>m!=null).Select(original=>
   {
    var name=original.name.Replace(" ","_");var path=Folder+name+"_Compatibility.mat";
    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("HearthClone/Compatibility/Original Hero Death"));AssetDatabase.CreateAsset(mat,path);}
    mat.shader=Shader.Find("HearthClone/Compatibility/Original Hero Death");
    var paint=original.GetTexture("_MainTex");if(paint==null)paint=original.GetTexture("_Diffuse");
    // This is an intentional source actor-assigned portrait slot, not missing art.
    // Runtime replaces it independently from the dying hero on every effect instance.
    if(paint==null&&name=="Hero_portrait_template")paint=e.faceView.avatarImage.sprite.texture;
    if(paint==null)throw new Exception("Unresolved original hero death paint: "+name);
    mat.SetTexture("_MainTex",paint);mat.SetTextureScale("_MainTex",Vector2.one);mat.SetTextureOffset("_MainTex",Vector2.zero);
    var so=new SerializedObject(original);var colors=so.FindProperty("m_SavedProperties.m_Colors");
    Color color=Color.white,tint=Color.white;
    for(int i=0;i<colors.arraySize;i++){var p=colors.GetArrayElementAtIndex(i);string key=p.FindPropertyRelative("first").stringValue;var value=p.FindPropertyRelative("second").colorValue;if(key=="_Color")color=value;if(key=="_TintColor")tint=value;}
    float intensity=1;var floats=so.FindProperty("m_SavedProperties.m_Floats");
    for(int i=0;i<floats.arraySize;i++){var f=floats.GetArrayElementAtIndex(i);if(f.FindPropertyRelative("first").stringValue=="_Intensity")intensity=f.FindPropertyRelative("second").floatValue;}
    mat.SetColor("_Color",color);mat.SetColor("_TintColor",tint);mat.SetFloat("_Intensity",intensity);
    bool particle=renderer is ParticleSystemRenderer;bool crater=name=="GenFX_End_Game_Craters";
    bool additive=particle&&!name.Contains("Dust")&&!name.Contains("Smoke")&&!name.Contains("Debris1_mesh");
    if(name=="FX_HeroCracks")additive=true;
    mat.SetFloat("_SrcBlend",particle||crater||name=="FX_HeroCracks"?5:1);mat.SetFloat("_DstBlend",additive?1:particle||crater?10:0);
    mat.SetFloat("_ZWrite",particle||crater||name=="FX_HeroCracks"?0:1);
    mat.SetFloat("_ParticleMultiplier",particle?2:1);mat.renderQueue=crater?2998:particle?3000:name=="FX_HeroCracks"?3001:2000;
    EditorUtility.SetDirty(mat);materialCount++;return mat;
   }).ToArray();
   if(materials.Length==0)renderer.enabled=false;
   renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.Off;
  }
  var crackMesh=instance.transform.Find("FX_CrackQuad").GetComponent<MeshFilter>().sharedMesh;
  // Copy only to enable CPU inspection; source mesh bytes/metadata stay untouched.
  var readablePath=Folder+"OriginalCrackReadable.asset";
  File.WriteAllText(readablePath,File.ReadAllText(AssetDatabase.GetAssetPath(crackMesh)).Replace("m_IsReadable: 0","m_IsReadable: 1"));
  AssetDatabase.ImportAsset(readablePath,ImportAssetOptions.ForceSynchronousImport);crackMesh=AssetDatabase.LoadAssetAtPath<Mesh>(readablePath);
  var crackColors=crackMesh.colors;var crackVertices=crackMesh.vertices;var crackUvs=crackMesh.uv;var crackTriangles=crackMesh.triangles;
  var crackMat=instance.transform.Find("FX_CrackQuad").GetComponent<Renderer>().sharedMaterial;
  var uiMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"OriginalCrackUI.mat");if(uiMat==null){uiMat=new Material(Shader.Find("HearthClone/Compatibility/Original Hero Crack UI"));AssetDatabase.CreateAsset(uiMat,Folder+"OriginalCrackUI.mat");}
  uiMat.SetColor("_Color",crackMat.GetColor("_Color"));uiMat.SetFloat("_Intensity",crackMat.GetFloat("_Intensity"));EditorUtility.SetDirty(uiMat);
  var crackPaint=crackMat.GetTexture("_MainTex");
  var prefab=PrefabUtility.SaveAsPrefabAsset(instance,Folder+"OriginalHeroDeath.prefab");UnityEngine.Object.DestroyImmediate(instance);
  var library=AssetDatabase.LoadAssetAtPath<NativeHeroDeathLibrary>(Folder+"NativeHeroDeath.asset");if(library==null){library=ScriptableObject.CreateInstance<NativeHeroDeathLibrary>();AssetDatabase.CreateAsset(library,Folder+"NativeHeroDeath.asset");}
  library.crackColors=crackColors;library.crackVertices=crackVertices;library.crackUvs=crackUvs;library.crackTriangles=crackTriangles;library.crackPaint=crackPaint;library.crackUiMaterial=uiMat;
  library.sourcePrefab=prefab;library.crack=Bake("HeroCrackQuad_FadeIn","FX_CrackQuad");library.debris=Bake("HeroExplode","HeroBombDebris");library.light=Bake("LightBurst","Point light");
  var sourceData=JObject.Parse(File.ReadAllText("Docs/Recovery/Hero-death-source.json"));var machine=sourceData["machines"][0];library.crackClock=Clock(machine,"State 2");library.explodeClock=Clock(machine,"State 3");
  library.crackDuration=.5f;library.explodeSlowDuration=.8f;library.holdDuration=1.3f;library.debrisCenter=bounds.center;library.debrisWidth=bounds.size.x;
  library.crackSound=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"hero_portrait_crack_1.wav");library.explodeSound=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"hero_portrait_explode_1.wav");EditorUtility.SetDirty(library);
  var presenter=e.frontEnd.tableSurface.GetComponent<NativeHeroDeathPresentation>();if(presenter==null)presenter=e.frontEnd.tableSurface.AddComponent<NativeHeroDeathPresentation>();
  presenter.playerLabels=Labels(e.faceView);presenter.opponentLabels=Labels(e.opponentFaceView);
  presenter.match=e;presenter.boardCamera=board.boardCamera;presenter.boardRoot=board.boardRoot.transform;presenter.library=library;
  EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveScene(e.gameObject.scene);AssetDatabase.SaveAssets();
  return "Original hero death source crack + 17-piece debris curves + particle modules/light + exact two WAVs + source clocks installed; "+materialCount+" source material slots adapted.";
  }
  finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);}
 }
}
