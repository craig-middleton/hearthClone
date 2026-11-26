using System;
using System.Linq;
using HearthstoneClone.Cards;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallGrizzlyTauntPrototype
{
    const string Folder = "Assets/_Project/Art/GrizzlyPrototype/";
    public static string Main()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Clean Edit mode required");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var data = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_Project/ScriptableObjects/KeywordMinions/IronfurGrizzly.asset");
        var library = AssetDatabase.LoadAssetAtPath<GrizzlyTauntPrototypeLibrary>(Folder + "GrizzlyTaunt.asset");
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<GrizzlyTauntPrototypeLibrary>();
            AssetDatabase.CreateAsset(library, Folder + "GrizzlyTaunt.asset");
        }
        library.grizzlyArtwork = data.artwork;
        library.originalSpawnClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "AllyInPlaySpawn.anim");
        if (library.originalSpawnClip == null) throw new Exception("Original source spawn clip absent");
        library.spawnChannels = AnimationUtility.GetCurveBindings(library.originalSpawnClip)
            .Where(b => b.type == typeof(Transform) && string.IsNullOrEmpty(b.path) &&
                (b.propertyName.StartsWith("m_LocalPosition.") || b.propertyName.StartsWith("m_LocalRotation.")))
            .Select(b => new GrizzlyTauntPrototypeLibrary.Channel { property = b.propertyName,
                curve = AnimationUtility.GetEditorCurve(library.originalSpawnClip, b) }).ToArray();
        if (library.spawnChannels.Length != 7) throw new Exception("Incomplete original spawn channels");
        library.spawnDuration = library.spawnChannels.Max(c => c.curve.keys.Last().time);
        library.tauntUpSound = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "taunt_shield_up.wav");
        if (library.tauntUpSound == null) throw new Exception("Run PrepareGrizzlyTauntPrototype.py first");
        EditorUtility.SetDirty(library);
        const string prefabPath = "Assets/_Project/Prefabs/HearthstoneTable/TableMinion.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var adapter = prefab.GetComponent<GrizzlyTauntPrototype>();
            if (adapter == null) adapter = prefab.AddComponent<GrizzlyTauntPrototype>();
            adapter.library = library;
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        var board = UnityEngine.Object.FindObjectsByType<NativeBoardPresentation>(FindObjectsInactive.Include).Single();
        if (board.boardRoot.GetComponent<GrizzlyTauntPrototypeDirector>() == null)
            board.boardRoot.AddComponent<GrizzlyTauntPrototypeDirector>();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        EditorSceneManager.SaveScene(board.gameObject.scene);
        return "Ironfur Grizzly-only original .4833s summon clip + taunt delay/.4s Spring birth + exact up sound installed. Other minions unchanged. Original taunt fade/gleam/particle/death sequence remains incomplete.";
    }
}
