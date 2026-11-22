using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using HearthstoneClone.UI;
using HearthstoneClone.Cards;
public static class InstallNativeSpells
{
 const string F="Assets/_Project/Art/NativeSpells/";
 [Serializable]class Data{public NativeSpellLibrary.Entry[] entries;}
 static bool Saved(Material mat,string name,string category)
 {
  var list=new SerializedObject(mat).FindProperty("m_SavedProperties."+category);
  for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue==name)return true;
  return false;
 }
 static AnimationCurve Curve(NativeSpellLibrary.Key[] keys)=>new AnimationCurve(keys.Select(k=>new Keyframe(k.time,k.value,k.inSlope,k.outSlope)).ToArray());
 static void InstallPortraits()
 {
  // These keys are verified from each original CardDef's m_PortraitTexturePath.
  var names=new[]{"Fireball","Frostbolt","Arcane Explosion","Flamestrike"};
  var files=new[]{"W15_A087_D.png","WoW_CHI_006_D.png","W10_A176_D.png","WOW_SPE_006(b)_D.png"};
  var cards=AssetDatabase.FindAssets("t:CardData",new[]{"Assets/_Project"})
   .Select(g=>AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
  for(int i=0;i<names.Length;i++)
  {
   string path=F+"Portraits/"+files[i];var importer=AssetImporter.GetAtPath(path) as TextureImporter;
   if(importer==null)throw new Exception("Verified original spell portrait absent "+path);
   importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
   importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
   var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
   if(sprite==null)throw new Exception("Verified original spell portrait did not import "+path);
   foreach(var card in cards.Where(c=>c!=null&&c.cardName==names[i])){card.artwork=sprite;EditorUtility.SetDirty(card);}
  }
 }
 public static string Main()
 {
  if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var data=JsonUtility.FromJson<Data>(File.ReadAllText(F+"SourceConfiguration.json"));
  var library=AssetDatabase.LoadAssetAtPath<NativeSpellLibrary>(F+"NativeSpells.asset");if(library==null){library=ScriptableObject.CreateInstance<NativeSpellLibrary>();AssetDatabase.CreateAsset(library,F+"NativeSpells.asset");}
  library.entries=data.entries;
  foreach(var entry in library.entries)foreach(var phase in new[]{entry.missile,entry.impact})
  {
   var template=AssetDatabase.LoadAssetAtPath<GameObject>(phase.sourcePrefab);if(template==null)throw new Exception("Source spell template absent "+phase.sourcePrefab);
   var actor=UnityEngine.Object.Instantiate(template);
   try
   {
   actor.name=phase.name;actor.transform.position=Vector3.zero;
   foreach(var animation in actor.GetComponentsInChildren<Animation>(true)){animation.enabled=false;animation.playAutomatically=false;}
   foreach(var animator in actor.GetComponentsInChildren<Animator>(true))animator.enabled=false;
   foreach(var light in actor.GetComponentsInChildren<Light>(true))light.enabled=true;
   foreach(var r in actor.GetComponentsInChildren<Renderer>(true))
   {
    r.sharedMaterials=r.sharedMaterials.Select((source,slot)=>
    {
     if(source==null)
     {
      // The original serialized PS renderer has a null second (trail) slot.
      // Its TrailModule is disabled, so this slot remains intentionally empty.
      var particles=r as ParticleSystemRenderer;
      if(particles!=null&&slot==1&&!particles.GetComponent<ParticleSystem>().trails.enabled)return null;
      throw new Exception("Null active source spell material: "+r.name+" slot "+slot);
     }
     string path=F+source.name.Replace(" ","_")+"_Compatibility.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
     if(mat==null){mat=new Material(Shader.Find("HearthClone/Compatibility/Original Spell Surface"));AssetDatabase.CreateAsset(mat,path);}
     mat.shader=Shader.Find("HearthClone/Compatibility/Original Spell Surface");
     var paint=SourceMaterialProperties.Texture(source,"_MainTex");if(paint==null)throw new Exception("Source spell paint missing "+source.name);
     mat.SetTexture("_MainTex",paint);mat.SetTextureScale("_MainTex",SourceMaterialProperties.Scale(source,"_MainTex"));mat.SetTextureOffset("_MainTex",SourceMaterialProperties.Offset(source,"_MainTex"));
     mat.SetColor("_Color",Saved(source,"_Color","m_Colors")?SourceMaterialProperties.Color(source,"_Color"):Color.white);
     mat.SetColor("_TintColor",Saved(source,"_TintColor","m_Colors")?SourceMaterialProperties.Color(source,"_TintColor"):Color.white);
     mat.SetFloat("_ParticleMultiplier",Saved(source,"_TintColor","m_Colors")?2:1);mat.SetFloat("_SourceAlpha",1);
     mat.SetFloat("_Intensity",Saved(source,"_Intensity","m_Floats")?SourceMaterialProperties.Float(source,"_Intensity"):1);
     mat.SetFloat("_SrcBlend",Saved(source,"_RgbSrcBlend","m_Floats")?SourceMaterialProperties.Float(source,"_RgbSrcBlend"):5);
     mat.SetFloat("_DstBlend",Saved(source,"_RgbDstBlend","m_Floats")?SourceMaterialProperties.Float(source,"_RgbDstBlend"):1);
     mat.renderQueue=3000;EditorUtility.SetDirty(mat);return mat;
    }).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
   }
   phase.prefab=PrefabUtility.SaveAsPrefabAsset(actor,F+phase.name+"_Playable.prefab");
   }
   finally{UnityEngine.Object.DestroyImmediate(actor);}
   phase.sound=AssetDatabase.LoadAssetAtPath<AudioClip>(phase.soundPath);if(phase.sound==null)throw new Exception("Source spell sound missing "+phase.soundPath);
   if(phase.flashAlphaKeys!=null&&phase.flashAlphaKeys.Length>0){phase.flashAlpha=Curve(phase.flashAlphaKeys);phase.lightIntensity=Curve(phase.lightKeys);}
  }
  InstallPortraits();
  EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
  var e=UnityEngine.Object.FindObjectsByType<EffectTester>(FindObjectsInactive.Include).Single();var effects=e.battleEffects;
  var native=effects.GetComponent<NativeSpellPresentation>();if(native==null)native=effects.gameObject.AddComponent<NativeSpellPresentation>();
  native.library=library;native.overlay=effects.overlay;native.boardCamera=UnityEngine.Object.FindObjectsByType<NativeBoardPresentation>(FindObjectsInactive.Include).Single().boardCamera;
  native.composite=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HearthstoneTable/SpellLightComposite.mat");effects.originalSpells=native;
  EditorUtility.SetDirty(native);EditorUtility.SetDirty(effects);
  foreach(var c in new Component[]{native,effects})if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
  EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveScene(e.gameObject.scene);AssetDatabase.SaveAssets();
  return "Original Fireball/Frostbolt missile/impact models, particles, exact sounds, source .4s easeInQuad, flash/light curves and source viewport captures installed.";
 }
}
