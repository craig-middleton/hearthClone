using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using HearthstoneClone.UI;
public static class InstallNativeHandOutlines
{
 const string Folder="Assets/_Project/Art/NativeMinions/",Prefab="Assets/_Project/Prefabs/HearthstoneTable/TableCard.prefab";
 static NativeCardLibrary library;
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Clean Edit mode required");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  library=AssetDatabase.LoadAssetAtPath<NativeCardLibrary>("Assets/_Project/Art/NativeCards/NativeCards.asset");
  var original=AssetDatabase.LoadAssetAtPath<Material>(Folder+"HighlightGreenInPlay.mat");
  var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"OriginalHandOutline_Compatibility.mat");
  if(mat==null){mat=new Material(Shader.Find("HearthClone/UI/Source Hand Silhouette"));AssetDatabase.CreateAsset(mat,Folder+"OriginalHandOutline_Compatibility.mat");}
  mat.shader=Shader.Find("HearthClone/UI/Source Hand Silhouette");foreach(var field in new[]{"_Color","_HiColor"})mat.SetColor(field,SourceMaterialProperties.Color(original,field));foreach(var field in new[]{"_Intensity","_ColorIntensity"})mat.SetFloat(field,SourceMaterialProperties.Float(original,field));EditorUtility.SetDirty(mat);
  var root=PrefabUtility.LoadPrefabContents(Prefab);
  try
  {
   var face=root.GetComponent<NativeCardFace>();var style=root.GetComponent<NativeHandCardPresentation>();if(style==null)style=root.AddComponent<NativeHandCardPresentation>();
   style.minionOutline=Outline(face.minionFace.transform,library.minion,Folder+"Card_InHand_Ally_Silhouette.png",mat);
   style.spellOutline=Outline(face.spellFace.transform,library.spell,Folder+"Card_Inhand_Ability_Silhouette.png",mat);
   PrefabUtility.SaveAsPrefabAsset(root,Prefab);
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();return "Original minion/ability hand silhouette paint, source green parameters, affordability/turn/space eligibility, maskable nonblocking outlines installed.";
 }
 static RawImage Outline(Transform parent,NativeCardMesh[] shapes,string path,Material material)
 {
  var old=parent.Find("Original ready silhouette");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
  var shape=shapes.First(s=>s.role==NativeCardRole.Paint&&s.source.EndsWith("Mesh [0]"));
  var used=shape.triangles.Distinct().Select(i=>shape.points[i]).ToArray();var min=new Vector2(used.Min(v=>v.x),used.Min(v=>v.y));var max=new Vector2(used.Max(v=>v.x),used.Max(v=>v.y));
  // Align original silhouette's half-intensity edge with the actual source body bounds.
  var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));var pixels=image.GetPixels32();int left=image.width,right=0,bottom=image.height,top=0;
  for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++)if(pixels[y*image.width+x].r>=128){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
  var low=new Vector2((float)left/image.width,(float)bottom/image.height);var high=new Vector2((float)(right+1)/image.width,(float)(top+1)/image.height);UnityEngine.Object.DestroyImmediate(image);
  var size=new Vector2((max.x-min.x)/(high.x-low.x),(max.y-min.y)/(high.y-low.y));var center=(min+max)*.5f-Vector2.Scale((low+high)*.5f-Vector2.one*.5f,size);
  var go=new GameObject("Original ready silhouette",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));go.transform.SetParent(parent,false);go.transform.SetAsFirstSibling();var rect=(RectTransform)go.transform;rect.anchoredPosition=center;rect.sizeDelta=size;
  var graphic=go.GetComponent<RawImage>();graphic.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);graphic.material=material;graphic.raycastTarget=false;graphic.enabled=false;return graphic;
 }
}
