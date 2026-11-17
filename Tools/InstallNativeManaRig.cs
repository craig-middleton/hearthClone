using System;
using System.Linq;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class InstallNativeManaRig
{
 const string Folder="Assets/_Project/Art/NativeMana/";
 public static string Main()
 {
  if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var e=UnityEngine.Object.FindObjectsByType<EffectTester>(FindObjectsInactive.Include).Single();
  var board=UnityEngine.Object.FindObjectsByType<NativeBoardPresentation>(FindObjectsInactive.Include).Single();
  var library=AssetDatabase.LoadAssetAtPath<NativeManaLibrary>(Folder+"SourceCurves.asset");
  if(library==null){library=ScriptableObject.CreateInstance<NativeManaLibrary>();AssetDatabase.CreateAsset(library,Folder+"SourceCurves.asset");}
  library.birth=Clip("ManaGemBirth");library.used=Clip("ManaGemUsed");library.restore=Clip("ManaGem_Restore");EditorUtility.SetDirty(library);
  var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"CrystalCompatibility.mat");
  if(mat==null){mat=new Material(Shader.Find("HearthClone/Compatibility/Original Mana Crystal"));AssetDatabase.CreateAsset(mat,Folder+"CrystalCompatibility.mat");}
  mat.CopyPropertiesFromMaterial(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/RecoveredInterface/AssetBundles/essential_base_global-material-0.unity3d/Gem_Mana_Blue.mat"));
  mat.shader=Shader.Find("HearthClone/Compatibility/Original Mana Crystal");EditorUtility.SetDirty(mat);
  var surface=e.frontEnd.tableSurface.transform;
  foreach(string name in new[]{"Native mana crystal view","Native mana crystals"}){var old=surface.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
  var root=new GameObject("Native mana crystals",typeof(NativeManaRack));root.transform.SetParent(surface,false);
  var rack=root.GetComponent<NativeManaRack>();rack.boardRoot=board.boardRoot.transform;rack.library=library;rack.crystalMaterial=mat;
  surface.Find("ManaTray").gameObject.SetActive(false);
  rack.sourceMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/RecoveredInterface/AssetBundles/essential_base_qualityhigh_global-content-0.unity3d/ManaGem.asset");
  e.faceView.nativeManaRack=rack;surface.Find("ManaCrystalRow").gameObject.SetActive(false);
  e.handDisplay.handPanel.SetAsLastSibling();e.opponentHandDisplay.handPanel.SetAsLastSibling();
  EditorUtility.SetDirty(e.faceView);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveScene(e.gameObject.scene);
  return "Installed direct native-board resource meshes in the original frame recess; UI tray and crystal overlays disabled. "+library.birth.curves.Length+" birth bindings.";
 }
 static NativeManaClip Clip(string name)
 {
  var source=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+name+".anim");
  var bindings=AnimationUtility.GetCurveBindings(source).Where(b=>string.IsNullOrEmpty(b.path));
  return new NativeManaClip{duration=source.length,curves=bindings.Select(b=>new NativeManaCurve{property=b.propertyName.Replace("material.",""),curve=AnimationUtility.GetEditorCurve(source,b)}).ToArray()};
 }
}
