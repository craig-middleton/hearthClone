using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HearthstoneClone.Cards;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InstallNativeCardStudy
{
 const string Folder="Assets/_Project/Art/CardStudy/";
 const string Source="Assets/_Project/RecoveredInterface/AssetBundles/essential_base_global-prefab-0.unity3d/";
 const string Table="Assets/_Project/Prefabs/HearthstoneTable/TableCard.prefab";
 static readonly Vector2 Center=new Vector2(-.055f,.041f);
 const float Scale=180f/2.17f;
 static Vector2 Point(GameObject actor,Vector3 world){var p=world-actor.transform.position;return (new Vector2(p.x,p.z)-Center)*Scale;}
 static T Save<T>(T value,string path) where T:UnityEngine.Object
 {
  var old=AssetDatabase.LoadAssetAtPath<T>(path);
  if(old==null){AssetDatabase.CreateAsset(value,path);return value;}
  EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);return old;
 }
 static NativeCardMesh Surface(GameObject original,GameObject proof,NativeCardMesh recipe,int index)
 {
  if(recipe.role==NativeCardRole.Name)return recipe;
  string path=recipe.source.Substring(0,recipe.source.LastIndexOf(" [",StringComparison.Ordinal));
  int sub=int.Parse(recipe.source.Substring(recipe.source.LastIndexOf('[')+1).TrimEnd(']'));
  var source=original.transform.Find(path).GetComponent<MeshRenderer>().sharedMaterials[sub];
  var renderer=proof.transform.Find(path).GetComponent<MeshRenderer>();var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
  recipe.secondaryUV=mesh.uv2;
  recipe.sourceNormals=mesh.normals.Select(n=>{var world=renderer.transform.TransformDirection(n);return new Vector3(world.x,world.z,-world.y);}).ToArray();
  recipe.sourceMaterial=PrototypeCardMaterialBaker.Bake(source,Folder+"Materials",index+"_"+source.name.Replace(' ','_'));
  return recipe;
 }
 static NativeCardMesh Geometry(GameObject actor,MeshRenderer renderer,int sub,Material original,Texture paint,string key)
 {
  var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;var world=mesh.vertices.Select(renderer.transform.TransformPoint).ToArray();
  var triangles=new List<int>();var indices=mesh.GetTriangles(sub);
  for(int i=0;i<indices.Length;i+=3)
  {
   int a=indices[i],b=indices[i+1],c=indices[i+2];
   if(Vector3.Cross(world[b]-world[a],world[c]-world[a]).y>.00000001f)triangles.AddRange(new[]{a,b,c});
  }
  if(triangles.Count==0)throw new Exception("Source study has no top-facing triangles: "+key);
  var st=SourceMaterialProperties.Scale(original,"_MainTex");var offset=SourceMaterialProperties.Offset(original,"_MainTex");
  return new NativeCardMesh{source=AnimationUtility.CalculateTransformPath(renderer.transform,actor.transform)+" ["+sub+"]",role=NativeCardRole.Paint,points=world.Select(v=>Point(actor,v)).ToArray(),uv=mesh.uv.Select(v=>Vector2.Scale(v,st)+offset).ToArray(),secondaryUV=mesh.uv2,sourceNormals=mesh.normals,triangles=triangles.ToArray(),paint=paint,sourceMaterial=PrototypeCardMaterialBaker.Bake(original,Folder+"Materials",key)};
 }
 // Original Card_Hand_Ally RaceUberText: .85 x .17, matching the spell school box.
 // UberText-to-TMP fitting remains compatibility; both use the same recovered Belwe style.
 static Vector2 SourceTribeSize()
 {
  var original=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Card_Hand_Ally.prefab");
  var anchor=original.transform.Find("RootObject/RacePlate_mesh/RaceUberText");
  var x=anchor.TransformVector(Vector3.right);var y=anchor.TransformVector(Vector3.up);
  return Vector2.Scale(new Vector2(.85f,.17f),new Vector2(new Vector2(x.x,x.z).magnitude,new Vector2(y.x,y.z).magnitude))*Scale;
 }
 static void TribePresentation(NativeCardFace face)
 {
  var school=face.labels.Single(l=>l.role==NativeCardRole.School&&l.text.transform.IsChildOf(face.spellFace.transform)).text;
  var text=face.studyTribe;
  text.font=school.font;text.fontSharedMaterial=school.fontSharedMaterial;
  text.enableAutoSizing=school.enableAutoSizing;text.fontSizeMin=school.fontSizeMin;
  text.fontSizeMax=school.fontSizeMax;text.fontSize=school.fontSizeMax;
  text.alignment=school.alignment;text.textWrappingMode=school.textWrappingMode;
  text.overflowMode=school.overflowMode;text.margin=school.margin;
  // UI sibling order must preserve the source plate behind the original stat gems.
  var firstGem=face.minionFace.GetComponentsInChildren<NativeCardLayer>(true)
   .Where(p=>p.Definition.source.Contains("/Gem_")).OrderBy(p=>p.transform.GetSiblingIndex()).First();
  foreach(var plate in face.studyDecorations)plate.transform.SetSiblingIndex(firstGem.transform.GetSiblingIndex());
 }
 public static string RefreshTribePresentation()
 {
  if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit required");
  var root=PrefabUtility.LoadPrefabContents(Table);
  try
  {
   var face=root.GetComponent<NativeCardFace>();
   foreach(var entry in face.study.entries)entry.tribeSize=SourceTribeSize();
   EditorUtility.SetDirty(face.study);TribePresentation(face);
   PrefabUtility.SaveAsPrefabAsset(root,Table);AssetDatabase.SaveAssets();
   return "Beast uses original .85 x .17 source text box and shared school font fitting; source plate draws behind attack/health gems.";
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 public static string Main()
 {
  if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit required");
  Directory.CreateDirectory(Folder+"Materials");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var baseline=AssetDatabase.LoadAssetAtPath<NativeCardLibrary>("Assets/_Project/Art/NativeCards/NativeCards.asset");
  var clone=UnityEngine.Object.Instantiate(baseline);clone.name="Grizzly and Fireball source study";
  var cards=AssetDatabase.FindAssets("t:CardData",new[]{"Assets/_Project"}).Select(g=>AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(g))).Where(c=>c.cardName=="Ironfur Grizzly"||c.cardName=="Fireball").ToArray();
  if(cards.Count(c=>c.cardName=="Ironfur Grizzly")!=1||!cards.Any(c=>c.cardName=="Fireball"))throw new Exception("Target card data missing");
  Vector2 tribePosition=default,tribeSize=default,schoolPosition=default;
  var proof=EditorSceneManager.OpenScene("Assets/Scenes/RecoveredInterfaceProof.unity",OpenSceneMode.Additive);
  try
  {
   int materialIndex=0;
   foreach(bool spell in new[]{false,true})
   {
    var actor=proof.GetRootGameObjects().Single(g=>g.name==(spell?"Native spell — Fireball":"Native minion — Wolf Rider"));
    var original=AssetDatabase.LoadAssetAtPath<GameObject>(Source+(spell?"Card_Hand_Ability":"Card_Hand_Ally")+".prefab");
    var recipes=spell?clone.spell:clone.minion;
    for(int i=0;i<recipes.Length;i++)recipes[i]=Surface(original,actor,recipes[i],materialIndex++);
    if(!spell)
    {
     var plate=original.transform.Find("RootObject/RacePlate_mesh").GetComponent<MeshRenderer>();
     var mat=plate.sharedMaterial;
     clone.minion=clone.minion.Concat(new[]{Geometry(original,plate,0,mat,SourceMaterialProperties.Texture(mat,"_MainTex"),"Grizzly_source_Beast_plate")}).ToArray();
     var anchor=plate.transform.Find("RaceUberText");tribePosition=Point(original,anchor.position);
     tribeSize=SourceTribeSize();
    }
    else
    {
     var description=actor.transform.Find("RootObject/NonQuestObjects/Mesh/Description_mesh").GetComponent<MeshRenderer>();
     var old=description.GetComponent<MeshFilter>().sharedMesh;
     description.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"Source/Card_InHand_Ability_Description_School_mesh.asset");
     if(description.GetComponent<MeshFilter>().sharedMesh==null)throw new Exception("Original school description mesh absent");
     var sourceDescription=original.transform.Find("RootObject/NonQuestObjects/Mesh/Description_mesh").GetComponent<MeshRenderer>();
     schoolPosition=Point(original,sourceDescription.transform.Find("RaceUberText").position);
     for(int i=0;i<clone.spell.Length;i++)
      if(clone.spell[i].source.Contains("Description_mesh ["))
      {
       int sub=int.Parse(clone.spell[i].source.Split('[').Last().TrimEnd(']'));
       clone.spell[i]=Geometry(actor,description,sub,sourceDescription.sharedMaterials[sub],clone.spell[i].paint,"Fireball_source_school_"+sub);
      }
     description.GetComponent<MeshFilter>().sharedMesh=old;
    }
   }
  }
  finally{EditorSceneManager.CloseScene(proof,true);}
  Directory.CreateDirectory(Folder+"Names");
  foreach(var name in clone.names.Where(n=>cards.Contains(n.card)))
   name.texture=PrototypeCardTypographyBaker.Bake(name.card,name.card.cardType==CardType.Spell,
    Folder+"Names/"+name.card.name+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(name.card)).Substring(0,8)+".png");
  var library=Save(clone,Folder+"GrizzlyFireball.asset");
  var study=ScriptableObject.CreateInstance<NativeCardStudy>();study.baselinePaint=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/NativeCards/OpaquePaint.mat");
  var baselineFace=AssetDatabase.LoadAssetAtPath<GameObject>(Table).GetComponent<NativeCardFace>();
  study.baselineSchoolPosition=baselineFace.labels.Single(l=>l.role==NativeCardRole.School&&l.text.transform.IsChildOf(baselineFace.spellFace.transform)).text.rectTransform.anchoredPosition;
  study.entries=cards.Select(c=>new NativeCardStudy.Entry{card=c,library=library,tribe=c.cardName=="Ironfur Grizzly"?"Beast":"",tribePosition=tribePosition,tribeSize=tribeSize,useOriginalSchoolAnchor=c.cardType==CardType.Spell,schoolPosition=schoolPosition}).ToArray();
  study=Save(study,Folder+"Study.asset");
  var root=PrefabUtility.LoadPrefabContents(Table);
  try
  {
   var face=root.GetComponent<NativeCardFace>();face.study=study;
   foreach(var item in face.studyDecorations??Array.Empty<NativeCardLayer>())if(item!=null)UnityEngine.Object.DestroyImmediate(item.gameObject);
   if(face.studyTribe!=null)UnityEngine.Object.DestroyImmediate(face.studyTribe.gameObject);
   var go=new GameObject("Original Grizzly Beast plate",typeof(RectTransform),typeof(CanvasRenderer),typeof(NativeCardLayer));go.transform.SetParent(face.minionFace.transform,false);
   var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(180,270);
   var part=go.GetComponent<NativeCardLayer>();part.library=library;part.layerIndex=baseline.minion.Length;part.raycastTarget=false;go.SetActive(false);face.studyDecorations=new[]{part};
   var label=new GameObject("Original Grizzly Beast label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(face.minionFace.transform,false);
   var text=label.GetComponent<TextMeshProUGUI>();text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/RecoveredInterface/Proof/Belwe.asset");text.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/RecoveredInterface/Proof/BelweOutlined.mat");text.text="Beast";text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;label.SetActive(false);face.studyTribe=text;
   TribePresentation(face);
   PrefabUtility.SaveAsPrefabAsset(root,Table);
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();
  return "Opt-in Ironfur Grizzly/Fireball source-material study installed: original UV2/secondary paint/gem inputs, Grizzly Beast plate, Fireball school mesh. Other card data uses baseline library.";
 }
}
