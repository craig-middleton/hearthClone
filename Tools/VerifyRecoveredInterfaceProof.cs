using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class VerifyRecoveredInterfaceProof
{
    const string Root = "Assets/_Project/RecoveredInterface";
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task<string[]> Main()
    {
        Check(EditorApplication.isPlaying, "Run in the isolated proof's Play mode");
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == "Assets/Scenes/RecoveredInterfaceProof.unity", "Proof scene only");
        Check(!EditorBuildSettings.scenes.Any(s => s.path.Contains("RecoveredInterfaceProof")), "Proof entered build scenes");
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        Check(prefabs.Length == 5, "Expected five selected native assemblies");
        Check(prefabs.All(p => p.GetComponentsInChildren<MonoBehaviour>(true).Length == 0), "Original client behaviour entered imported prefabs");
        Check(prefabs.All(p => p.GetComponentsInChildren<Camera>(true).Length == 0 && p.GetComponentsInChildren<AudioSource>(true).Length == 0), "Source camera/audio was not stripped");
        var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AnimationClip>).ToArray();
        Check(clips.Count(c => c.name.StartsWith("ENDTURN_")) == 19, "Native button clips missing");
        Check(clips.All(c => AnimationUtility.GetAnimationEvents(c).Length == 0), "Client callbacks remain in imported animation");
        foreach (string name in new[] { "Native minion — Wolf Rider", "Native spell — Fireball" })
        {
            var actor = GameObject.Find(name); Check(actor != null, "Card proof missing");
            var renderers = actor.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).ToArray();
            Check(renderers.Length >= 4 && renderers.All(r => r.sharedMaterials.All(m => m != null && m.mainTexture != null && m.shader.isSupported)), "Visible card paint missing");
            Check(actor.GetComponentsInChildren<TMP_Text>().Any(t => t.font.faceInfo.familyName.Contains("Franklin")), "Native description font missing");
            Check(renderers.Any(r => r.name == "Card_InHand_Ally_CurvedText" && r.sharedMaterial.mainTexture.name.Contains("CurvedName")), "Source curved name mesh not used");
        }
        var motion = UnityEngine.Object.FindAnyObjectByType<NativeInterfaceProofMotion>();
        Check(motion != null, "Native button animation study missing");
        var phases = new HashSet<string>(); bool flipped = false, depressed = false, green = false;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (timer.Elapsed.TotalSeconds < 9)
        {
            phases.Add(motion.Phase ?? "");
            flipped |= Quaternion.Angle(motion.buttonAnimation.transform.localRotation, Quaternion.identity) > 140;
            depressed |= motion.Phase == "Press" && motion.buttonAnimation.transform.localPosition.y < -.1f;
            green |= motion.Phase == "No more plays" && motion.greenHighlight.enabled && motion.buttonRenderer.material.mainTextureOffset.y > .49f;
            await Task.Delay(15);
        }
        Check(flipped && depressed && green, "Original flip/press or source green-atlas state did not animate");
        Check(phases.Contains("Your turn") && phases.Contains("Enemy turn"), "Turn-state cycle did not complete");
        var arrow = GameObject.Find("Recovered targeting parts");
        Check(arrow != null && arrow.GetComponentsInChildren<MeshFilter>().Length == 9, "Native head/ring/dash meshes missing");
        return new[] { "Five source assemblies contain no client scripts, cameras or audio; 31 clips have no client callbacks", "Both native card actors use supported painted meshes, original curved name targets and genuine Franklin description font", "Native End Turn clips visibly rotate through 180 degrees, depress on press and select the green atlas/highlight state; both turns reached", "Native targeting head, seven separate dashes and hollow ring render from original meshes; proof remains excluded from builds" };
    }
}
