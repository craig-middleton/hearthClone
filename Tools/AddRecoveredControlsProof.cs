using System;
using System.Linq;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Adds source meshes and a native animation study to the isolated card proof.
public static class AddRecoveredControlsProof
{
    const string Root = "Assets/_Project/RecoveredInterface";
    const string Prefabs = Root + "/AssetBundles/essential_base_global-prefab-0.unity3d/";
    static T Load<T>(string name) where T : UnityEngine.Object
    {
        var match = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Root }).Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>).FirstOrDefault(x => x.name == name);
        if (match == null) throw new Exception("Missing recovered asset: " + name);
        return match;
    }
    public static string Main()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path != "Assets/Scenes/RecoveredInterfaceProof.unity")
            throw new Exception("Open the isolated interface proof in Edit mode.");
        foreach (string name in new[] { "Recovered targeting parts", "Native End Turn study" })
        {
            var old = GameObject.Find(name); if (old != null) UnityEngine.Object.DestroyImmediate(old);
        }
        var arrow = new GameObject("Recovered targeting parts");
        Vector3 start = new Vector3(1.05f, 0, -.25f), end = new Vector3(2.9f, 0, 1.15f);
        Vector3 control = new Vector3(2.4f, 0, -.10f);
        var paint = Load<Material>("TargetingArrow-ArrowDashes");
        var targetPaint = Load<Material>("TargetingArrow-ArrowTarget");
        paint.shader = Shader.Find("Unlit/Transparent"); targetPaint.shader = Shader.Find("Unlit/Texture");
        // Original topology selects each atlas region and leaves the ring genuinely hollow.
        for (int i = 0; i < 7; i++)
        {
            float t = .08f + i * .125f;
            var point = (1-t)*(1-t)*start + 2*(1-t)*t*control + t*t*end;
            var tangent = (2*(1-t)*(control-start) + 2*t*(end-control)).normalized;
            Part("Original dash " + i, arrow.transform, Load<Mesh>("TargetArrow_DashMesh"), paint, point,
                Quaternion.LookRotation(tangent, Vector3.up), .30f);
        }
        var direction = (end - control).normalized;
        Part("Original arrowhead", arrow.transform, Load<Mesh>("TargetArrow_ArrowMesh"), paint,
            end - direction * .23f + Vector3.up * .08f, Quaternion.LookRotation(-direction, Vector3.up), .44f);
        Part("Original hollow target", arrow.transform, Load<Mesh>("TargetArrow_TargetMesh"), targetPaint,
            end + Vector3.up * .025f, Quaternion.identity, .52f);

        var holder = new GameObject("Native End Turn study");
        holder.transform.position = new Vector3(2.3f, 0, -1.20f);
        var button = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "EndTurnButton.prefab"));
        button.transform.SetParent(holder.transform, false);
        foreach (var animation in button.GetComponentsInChildren<Animation>(true)) { animation.playAutomatically = false; animation.Stop(); }
        var mesh = button.transform.Find("ButtonContainer/ButtonMesh");
        var green = mesh.Find("GreenHighlightPlane");
        // Native clips reference this historical child name. Reconcile it in the proof instance only.
        green.name = "Highlight_Plane";
        var greenRenderer = green.GetComponent<Renderer>();
        greenRenderer.sharedMaterial.shader = Shader.Find("HearthClone/Interface/Recovered Highlight Study");
        greenRenderer.enabled = false;
        mesh.Find("WhiteHighlightPlane").GetComponent<Renderer>().enabled = false;
        var material = mesh.GetComponent<Renderer>().sharedMaterial;
        material.shader = Shader.Find("Unlit/Texture");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Proof/Belwe.asset");
        Label(mesh.Find("MyTurnText"), "END TURN", font);
        Label(mesh.Find("WaitingText"), "Enemy Turn", font);
        var animator = mesh.GetComponent<Animation>();
        var yourTurn = Load<AnimationClip>("ENDTURN_YOUR_TURN");
        yourTurn.SampleAnimation(mesh.gameObject, yourTurn.length);
        greenRenderer.enabled = false;
        var motion = holder.AddComponent<NativeInterfaceProofMotion>();
        motion.buttonAnimation = animator; motion.buttonRenderer = mesh.GetComponent<Renderer>(); motion.greenHighlight = greenRenderer;
        foreach (var component in holder.GetComponentsInChildren<Component>(true))
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(holder.scene); EditorSceneManager.SaveScene(holder.scene);
        return "Added source targeting head/dashes/hollow ring and native End Turn mesh with original flip/press/turn clips. Play this proof to view the button cycle; no live gameplay changes.";
    }
    static void Part(string name, Transform parent, Mesh mesh, Material material, Vector3 position, Quaternion rotation, float scale)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
        go.transform.position = position; go.transform.rotation = rotation; go.transform.localScale = Vector3.one * scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
    static void Label(Transform anchor, string value, TMP_FontAsset font)
    {
        var go = new GameObject("Reconstructed native-font label", typeof(TextMeshPro)); go.transform.SetParent(anchor, false);
        var text = go.GetComponent<TextMeshPro>(); text.font = font; text.text = value;
        text.rectTransform.sizeDelta = new Vector2(1f, .30f); text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 3.2f; text.enableAutoSizing = true; text.fontSizeMin = 1.5f; text.fontSizeMax = 3.2f;
        text.color = new Color(.12f,.075f,.025f); text.textWrappingMode = TextWrappingModes.NoWrap;
    }
}
