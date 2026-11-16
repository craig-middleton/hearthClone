using System;
using System.Linq;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class InstallNativeMana
{
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Edit mode only");
  var e=UnityEngine.Object.FindObjectsByType<EffectTester>(FindObjectsInactive.Include).Single();
  const string path="Assets/_Project/Prefabs/HearthstoneTable/TableMana.prefab";
  var p=PrefabUtility.LoadPrefabContents(path);
  try{
   var contour=p.GetComponent<AtlasContour>();if(contour!=null)UnityEngine.Object.DestroyImmediate(contour);
   var old=p.GetComponent<Image>();if(old!=null)UnityEngine.Object.DestroyImmediate(old);
   var im=p.AddComponent<NativeManaCrystal>();im.sourceMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/RecoveredDecks/ManaGem.asset");
   im.paint=AssetDatabase.LoadAssetAtPath<Texture>("Assets/_Project/RecoveredInterface/AssetBundles/essential_base_global-texture-2.unity3d/GenFX_Gem_Mana_D.png");
   im.raycastTarget=false;((RectTransform)p.transform).sizeDelta=new Vector2(31,35);
   PrefabUtility.SaveAsPrefabAsset(p,path);
  }finally{PrefabUtility.UnloadPrefabContents(p);}
  var so=new SerializedObject(e.faceView);
  foreach(string field in new[]{"manaCrystalFullMaterial","manaCrystalEmptyMaterial"})so.FindProperty(field).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/NativeCards/OpaquePaint.mat");
  so.ApplyModifiedPropertiesWithoutUndo();
  var surface=e.frontEnd.tableSurface.transform;var row=(RectTransform)surface.Find("ManaCrystalRow");row.sizeDelta=new Vector2(332,40);row.anchoredPosition=new Vector2(556,-458);
  row.GetComponent<HorizontalLayoutGroup>().spacing=2;
  EditorUtility.SetDirty(e.faceView);EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveScene(e.gameObject.scene);AssetDatabase.SaveAssets();
  return "Original ManaGem geometry and source paint installed in compact tray row; no phone gem contour or shader reconstruction.";
 }
}
