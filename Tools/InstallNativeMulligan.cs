using System;
using System.Linq;
using System.Collections.Generic;
using HearthstoneClone.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class InstallNativeMulligan
{
 const string Folder="Assets/_Project/Art/NativeMulligan/";
 static NativeMulliganArt library;static TMP_FontAsset font;static Material number,opaque;
 public static string Main()
 {
  if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  library=AssetDatabase.LoadAssetAtPath<NativeMulliganArt>(Folder+"NativeMulligan.asset");
  if(library==null){library=ScriptableObject.CreateInstance<NativeMulliganArt>();AssetDatabase.CreateAsset(library,Folder+"NativeMulligan.asset");}
  library.shapes=new[]{Bake("header","OriginalChooseBanner","BannerMulligan"),Bake("keep","Mulligan_Button_Keep","Quad_Btn_Keep_mesh"),Bake("replace","Mulligan_Button_Replace","Quad_Btn_Replace_mesh"),Bake("cross","Mulligan_X_Mark","Quad_mesh"),Bake("confirm","OriginalConfirm","Button_mesh")};
  EditorUtility.SetDirty(library);
  font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/RecoveredInterface/Proof/Belwe.asset");
  number=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/RecoveredInterface/Proof/BelweOutlined.mat");opaque=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/NativeCards/OpaquePaint.mat");
  var e=UnityEngine.Object.FindObjectsByType<EffectTester>(FindObjectsInactive.Include).Single();var presentation=UnityEngine.Object.FindObjectsByType<MatchPresentation>(FindObjectsInactive.Include).Single();
  var native=e.frontEnd.tableSurface.GetComponentsInChildren<NativeBoardPresentation>(true).Single();
  var camera=native.boardCamera;
  var heading=(RectTransform)presentation.mulliganHeading.transform;
  heading.anchorMin=Vector2.zero;heading.anchorMax=Vector2.one;heading.anchoredPosition=Vector2.zero;heading.sizeDelta=Vector2.zero;
  foreach(Transform child in heading.Cast<Transform>().ToArray())if(child.name!="MulliganShade")UnityEngine.Object.DestroyImmediate(child.gameObject);
  var shade=heading.Find("MulliganShade").GetComponent<Image>();shade.rectTransform.anchorMin=shade.rectTransform.anchorMax=Vector2.one*.5f;shade.rectTransform.sizeDelta=((RectTransform)shade.GetComponentInParent<Canvas>().rootCanvas.transform).rect.size;presentation.mulliganShade=shade.rectTransform;shade.rectTransform.anchoredPosition=Vector2.zero;shade.raycastTarget=false;shade.color=new Color(0,0,0,.4f);
  var bannerWorld=new Vector3(-7.7557516f,4.371788f,-1.8f);
  var bannerSource=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"OriginalChooseBanner.prefab");
  if(bannerSource==null)throw new Exception("Run Tools/PrepareNativeMulliganBanner.py before this installer.");
  var bannerMesh=bannerSource.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="BannerMulligan");
  var bannerBounds=ProjectedMesh(camera,heading,bannerSource,bannerMesh,bannerWorld);
  Part("Original parchment banner",heading,"header",bannerBounds.center,bannerBounds.width,true);
  var headline=bannerSource.transform.Find("BannerMulligan/HeadlineText");
  var headlineBounds=ProjectedText(camera,heading,bannerSource,headline,bannerWorld,1.82f,.24f);
  Text("Starting Hand",heading,"Starting Hand",headlineBounds.center,headlineBounds.size,headlineBounds.height*.82f,new Color(.9019608f,.9019608f,.9019608f),true);
  var caption=bannerSource.transform.Find("BannerMulligan/CaptionText");
  var captionBounds=ProjectedText(camera,heading,bannerSource,caption,bannerWorld,2.68f,.22f);
  Text("Original caption",heading,"Keep or Replace Cards",captionBounds.center,captionBounds.size,captionBounds.height*.88f,new Color(.99215686f,.81960785f,.16078432f),true);
  var panel=(RectTransform)e.mulliganPanel;panel.anchoredPosition=new Vector2(0,-20);panel.sizeDelta=new Vector2(1200,430);
  var group=panel.GetComponent<HorizontalLayoutGroup>();group.spacing=70;group.childAlignment=TextAnchor.MiddleCenter;group.childControlWidth=group.childControlHeight=group.childForceExpandWidth=group.childForceExpandHeight=false;group.enabled=false;
  var style=panel.GetComponent<NativeMulliganPresentation>();if(style==null)style=panel.gameObject.AddComponent<NativeMulliganPresentation>();style.library=library;style.font=font;style.outlinedText=number;style.boardCamera=camera;
  style.sourceZoneCenter=new Vector3(-7.740872f,.0023448467f,-4.451679f);style.sourceZoneSize=new Vector2(10,5);style.sourceHandHeight=2.87161303f;style.sourceCardScale=1.1f;
  style.sourceBadgeWidth=SourceWorldWidth("Mulligan_Button_Replace");style.sourceCrossWidth=SourceWorldWidth("Mulligan_X_Mark");
  style.cardSize=style.SourceCardSize(style.SourceCardPosition(0,3));
  var button=e.confirmMulliganButton;foreach(Transform child in button.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
  var rect=(RectTransform)button.transform;rect.anchoredPosition=new Vector2(0,-155);rect.sizeDelta=new Vector2(170,85);
  var image=button.GetComponent<Image>();var contour=button.GetComponent<AtlasContour>();if(contour!=null)UnityEngine.Object.DestroyImmediate(contour);
  image.sprite=null;image.color=Color.clear;image.material=null;image.raycastTarget=true;
  Part("Original blue confirm button",button.transform,"confirm",Vector2.zero,170,true);
  Text("Confirm",button.transform,"Confirm",Vector2.zero,new Vector2(170,50),28,Color.white,true);
  heading.SetAsLastSibling();panel.SetAsLastSibling();button.transform.SetAsLastSibling();
  foreach(var root in e.gameObject.scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<Component>(true))if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveScene(e.gameObject.scene);
  return "Native mulligan source StartingHandZone/camera-projected desktop card scale and badge positions installed; source banner/detail bones and typography. Shapes: "+string.Join(",",library.shapes.Select(s=>s.key+"="+s.points.Length+"v "+s.aspect.ToString("F2")));
 }
 static Vector2 Project(Camera camera,RectTransform parent,Vector3 world)
 {
  RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,camera.WorldToScreenPoint(world),null,out var p);return p;
 }
 static float SourceWorldWidth(string prefab)
 {
  var root=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+prefab+".prefab");
  var vertices=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(r.transform.TransformPoint)).ToArray();
  return vertices.Max(v=>v.x)-vertices.Min(v=>v.x);
 }
 static Rect ProjectedMesh(Camera camera,RectTransform parent,GameObject root,MeshRenderer renderer,Vector3 position)
 {
  var points=renderer.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>Project(camera,parent,renderer.transform.TransformPoint(v)-root.transform.position+position)).ToArray();
  return Rect.MinMaxRect(points.Min(v=>v.x),points.Min(v=>v.y),points.Max(v=>v.x),points.Max(v=>v.y));
 }
 static Rect ProjectedText(Camera camera,RectTransform parent,GameObject root,Transform text,Vector3 position,float width,float height)
 {
  var points=new[]{new Vector3(-width*.5f,-height*.5f,0),new Vector3(-width*.5f,height*.5f,0),new Vector3(width*.5f,-height*.5f,0),new Vector3(width*.5f,height*.5f,0)}
   .Select(v=>Project(camera,parent,text.TransformPoint(v)-root.transform.position+position)).ToArray();
  return Rect.MinMaxRect(points.Min(v=>v.x),points.Min(v=>v.y),points.Max(v=>v.x),points.Max(v=>v.y));
 }
 static NativeMulliganShape Bake(string key,string prefab,string rendererName)
 {
  var root=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+prefab+".prefab");var renderer=root.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name==rendererName);
  var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices.Select(p=>root.transform.InverseTransformPoint(renderer.transform.TransformPoint(p))).ToArray();
  Bounds bounds=new Bounds(vertices[0],Vector3.zero);foreach(var p in vertices)bounds.Encapsulate(p);
  bool xy=bounds.size.y>bounds.size.z;var points=vertices.Select(p=>new Vector2(p.x,xy?p.y:p.z)).ToArray();
  Vector2 min=new Vector2(points.Min(p=>p.x),points.Min(p=>p.y)),max=new Vector2(points.Max(p=>p.x),points.Max(p=>p.y));var size=max-min;
  var indices=mesh.GetTriangles(0);var triangles=new List<int>();
  for(int i=0;i<indices.Length;i+=3){int a=indices[i],b=indices[i+1],c=indices[i+2];var u=points[b]-points[a];var v=points[c]-points[a];if(u.x*v.y-u.y*v.x<-.00000001f){triangles.Add(a);triangles.Add(b);triangles.Add(c);}}
  if(triangles.Count==0)triangles.AddRange(indices);
  var mat=renderer.sharedMaterial;var serialized=new SerializedObject(mat);var pairs=serialized.FindProperty("m_SavedProperties.m_TexEnvs");Texture paint=null;Vector2 scale=Vector2.one,offset=Vector2.zero;
  for(int i=0;i<pairs.arraySize;i++){var pair=pairs.GetArrayElementAtIndex(i);if(pair.FindPropertyRelative("first").stringValue!="_MainTex")continue;var value=pair.FindPropertyRelative("second");paint=(Texture)value.FindPropertyRelative("m_Texture").objectReferenceValue;scale=value.FindPropertyRelative("m_Scale").vector2Value;offset=value.FindPropertyRelative("m_Offset").vector2Value;}
  if(paint==null||size.x<.00001f||size.y<.00001f)throw new Exception("Invalid source shape: "+key);
  return new NativeMulliganShape{key=key,source=prefab+"/"+rendererName,paint=paint,aspect=size.x/size.y,points=points.Select(p=>new Vector2((p.x-min.x)/size.x-.5f,(p.y-min.y)/size.y-.5f)).ToArray(),uv=mesh.uv.Select(u=>Vector2.Scale(u,scale)+offset).ToArray(),triangles=triangles.ToArray()};
 }
 static NativeMulliganPart Part(string name,Transform parent,string key,Vector2 position,float width,bool solid)
 {
  var root=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(NativeMulliganPart));root.transform.SetParent(parent,false);var rect=(RectTransform)root.transform;
  rect.anchoredPosition=position;rect.sizeDelta=new Vector2(width,width/library.Get(key).aspect);
  var part=root.GetComponent<NativeMulliganPart>();part.library=library;part.shape=key;part.raycastTarget=false;if(solid)part.material=opaque;return part;
 }
 static void Text(string name,Transform parent,string value,Vector2 position,Vector2 size,float fontSize,Color color,bool outline)
 {
  var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.anchoredPosition=position;rect.sizeDelta=size;
  var text=go.GetComponent<TextMeshProUGUI>();text.font=font;if(outline)text.fontSharedMaterial=number;text.text=value;text.fontSize=fontSize;text.enableAutoSizing=true;text.fontSizeMax=fontSize;text.fontSizeMin=fontSize*.6f;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;text.color=color;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
 }
}
