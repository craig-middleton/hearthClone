using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using HearthstoneClone.UI;
public static class InstallDeckPockets
{
 const string Art="Assets/_Project/Art/HearthstoneTable";
 static Sprite frame,dark,back;static TMP_FontAsset font;static Material number;
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first");
  string path=Art+"/ORG_GroundCorners.png";
  if(!File.Exists(path))File.Copy("/home/craig-middleton/Desktop/Hearthstone Export/Texture2D/ORG_GroundCorners.png",path);
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
  importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaSource=TextureImporterAlphaSource.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  frame=Slice("ORG_GroundCorners.png","DeckPocketRim",960,280,64,197);
  dark=Slice("Hero_Power_Frame.png","PocketInterior",112,110,24,24);
  back=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/ClassicBack.asset");font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art+"/TableSerif.asset");number=AssetDatabase.LoadAssetAtPath<Material>(Art+"/OutlinedNumbers.mat");
  var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();var surface=GameObject.Find("GameCanvas").transform.Find("TableSurface");
  var old=surface.Find("DeckPile");if(old!=null)old.gameObject.SetActive(false);
  var previous=surface.Find("DeckPockets");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
  var root=Rect("DeckPockets",surface,0,0,1920,1080);
  Pocket(root,e.opponentFaceView,"OpponentDeck",785,270);
  Pocket(root,e.faceView,"PlayerDeck",785,-165);
  foreach(var c in e.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)))
   if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
  EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
  return "Installed upper/lower right-edge native deck pockets with independent live deck stacks and counts";
 }
 static void Pocket(Transform parent,FaceView owner,string name,float x,float y)
 {
  var root=Rect(name,parent,x,y,108,214);var rim=Image("StoneRim",root,frame,0,0,108,214,new Color(.82f,.73f,.62f));
  rim.gameObject.AddComponent<AtlasContour>().outline=new[]{new Vector2(.12f,0),new Vector2(.88f,0),new Vector2(.98f,.06f),new Vector2(.98f,.94f),new Vector2(.88f,1),new Vector2(.12f,1),new Vector2(.02f,.94f),new Vector2(.02f,.06f)};
  Image("Recess",root,dark,-3,0,61,174,new Color(.65f,.65f,.65f));
  var clip=Rect("InsidePocket",root,-3,0,59,168);clip.gameObject.AddComponent<RectMask2D>();
  var layers=new GameObject[6];
  for(int i=0;i<layers.Length;i++)
  {
   var edge=Image("CardEdge"+i,clip,back,-10+i*3,-7+i*2,67,140,Color.white);
   edge.rectTransform.localRotation=Quaternion.Euler(0,58,0);layers[i]=edge.gameObject;
  }
  // A narrow stone lip occludes the deck edge so the stack sits inside the board.
  Image("InnerLip",root,Slice("ORG_GroundCorners.png","DeckPocketLip",963,330,10,110),-35,0,13,181,new Color(.82f,.73f,.62f));
  var text=Rect("Remaining",root,0,-121,90,35).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSharedMaterial=number;text.fontSize=23;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.text="0";
  var so=new SerializedObject(owner);var array=so.FindProperty("deckPileLayers");array.arraySize=layers.Length;
  for(int i=0;i<layers.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=layers[i];
  so.FindProperty("deckCountText").objectReferenceValue=text;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(owner);
 }
 static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
 {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
 static Image Image(string name,Transform parent,Sprite s,float x,float y,float w,float h,Color color)
 {var im=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();im.sprite=s;im.color=color;im.raycastTarget=false;return im;}
 static Sprite Slice(string file,string name,float x,float y,float w,float h)
 {
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+file);var sprite=Sprite.Create(texture,new Rect(x,texture.height-y-h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=name;
  string path=Art+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(old==null){AssetDatabase.CreateAsset(sprite,path);return sprite;}
  EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);return old;
 }
}
