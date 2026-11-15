// Isolated inspection scene. Never changes the playable table or its build settings.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateRecoveredBoardProof
{
    const string Root = "Assets/_Project/RecoveredOrgrimmar";
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Current scene has unsaved changes. Do not overwrite them.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=true;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        var materials=new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Material",new[]{Root}))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            var serialized=new SerializedObject(material);var pairs=serialized.FindProperty("m_SavedProperties.m_TexEnvs");
            Texture texture=null;Vector2 scale=Vector2.one,offset=Vector2.zero;
            foreach(string property in new[]{"_MainTex","_BotTex","_TopTex"})
            {
                for(int i=0;i<pairs.arraySize;i++)
                {
                    var pair=pairs.GetArrayElementAtIndex(i);
                    if(pair.FindPropertyRelative("first").stringValue!=property)continue;
                    var value=pair.FindPropertyRelative("second");
                    var candidate=(Texture)value.FindPropertyRelative("m_Texture").objectReferenceValue;
                    if(candidate==null)continue;
                    texture=candidate;scale=value.FindPropertyRelative("m_Scale").vector2Value;offset=value.FindPropertyRelative("m_Offset").vector2Value;break;
                }
                if(texture!=null)break;
            }
            if(texture==null)throw new InvalidOperationException("No resolved board texture: "+material.name);
            material.shader=Shader.Find(material.name=="ORG_ZeppNetting"?"Unlit/Transparent":"Unlit/Texture");
            material.mainTexture=texture;material.mainTextureScale=scale;material.mainTextureOffset=offset;
            EditorUtility.SetDirty(material);materials.Add(material.name+" -> "+texture.name);
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/AssetBundles/initial_base_qualityhigh_global-b6b43e63-prefab-0.unity3d/ORG.prefab");
        if(prefab==null)throw new InvalidOperationException("Recovered ORG prefab did not import.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var board=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);board.name="Recovered original Orgrimmar";
        var renderers=board.GetComponentsInChildren<Renderer>(true);
        var visible=renderers.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.name!="TableTop").ToArray();
        if(visible.Length==0)throw new InvalidOperationException("No visible recovered geometry.");
        Bounds bounds=visible[0].bounds;foreach(var r in visible.Skip(1))bounds.Encapsulate(r.bounds);
        var camera=new GameObject("Recovered Board Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.025f,.015f);
        // Native Main Camera recovered from BoardCameras.prefab in the same client export.
        camera.orthographic=false;camera.aspect=16f/9f;camera.fieldOfView=34.870445f;camera.nearClipPlane=1;camera.farClipPlane=30;
        camera.transform.position=new Vector3(-7.795346f,19.597504f,-5.965445f);
        camera.transform.rotation=new Quaternion(.68707883f,0,0,.7265829f);
        // No Canvas, board plate, gameplay bootstrapper, or imported game scripts in this scene.
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/RecoveredOrgrimmarProof.unity");AssetDatabase.SaveAssets();
        Selection.activeGameObject=board;
        if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.FrameSelected();
        return "Original ORG proof: "+board.GetComponentsInChildren<Transform>(true).Length+" transforms, "+renderers.Length+" mesh/skinned renderers; materials: "+string.Join("; ",materials)+"; bounds="+bounds;
    }
}
