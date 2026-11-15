using System;
using System.Linq;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InstallNativeMatchControls
{
    const string Root="Assets/_Project/RecoveredInterface";
    static T Load<T>(string name) where T:UnityEngine.Object=>AssetDatabase.FindAssets("t:"+typeof(T).Name,new[]{Root}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).First(x=>x!=null&&x.name==name);
    public static string Main()
    {
        if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/HearthstoneTable.unity");
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        var oldModel=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Native End Turn model");
        var oldButton=e.endTurnButton.GetComponent<NativeTurnButtonPresentation>();if(oldButton!=null)UnityEngine.Object.DestroyImmediate(oldButton);
        if(oldModel!=null)UnityEngine.Object.DestroyImmediate(oldModel);
        var oldArrow=e.targetingRibbon.GetComponent<NativeTargetingPresentation>();
        if(oldArrow!=null)
        {
            foreach(var part in e.targetingRibbon.GetComponentsInChildren<NativeTargetPart>(true))UnityEngine.Object.DestroyImmediate(part.gameObject);
            UnityEngine.Object.DestroyImmediate(oldArrow);
        }
        var proof=EditorSceneManager.OpenScene("Assets/Scenes/RecoveredInterfaceProof.unity",OpenSceneMode.Additive);
        var holder=proof.GetRootGameObjects().Single(g=>g.name=="Native End Turn study");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder,scene);
        EditorSceneManager.CloseScene(proof,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        UnityEngine.Object.DestroyImmediate(holder.GetComponent<NativeInterfaceProofMotion>());
        holder.name="Native End Turn model";
        var mesh=holder.transform.Find("EndTurnButton/ButtonContainer/ButtonMesh");
        var presentation=e.endTurnButton.gameObject.AddComponent<NativeTurnButtonPresentation>();
        presentation.game=e;presentation.model=holder;presentation.animator=mesh.GetComponent<Animation>();
        presentation.paintRenderer=mesh.GetComponent<Renderer>();presentation.greenHighlight=mesh.Find("Highlight_Plane").GetComponent<Renderer>();
        var camera=Camera.main;camera.aspect=16f/9f;camera.fieldOfView=NativeBoardPresentation.NativeVerticalFieldOfView;
        var ray=camera.ViewportPointToRay(new Vector3(.5f+595f/1920,.5f+44f/1080,0));
        var plane=new Plane(Vector3.up,new Vector3(0,2.5f,0));plane.Raycast(ray,out float distance);
        holder.transform.position=ray.GetPoint(distance);holder.transform.localScale=Vector3.one;
        var bounds=presentation.paintRenderer.bounds;
        float min=1,max=0;
        foreach(float x in new[]{bounds.min.x,bounds.max.x})foreach(float y in new[]{bounds.min.y,bounds.max.y})foreach(float z in new[]{bounds.min.z,bounds.max.z})
        {float p=camera.WorldToViewportPoint(new Vector3(x,y,z)).x;min=Mathf.Min(min,p);max=Mathf.Max(max,p);}
        holder.transform.localScale=Vector3.one*(158f/((max-min)*1920));
        camera.ResetAspect();
        e.endTurnButton.GetComponent<Image>().color=Color.clear;
        foreach(Transform child in e.endTurnButton.transform)child.gameObject.SetActive(false);
        holder.SetActive(false);

        var ribbon=e.targetingRibbon;
        var native=ribbon.gameObject.AddComponent<NativeTargetingPresentation>();
        ribbon.nativePresentation=native;
        var arrowTexture=Load<Material>("TargetingArrow-ArrowDashes").mainTexture;
        var ringTexture=Load<Material>("TargetingArrow-ArrowTarget").mainTexture;
        native.dashes=new RectTransform[24];
        for(int i=0;i<24;i++)native.dashes[i]=Part("Native dash "+i,ribbon.transform,Load<Mesh>("TargetArrow_DashMesh"),arrowTexture);
        native.head=Part("Native arrowhead",ribbon.transform,Load<Mesh>("TargetArrow_ArrowMesh"),arrowTexture);
        native.ring=Part("Native target ring",ribbon.transform,Load<Mesh>("TargetArrow_TargetMesh"),ringTexture);
        native.Clear();
        foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
            if(c!=null&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Installed original animated End Turn model and topology/UV-based targeting graphics; gameplay listeners and cancellation preserved.";
    }
    static RectTransform Part(string name,Transform parent,Mesh mesh,Texture texture)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(NativeTargetPart));go.transform.SetParent(parent,false);
        var graphic=go.GetComponent<NativeTargetPart>();graphic.raycastTarget=false;graphic.paint=texture;
        graphic.points=mesh.vertices.Select(v=>new Vector2(v.x,v.z)).ToArray();graphic.uv=mesh.uv;
        var normals=mesh.normals;var t=mesh.triangles;var indices=new System.Collections.Generic.List<int>();
        for(int i=0;i<t.Length;i+=3)if(normals[t[i]].y+normals[t[i+1]].y+normals[t[i+2]].y>0)indices.AddRange(new[]{t[i],t[i+1],t[i+2]});
        graphic.triangles=indices.ToArray();return (RectTransform)go.transform;
    }
}
