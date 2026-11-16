using System;
using System.Linq;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallOriginalCardMotion
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit mode required");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.isDirty) throw new Exception("Save/review the current scene before authoring");
        if (scene.path != "Assets/Scenes/HearthstoneTable.unity") scene = EditorSceneManager.OpenScene("Assets/Scenes/HearthstoneTable.unity");
        var game = UnityEngine.Object.FindObjectsByType<EffectTester>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var camera = UnityEngine.Object.FindObjectsByType<NativeBoardPresentation>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single().boardCamera;
        foreach (var display in new[] { game.handDisplay, game.opponentHandDisplay })
        {
            var motion = display.GetComponent<NativeCardDrawPresentation>();
            if (motion == null) motion = display.gameObject.AddComponent<NativeCardDrawPresentation>();
            motion.boardCamera = camera; motion.opponent = display == game.opponentHandDisplay;
            display.drawPresentation = motion; EditorUtility.SetDirty(display); EditorUtility.SetDirty(motion);
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Installed original friendly/opponent deck draw presentation using recovered board camera; saved playable scene.";
    }
}
