using System;
using System.Linq;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
public static class InstallNativeMinions
{
 const string Folder="Assets/_Project/Art/NativeMinions/";
 const string Prefab="Assets/_Project/Prefabs/HearthstoneTable/TableMinion.prefab";
 static GameObject Prepare(string name)
 {
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+name+".prefab");var root=UnityEngine.Object.Instantiate(source);root.name=name+"_Renderable";
  if(name=="Card_Play_Ally")root.transform.localPosition=Vector3.zero;
  // Source Taunt State6: settled iTweenScaleTo vector (.9775736 each axis), .4s Spring.
  // The authored .1 scale is its birth pose, not the gameplay size. No new tween.
  if(name=="Card_Play_Ally_Taunt")root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Taunt_Shield_mesh").localScale=Vector3.one*.9775736f;
  foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
  {
   bool shadow=name.Contains("Shadow"),highlight=renderer.name=="Highlight_Card_Plane";
   renderer.sharedMaterials=renderer.sharedMaterials.Select(original=>
   {
    // Original Card_Play_Ally actor states4/5/6 (selectable/selected/playable)
    // map to HighlightGreenInPlay, rather than the template's inactive yellow.
    if(highlight)original=AssetDatabase.LoadAssetAtPath<Material>(Folder+"HighlightGreenInPlay.mat");
    string path=Folder+original.name.Replace(" ","_")+"_Compatibility.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    string shader=highlight?"HearthClone/Compatibility/Source Selection Highlight":shadow?"HearthClone/Compatibility/Source White Neutral Shadow":"Universal Render Pipeline/Unlit";
    if(material==null){material=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(material,path);}material.shader=Shader.Find(shader);
    var texture=SourceMaterialProperties.Texture(original,"_MainTex");
    if(highlight)
    {
     foreach(string field in new[]{"_Color","_HiColor","_Colorize"})material.SetColor(field,SourceMaterialProperties.Color(original,field));
     foreach(string field in new[]{"_ColorIntensity","_Intensity"})material.SetFloat(field,SourceMaterialProperties.Float(original,field));
     material.SetFloat("_UseUV2",renderer.GetComponent<MeshFilter>().sharedMesh.uv2.Length>0?1:0);
     material.SetTexture("_MainTex",Texture2D.blackTexture);
    }
    else
    {
     // Source portrait is dynamic; no painted replacement. Runtime binds card artwork.
     if(texture==null&&original.name.Contains("Portrait"))texture=Texture2D.whiteTexture;
     if(texture==null)throw new Exception("Unresolved original minion paint: "+original.name);
     if(shadow){material.SetTexture("_MainTex",texture);material.SetFloat("_Opacity",SourceMaterialProperties.Color(original,"_Color").a);}
     else{material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",SourceMaterialProperties.Color(original,"_Color"));}
    }
    EditorUtility.SetDirty(material);return material;
   }).ToArray();
   renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  }
  var saved=PrefabUtility.SaveAsPrefabAsset(root,Folder+name+"_Renderable.prefab");UnityEngine.Object.DestroyImmediate(root);return saved;
 }
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var actor=Prepare("Card_Play_Ally");var taunt=Prepare("Card_Play_Ally_Taunt");var shadow=Prepare("Card_InPlay_Ally_Shadow");
  var probe=UnityEngine.Object.Instantiate(actor);probe.transform.position=Vector3.zero;
  Texture2D normalMask,tauntMask;Vector3 center;float width;
  try
  {
   var plane=probe.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Highlight_Card_Plane");
   var mesh=probe.transform.Find("RootObject/Card_InPlay_Ally/Mesh").GetComponent<Renderer>();center=probe.transform.InverseTransformPoint(mesh.bounds.center);width=mesh.bounds.size.x;
   var core=probe.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r!=plane).ToArray();
   normalMask=NativeHighlightBaker.Bake(probe.transform,plane,core,Folder+"InPlay-source-silhouette.png");
   var shield=UnityEngine.Object.Instantiate(taunt,probe.transform);shield.transform.localPosition=taunt.transform.localPosition;
   tauntMask=NativeHighlightBaker.Bake(probe.transform,plane,core.Concat(shield.GetComponentsInChildren<MeshRenderer>(true)),Folder+"InPlay-taunt-source-silhouette.png");
   plane.sharedMaterial.SetTexture("_MainTex",normalMask);EditorUtility.SetDirty(plane.sharedMaterial);
  }
  finally{UnityEngine.Object.DestroyImmediate(probe);}
  var root=PrefabUtility.LoadPrefabContents(Prefab);
  try
  {
   var view=root.GetComponent<MinionView>();var native=root.GetComponent<NativeMinionPresentation>();if(native==null)native=root.AddComponent<NativeMinionPresentation>();
   native.actorPrefab=actor;native.tauntPrefab=taunt;native.shadowPrefab=shadow;native.sourceCenter=center;native.sourceWidth=width;native.normalHighlight=normalMask;native.tauntHighlight=tauntMask;
   view.artworkImage.enabled=false;foreach(var image in view.artworkImage.GetComponentsInParent<Image>())if(image.gameObject!=root)image.enabled=false;
   if(view.tauntVisual!=null)view.tauntVisual.SetActive(false);
   foreach(var image in root.GetComponentsInChildren<Image>(true))if(image.gameObject.name is "AttackGem" or "HealthGem")image.enabled=false;
   // Input/labels remain UI; the source world models own the visual appearance.
   view.normalColor=view.cannotAttackColor=view.selectedColor=view.frozenColor=Color.clear;
   if(view.readyGlow!=null){view.readyGlow.SetActive(false);view.readyGlow=null;}
   PrefabUtility.SaveAsPrefabAsset(root,Prefab);
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();return "Original played-minion frame/portrait/gems, shadow and settled taunt geometry installed, with source-geometry eligibility silhouette/green source material; compatibility blur kernel disclosed.";
 }
}
