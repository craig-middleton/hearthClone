using System;
using System.Linq;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
public static class InstallNativeChimney
{
 const string Folder="Assets/_Project/Art/Chimney/";
 public static string Main()
 {
  if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var scene=EditorSceneManager.GetActiveScene();var e=UnityEngine.Object.FindObjectsByType<EffectTester>(FindObjectsInactive.Include).Single();
  var native=UnityEngine.Object.FindObjectsByType<NativeBoardPresentation>(FindObjectsInactive.Include).Single();
  var old=native.boardRoot.transform.Find("Original chimney interaction");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"ChimneyEffects.prefab");var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
  instance.name="Original chimney interaction";instance.transform.SetParent(native.boardRoot.transform,false);
  var particles=instance.GetComponentsInChildren<ParticleSystem>(true);
  foreach(var particle in particles)
  {
   var renderer=particle.GetComponent<ParticleSystemRenderer>();var source=renderer.sharedMaterial;
   string path=Folder+particle.name+"_Compatibility.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(mat==null){mat=new Material(Shader.Find("HearthClone/Compatibility/Source Board Particles"));AssetDatabase.CreateAsset(mat,path);}
   if(source!=null)mat.CopyPropertiesFromMaterial(source);
   mat.shader=Shader.Find("HearthClone/Compatibility/Source Board Particles");
   if(source==null)
   {
    var texture=AssetDatabase.FindAssets("GenFX_Embers01 t:Texture2D").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Texture2D>).First(t=>t.name=="GenFX_Embers01");
    mat.SetTexture("_MainTex",texture);mat.SetColor("_TintColor",new Color(.5f,.5f,.5f,.5f));
   }
   bool additive=particle.name!="FX_SmokeStack";mat.SetFloat("_SrcBlend",(int)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(int)(additive?BlendMode.One:BlendMode.OneMinusSrcAlpha));
   renderer.sharedMaterials=new[]{mat};renderer.shadowCastingMode=ShadowCastingMode.Off;EditorUtility.SetDirty(mat);
  }
  var chimney=instance.AddComponent<NativeChimneyInteraction>();chimney.boardCamera=native.boardCamera;
  chimney.clickArea=instance.GetComponentInChildren<SphereCollider>(true);
  chimney.puff=particles.Single(p=>p.name=="FX_SmokePuff");chimney.ambientSmoke=particles.Single(p=>p.name=="FX_SmokeStack");
  chimney.voice=chimney.puff.gameObject.AddComponent<AudioSource>();chimney.voice.playOnAwake=false;chimney.voice.spatialBlend=1;
  chimney.sounds=Enumerable.Range(1,3).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"Orgrimmar_Clickable_Blacksmith_0"+i+".wav")).ToArray();
  // Centre the existing fan below the actual hero painting, preserving its height.
  Canvas.ForceUpdateCanvases();var hand=(RectTransform)e.handDisplay.handPanel;var parent=(RectTransform)hand.parent;
  var hero=e.faceView.avatarImage.rectTransform;var point=hero.TransformPoint(hero.rect.center);var local=parent.InverseTransformPoint(point);
  hand.anchoredPosition=new Vector2(local.x,hand.anchoredPosition.y);
  PrefabUtility.RecordPrefabInstancePropertyModifications(hand);
  foreach(var component in instance.GetComponentsInChildren<Component>(true))if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  return "Original chimney particles/collider and alternating .2s click states installed; three blacksmith sounds bound; player hand centred x="+hand.anchoredPosition.x;
 }
}
