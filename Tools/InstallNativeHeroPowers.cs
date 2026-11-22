using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using HearthstoneClone.UI;
public static class InstallNativeHeroPowers
{
 const string F="Assets/_Project/Art/NativeHeroEffects/";
 static NativeHeroPowerPresentation.Curve[] Bake(AnimationClip clip)=>AnimationUtility.GetCurveBindings(clip).Where(b=>b.type==typeof(Transform)||b.propertyName=="m_Enabled").Select(b=>new NativeHeroPowerPresentation.Curve{path=b.path,property=b.propertyName,curve=AnimationUtility.GetEditorCurve(clip,b)}).ToArray();
 public static string Main()
 {
  if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var e=UnityEngine.Object.FindObjectsByType<EffectTester>(FindObjectsInactive.Include).Single();
  var board=UnityEngine.Object.FindObjectsByType<NativeBoardPresentation>(FindObjectsInactive.Include).Single();
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(F+"Card_Play_HeroPower.prefab");
  Texture2D powerMask=null;
  foreach(var button in new[]{e.heroPowerButton,e.opponentHeroPowerButton})
  {
   var old=button.GetComponent<HeroPowerPresentation>();var texture=old.artwork.sprite.texture;
   var previous=board.boardRoot.transform.Find("Native "+button.name);if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
   var stage=new GameObject("Native "+button.name).transform;stage.SetParent(board.boardRoot.transform,false);
   var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab,e.gameObject.scene);actor.transform.SetParent(stage,false);actor.transform.localPosition=Vector3.zero;
   foreach(var animation in actor.GetComponentsInChildren<Animation>(true)){animation.playAutomatically=false;animation.Stop();animation.enabled=false;}
   var mesh=actor.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Mesh");
   var center=actor.transform.InverseTransformPoint(mesh.bounds.center);float diameter=mesh.bounds.size.x;
   foreach(var r in actor.GetComponentsInChildren<MeshRenderer>(true))
   {
    var mats=r.sharedMaterials.Select((source,index)=>
    {
     bool transparent=r.name=="shadow"||r.name=="Highlight_Card_Plane";
     string path=F+button.name+"_"+source.name.Replace(" ","_")+"_Compatibility.mat";
     var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
     if(mat==null){mat=new Material(Shader.Find(transparent?"HearthClone/Compatibility/Source Board Particles":"Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,path);}
     var paint=SourceMaterialProperties.Texture(source,"_MainTex");if(r.name=="Mesh"&&index==1)paint=texture;
     if(paint==null)throw new Exception("Unresolved power paint: "+source.name);
     if(transparent){mat.SetTexture("_MainTex",paint);mat.SetColor("_TintColor",r.name=="shadow"?new Color(.5f,.5f,.5f,.5f):SourceMaterialProperties.Color(source,"_Color")*.5f);mat.SetFloat("_SrcBlend",r.name=="shadow"?2:5);mat.SetFloat("_DstBlend",r.name=="shadow"?0:1);}
     else{mat.SetTexture("_BaseMap",paint);mat.SetColor("_BaseColor",Color.white);}
     if(r.name=="shadow"){mat.shader=Shader.Find("HearthClone/Compatibility/Source White Neutral Shadow");mat.SetTexture("_MainTex",paint);mat.SetFloat("_Opacity",SourceMaterialProperties.Color(source,"_Color").a);}
     if(r.name=="Highlight_Card_Plane")
     {
      if(powerMask==null)powerMask=NativeHighlightBaker.Bake(actor.transform,r,actor.GetComponentsInChildren<MeshRenderer>(true).Where(x=>x.name=="Mesh"||x.name=="Gem_Mana"),F+"HeroPowerSourceSilhouette.png");
      mat.shader=Shader.Find("HearthClone/Compatibility/Source Selection Highlight");mat.SetTexture("_MainTex",powerMask);mat.SetFloat("_UseUV2",1);
      foreach(var property in new[]{"_Color","_HiColor","_Colorize"})mat.SetColor(property,SourceMaterialProperties.Color(source,property));
      foreach(var property in new[]{"_ColorIntensity","_Intensity"})mat.SetFloat(property,SourceMaterialProperties.Float(source,property));
     }
     EditorUtility.SetDirty(mat);return mat;
    }).ToArray();r.sharedMaterials=mats;
   }
   foreach(var image in button.GetComponentsInChildren<Image>(true)){image.enabled=image.gameObject==button.gameObject;if(image.gameObject==button.gameObject)image.color=Color.clear;}
   foreach(var text in button.GetComponentsInChildren<TMP_Text>(true))text.gameObject.SetActive(false);
   if(old.readyGlow!=null)old.readyGlow.SetActive(false);
   var native=button.GetComponent<NativeHeroPowerPresentation>();if(native==null)native=button.gameObject.AddComponent<NativeHeroPowerPresentation>();
   var previousLabel=button.transform.Find("Original mana cost");if(previousLabel!=null)UnityEngine.Object.DestroyImmediate(previousLabel.gameObject);
   var label=new GameObject("Original mana cost",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));label.transform.SetParent(button.transform,false);
   var cost=label.GetComponent<TextMeshProUGUI>();cost.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/RecoveredInterface/Proof/Belwe.asset");cost.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/RecoveredInterface/Proof/BelweOutlined.mat");cost.fontSize=31;cost.alignment=TextAlignmentOptions.Center;cost.raycastTarget=false;((RectTransform)label.transform).sizeDelta=new Vector2(50,50);cost.text="2";
   native.button=(RectTransform)button.transform;native.stage=stage;native.actor=actor.transform;native.boardCamera=board.boardCamera;native.sourceCenter=center;native.sourceDiameter=diameter;
   native.gem=actor.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Gem_Mana");native.highlight=actor.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Highlight_Card_Plane");native.cost=cost;
   native.usedClip=AssetDatabase.LoadAssetAtPath<AnimationClip>(F+"HeroPower_Used.anim");native.restoreClip=AssetDatabase.LoadAssetAtPath<AnimationClip>(F+"HeroPower_Restore.anim");native.usedCurves=Bake(native.usedClip);native.restoreCurves=Bake(native.restoreClip);old.native=native;stage.gameObject.SetActive(false);
   foreach(var component in actor.GetComponentsInChildren<Component>(true))if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
  }
  foreach(var component in e.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)))if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveScene(e.gameObject.scene);
  return "Two original 3D hero-power actors, source frame/portrait/class submeshes, mana gems, shadow/highlight and used/restore clips installed.";
 }
}
