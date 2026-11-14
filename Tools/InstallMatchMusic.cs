using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HearthstoneClone.UI;
public static class InstallMatchMusic
{
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before installing music");
  var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var paths=Directory.GetFiles("Assets/_Project/Audio/MatchMusic","*.ogg").OrderBy(p=>p).ToArray();
  e.musicTracks.Clear();
  foreach(var path in paths)
  {
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
   settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;
   importer.defaultSampleSettings=settings;importer.SaveAndReimport();
   var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(clip==null)throw new Exception("Music import failed: "+path);
   e.musicTracks.Add(clip);
  }
  if(e.musicTracks.Count!=10||e.musicTracks.Distinct().Count()!=10)throw new Exception("Expected ten unique songs");
  e.musicSource.playOnAwake=false;e.musicSource.loop=true;e.musicSource.spatialBlend=0;
  EditorUtility.SetDirty(e);EditorUtility.SetDirty(e.musicSource);
  foreach(var component in e.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
   if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
  EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
  return "Installed ten native-source music tracks as streaming Vorbis assets";
 }
}
