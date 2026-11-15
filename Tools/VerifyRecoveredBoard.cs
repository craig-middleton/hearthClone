using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class VerifyRecoveredBoard
{
    public static string Main()
    {
        if(EditorSceneManager.GetActiveScene().path!="Assets/Scenes/RecoveredOrgrimmarProof.unity")throw new InvalidOperationException("Open the recovered proof scene.");
        var board=GameObject.Find("Recovered original Orgrimmar");
        if(board==null)throw new Exception("Recovered root missing");
        int transforms=board.GetComponentsInChildren<Transform>(true).Length;
        if(transforms!=121)throw new Exception("Original hierarchy changed: "+transforms);
        if(board.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)throw new Exception("Game scripts or missing scripts entered the proof");
        if(board.GetComponentsInChildren<Canvas>(true).Length!=0)throw new Exception("Board proof must use mesh geometry");
        var filters=board.GetComponentsInChildren<MeshFilter>(true);
        if(filters.Length!=35||filters.Any(f=>f.sharedMesh==null))throw new Exception("Missing original static mesh");
        var skins=board.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if(skins.Length!=2||skins.Any(s=>s.sharedMesh==null||s.bones.Any(b=>b==null)))throw new Exception("Missing original skin/bone reference");
        var meshes=filters.Select(f=>f.sharedMesh).Concat(skins.Select(s=>s.sharedMesh)).Distinct().ToArray();
        foreach(var mesh in meshes)
        {
            if(mesh.vertexCount==0||mesh.uv.Length!=mesh.vertexCount)throw new Exception("Missing vertex/UV data: "+mesh.name);
            for(int s=0;s<mesh.subMeshCount;s++)if(mesh.GetIndices(s).Any(i=>i<0||i>=mesh.vertexCount))throw new Exception("Invalid mesh indices: "+mesh.name);
        }
        var renderers=board.GetComponentsInChildren<Renderer>(true);
        foreach(var renderer in renderers.Where(r=>r.enabled&&r.gameObject.activeInHierarchy))foreach(var material in renderer.sharedMaterials)
            if(material==null||material.shader==null||!material.shader.isSupported||material.mainTexture==null)throw new Exception("Unresolved rendered material on "+renderer.name);
        var camera=Camera.main;
        if(camera==null||Mathf.Abs(camera.fieldOfView-34.870445f)>.001f)throw new Exception("Native camera FOV changed");
        if(UnityEditor.EditorBuildSettings.scenes.Any(s=>s.path.Contains("RecoveredOrgrimmarProof")))throw new Exception("Proof must stay outside build scenes");
        var clips=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/_Project/RecoveredOrgrimmar"});
        if(clips.Length!=14)throw new Exception("Original clips missing");
        var pipeline=new SerializedObject(GraphicsSettings.currentRenderPipeline);
        int defaultIndex=pipeline.FindProperty("m_DefaultRendererIndex").intValue;
        var defaultRenderer=pipeline.FindProperty("m_RendererDataList").GetArrayElementAtIndex(defaultIndex).objectReferenceValue;
        if(defaultIndex!=0||defaultRenderer.GetType().Name!="Renderer2DData")throw new Exception("Existing default UI renderer changed");
        if(camera.GetUniversalAdditionalCameraData().scriptableRenderer.GetType().Name!="UniversalRenderer")throw new Exception("Proof must use its own 3D renderer");
        return "PASS:121 original transforms;35 static mesh bindings+2 valid skinned bindings;"+meshes.Length+" distinct nonempty meshes with UVs/valid indices;14 clips;all rendered materials/textures resolved;native camera;no game scripts,Canvas or build-scene change.";
    }
}
