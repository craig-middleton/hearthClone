using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
public static class InstallNativeCorners
{
 const string Folder="Assets/_Project/Art/NativeCorners/";
 [Serializable] class Data {public NativeCornerLibrary.Machine[] machines;}
 static string Relative(Transform t,Transform root) => AnimationUtility.CalculateTransformPath(t,root);
 public static string Main()
 {
  if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var native=UnityEngine.Object.FindObjectsByType<NativeBoardPresentation>(FindObjectsInactive.Include).Single();
  var root=native.boardRoot.transform; // Native board root preserves the original ORG path structure.
  var sourceRoot=root.Find("ORG");if(sourceRoot==null)sourceRoot=root;
  var template=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"CornerEffects.prefab");
  if(template==null)throw new Exception("Missing recovered corner effects");
  foreach(var renderer in template.GetComponentsInChildren<ParticleSystemRenderer>(true))
   if(renderer.sharedMaterial==null||renderer.sharedMaterial.GetTexture("_MainTex")==null)throw new Exception("Unresolved original particle paint: "+renderer.name);
  var temp=(GameObject)PrefabUtility.InstantiatePrefab(template);
  var map=new Dictionary<Transform,Transform>();
  foreach(var t in temp.GetComponentsInChildren<Transform>(true))
  {
   var path=Relative(t,temp.transform);var target=path.Length==0?sourceRoot:sourceRoot.Find(path);
   if(target==null)throw new Exception("Missing original transform "+path);
   map.Add(t,target);
  }
  // Copy original Unity native components onto the existing models' hierarchy.
  // No duplicate board geometry or parallel animation bones are created.
  var componentMap=new Dictionary<UnityEngine.Object,UnityEngine.Object>();
  foreach(var pair in map)
  {
   componentMap[pair.Key]=pair.Value;componentMap[pair.Key.gameObject]=pair.Value.gameObject;
   foreach(var source in pair.Key.GetComponents<Component>().Where(c=>c is Collider||c is ParticleSystem||c is ParticleSystemRenderer))
   {
    var target=pair.Value.GetComponent(source.GetType());if(target==null)target=pair.Value.gameObject.AddComponent(source.GetType());
    EditorUtility.CopySerialized(source,target);componentMap[source]=target;
   }
   // Preserve original numeric layers; direct click tests use each source collider.
   if(pair.Key.GetComponent<Collider>()!=null)pair.Value.gameObject.layer=pair.Key.gameObject.layer;
  }
  foreach(var target in componentMap.Values.OfType<Component>().Where(c=>c is ParticleSystem||c is ParticleSystemRenderer))
  {
   var so=new SerializedObject(target);var prop=so.GetIterator();
   while(prop.Next(true))if(prop.propertyType==SerializedPropertyType.ObjectReference&&prop.objectReferenceValue!=null&&componentMap.TryGetValue(prop.objectReferenceValue,out var replacement))prop.objectReferenceValue=replacement;
   so.ApplyModifiedPropertiesWithoutUndo();
  }
  foreach(var pair in map.Where(p=>p.Key.GetComponent<ParticleSystem>()!=null))
  {
   var r=pair.Value.GetComponent<ParticleSystemRenderer>();var source=pair.Key.GetComponent<ParticleSystemRenderer>().sharedMaterial;
   if(source==null||source.GetTexture("_MainTex")==null)throw new Exception("Unresolved original particle material/texture: "+pair.Key.name);
   var matPath=Folder+source.name+"_Compatibility.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
   if(mat==null){mat=new Material(Shader.Find("HearthClone/Compatibility/Source Board Particles"));AssetDatabase.CreateAsset(mat,matPath);}
   mat.CopyPropertiesFromMaterial(source);mat.shader=Shader.Find("HearthClone/Compatibility/Source Board Particles");
   // Recovered corner materials use Hero/Particle/Particles_Transparent.
   mat.SetFloat("_SrcBlend",(int)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
   r.sharedMaterials=new[]{mat};r.shadowCastingMode=ShadowCastingMode.Off;EditorUtility.SetDirty(mat);
  }
  UnityEngine.Object.DestroyImmediate(temp);
  var logicText=File.ReadAllText(Folder+"CornerLogic.json");
  var data=JsonUtility.FromJson<Data>(logicText);
  var library=AssetDatabase.LoadAssetAtPath<NativeCornerLibrary>(Folder+"NativeCorners.asset");
  if(library==null){library=ScriptableObject.CreateInstance<NativeCornerLibrary>();AssetDatabase.CreateAsset(library,Folder+"NativeCorners.asset");}
  library.machines=data.machines;
  for(int mi=0;mi<library.machines.Length;mi++)for(int si=0;si<library.machines[mi].states.Length;si++)for(int ai=0;ai<library.machines[mi].states[si].actions.Length;ai++)
  {
   var m=library.machines[mi];var s=m.states[si];var a=s.actions[ai];if(!a.type.StartsWith("Audio"))continue;
   a.sounds=a.soundPaths.Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();
   if(a.sounds.Any(c=>c==null))throw new Exception("Missing original sound for "+m.path+"/"+s.name);
   if(a.weights==null||a.weights.Length!=a.sounds.Length)throw new Exception("Original sound weights/count mismatch for "+m.path+"/"+s.name);
  }
  EditorUtility.SetDirty(library);
  var runtime=root.GetComponent<NativeCornerInteractions>();if(runtime==null)runtime=root.gameObject.AddComponent<NativeCornerInteractions>();
  runtime.enabled=false;runtime.sourceRoot=sourceRoot;runtime.boardCamera=native.boardCamera;runtime.library=library;
  var targets=map.Values.ToHashSet();
  foreach(var m in library.machines)foreach(var s in m.states)foreach(var a in s.actions)
  {
   if(a.type!="PlayAnimation"&&a.type!="AnimationPlaythroughAction")continue;
   var p=a.parameters.Single(v=>v.name==(a.type=="PlayAnimation"?"gameObject":"m_GameObject"));var t=sourceRoot.Find(p.path.Substring(4));
   foreach(var descendant in t.GetComponentsInChildren<Transform>(true))targets.Add(descendant);
   // Animation events call absent client scripts. Source motion curves stay intact.
   foreach(AnimationState st in t.GetComponent<Animation>())if(AnimationUtility.GetAnimationEvents(st.clip).Length!=0)throw new Exception("Unadapted source animation event in "+st.name);
  }
  runtime.resetPoses=targets.Select(t=>new NativeCornerInteractions.Pose{target=t,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
  runtime.resetRenderers=targets.SelectMany(t=>t.GetComponents<Renderer>()).Select(r=>new NativeCornerInteractions.RendererState{target=r,enabled=r.enabled}).ToArray();
  runtime.resetColliders=targets.SelectMany(t=>t.GetComponents<Collider>()).Select(c=>new NativeCornerInteractions.ColliderState{target=c,enabled=c.enabled}).ToArray();
  runtime.enabled=true;
  foreach(var c in componentMap.Values.OfType<Component>().Append(runtime))if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
  foreach(var target in map.Values)if(PrefabUtility.IsPartOfPrefabInstance(target.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(target.gameObject);
  EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  return "Installed six recovered corner machines on native hierarchy, source colliders/particles/clips and sound bindings.";
 }
}
