using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RefineRecoveredBoardLighting
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode only");
        if(EditorSceneManager.GetActiveScene().path!="Assets/Scenes/RecoveredOrgrimmarProof.unity")throw new Exception("Proof scene only");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        const string rendererPath="Assets/Settings/RecoveredBoard3DRenderer.asset";
        var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if(data==null){data=ScriptableObject.CreateInstance<UniversalRendererData>();data.name="RecoveredBoard3DRenderer";AssetDatabase.CreateAsset(data,rendererPath);}
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if(pipeline==null)throw new Exception("The project pipeline changed; inspect before authoring");
        var settings=new SerializedObject(pipeline);var list=settings.FindProperty("m_RendererDataList");int index=-1;
        for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==data)index=i;
        if(index<0){index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=data;}
        settings.FindProperty("m_SoftShadowsSupported").boolValue=true;settings.ApplyModifiedPropertiesWithoutUndo();
        Camera.main.GetUniversalAdditionalCameraData().SetRenderer(index);
        var shader=Shader.Find("HearthClone/Board/Recovered Paint With Shadows");
        if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Proof shadow shader failed to compile");
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/_Project/RecoveredOrgrimmar"}))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if(material.name=="ORG_ZeppNetting")continue;
            material.shader=shader;material.SetColor("_ShadowTint",new Color(.235f,.18135f,.098f));material.SetFloat("_ShadowStrength",.45f);EditorUtility.SetDirty(material);
        }
        var board=GameObject.Find("Recovered original Orgrimmar");var holder=board.transform.Find("Directional light Main");
        if(holder==null)throw new Exception("Native light transform missing");
        var light=holder.GetComponent<Light>();if(light==null)light=holder.gameObject.AddComponent<Light>();
        light.type=LightType.Directional;light.color=Color.white;light.intensity=.275f;light.shadows=LightShadows.Soft;
        light.shadowStrength=.63479054f;light.shadowBias=.005f;light.shadowNormalBias=.03f;light.shadowCustomResolution=4096;light.cullingMask=1;
        foreach(var renderer in board.GetComponentsInChildren<Renderer>(true))
        {
            renderer.receiveShadows=true;
            renderer.shadowCastingMode=renderer.name=="TableTop"||renderer.name.StartsWith("PlaySurface")?ShadowCastingMode.Off:ShadowCastingMode.On;
        }
        Camera.main.renderingPath=RenderingPath.Forward;
        EditorSceneManager.MarkSceneDirty(board.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Applied URP paint/shadows with source light transform. Dedicated 3D renderer index="+index+"; existing default 2D renderer retained. Global quality settings unchanged: "+QualitySettings.shadows+"/"+QualitySettings.shadowDistance;
    }
}
