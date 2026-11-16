using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HearthstoneClone.Cards;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InstallNativeCards
{
    const string Art="Assets/_Project/Art/NativeCards";
    const string Table="Assets/_Project/Prefabs/HearthstoneTable/TableCard.prefab";
    static readonly Vector2 Center=new Vector2(-.055f,.041f);
    const float Scale=180f/2.17f;
    static NativeCardLibrary library;
    static Material opaquePaint;
    static readonly List<NativeCardText> labels=new List<NativeCardText>();
    public static string Main()
    {
        if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
        Directory.CreateDirectory(Art+"/Names");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        // Scene changes can unload unreferenced assets; load every source afterwards.
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/RecoveredInterfaceProof.unity");
        opaquePaint=AssetDatabase.LoadAssetAtPath<Material>(Art+"/OpaquePaint.mat");
        if(opaquePaint==null){opaquePaint=new Material(Shader.Find("HearthClone/UI/Native Card Paint"));AssetDatabase.CreateAsset(opaquePaint,Art+"/OpaquePaint.mat");}
        var minion=scene.GetRootGameObjects().Single(g=>g.name=="Native minion — Wolf Rider");
        var spell=scene.GetRootGameObjects().Single(g=>g.name=="Native spell — Fireball");
        library=AssetDatabase.LoadAssetAtPath<NativeCardLibrary>(Art+"/NativeCards.asset");
        if(library==null){library=ScriptableObject.CreateInstance<NativeCardLibrary>();AssetDatabase.CreateAsset(library,Art+"/NativeCards.asset");}
        library.minion=Geometry(minion,false);library.spell=Geometry(spell,true);
        var belwe=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/RecoveredInterface/Proof/Belwe.asset");
        var cards=AssetDatabase.FindAssets("t:CardData",new[]{"Assets/_Project"}).Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<CardData>).Where(c=>c!=null).ToArray();
        library.names=cards.Select(card=>new NativeCardName{card=card,texture=NativeCardTextBaker.Bake(Art+"/Names",card.name+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(card)).Substring(0,8),card.cardName,belwe,Color.white,true)}).ToArray();
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        var prefab=PrefabUtility.LoadPrefabContents(Table);
        try
        {
            var view=prefab.GetComponent<CardView>();
            var old=prefab.GetComponent<NativeCardFace>();if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            var oldRoot=view.faceUpDetails.transform.Find("Native card visuals");if(oldRoot!=null)UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);
            var presenter=prefab.AddComponent<NativeCardFace>();presenter.library=library;view.nativeFace=presenter;
            presenter.legacyGraphics=view.faceUpDetails.GetComponentsInChildren<Graphic>(true);
            var root=Rect("Native card visuals",view.faceUpDetails.transform,Vector2.zero,new Vector2(180,270));
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.sizeDelta=Vector2.zero;
            labels.Clear();
            presenter.minionFace=Face(root,minion,false);presenter.spellFace=Face(root,spell,true);
            presenter.labels=labels.ToArray();
            var nameShape=library.minion.First(m=>m.role==NativeCardRole.Name);
            var nameCenter=new Vector2(nameShape.points.Average(p=>p.x),nameShape.points.Average(p=>p.y));
            var fallback=Rect("Future card name fallback",root,nameCenter,new Vector2(150,24)).gameObject.AddComponent<TextMeshProUGUI>();
            fallback.font=belwe;fallback.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/RecoveredInterface/Proof/BelweOutlined.mat");
            fallback.fontSize=18;fallback.enableAutoSizing=true;fallback.fontSizeMin=10;fallback.fontSizeMax=18;
            fallback.alignment=TextAlignmentOptions.Center;fallback.raycastTarget=false;presenter.fallbackName=fallback;
            presenter.minionFace.SetActive(false);presenter.spellFace.SetActive(false);fallback.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(prefab,Table);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/HearthstoneTable.unity");
        return "Installed native mesh-layer card faces and source typography on TableCard. Existing hand/drag/mulligan/collection code and face-down backs retained. Names="+library.names.Length;
    }
    static Vector2 Point(GameObject actor,Vector3 world)
    {var p=world-actor.transform.position;return (new Vector2(p.x,p.z)-Center)*Scale;}
    static NativeCardMesh[] Geometry(GameObject actor,bool spell)
    {
        var list=new List<(int order,NativeCardMesh mesh)>();
        foreach(var renderer in actor.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled))
        {
            var filter=renderer.GetComponent<MeshFilter>();
            if(filter==null||filter.sharedMesh==null||!AssetDatabase.GetAssetPath(filter.sharedMesh).Contains("/AssetBundles/"))continue;
            var mesh=filter.sharedMesh;var world=mesh.vertices.Select(filter.transform.TransformPoint).ToArray();
            var source=AnimationUtility.CalculateTransformPath(filter.transform,actor.transform);
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                if(sub>=renderer.sharedMaterials.Length||renderer.sharedMaterials[sub]==null)continue;
                var material=renderer.sharedMaterials[sub];var indices=mesh.GetIndices(sub);
                var triangles=new List<(float height,int a,int b,int c)>();
                for(int i=0;i<indices.Length;i+=3)
                {
                    int a=indices[i],b=indices[i+1],c=indices[i+2];
                    // Geometric winding selects visible top surfaces; do not use atlas alpha as a cutout.
                    if(Vector3.Cross(world[b]-world[a],world[c]-world[a]).y<=.00000001f)continue;
                    triangles.Add(((world[a].y+world[b].y+world[c].y)/3,a,b,c));
                }
                if(triangles.Count==0)continue;
                NativeCardRole role=NativeCardRole.Paint;int order=0;
                if(renderer.name=="PortraitFrame_mesh")
                {role=sub==(spell?1:0)?NativeCardRole.Artwork:NativeCardRole.Paint;order=role==NativeCardRole.Artwork?10:12;}
                else if(renderer.name=="Description_mesh")order=20;
                else if(renderer.name=="NameBanner_mesh")order=30;
                else if(renderer.name.StartsWith("Gem_"))order=40;
                else if(renderer.name=="Card_InHand_Ally_CurvedText"){role=NativeCardRole.Name;order=50;}
                var uv=mesh.uv;
                if(role==NativeCardRole.Paint)uv=uv.Select(p=>Vector2.Scale(p,material.mainTextureScale)+material.mainTextureOffset).ToArray();
                list.Add((order,new NativeCardMesh{source=source+" ["+sub+"]",role=role,points=world.Select(p=>Point(actor,p)).ToArray(),
                    uv=uv,triangles=triangles.OrderBy(t=>t.height).SelectMany(t=>new[]{t.a,t.b,t.c}).ToArray(),paint=material.mainTexture}));
            }
        }
        return list.OrderBy(p=>p.order).Select(p=>p.mesh).ToArray();
    }
    static GameObject Face(Transform parent,GameObject actor,bool spell)
    {
        var root=Rect(spell?"Native spell":"Native minion",parent,Vector2.zero,new Vector2(180,270));
        var recipe=spell?library.spell:library.minion;
        for(int i=0;i<recipe.Length;i++)
        {
            var rect=Rect(recipe[i].source,root,Vector2.zero,new Vector2(180,270));
            rect.gameObject.AddComponent<CanvasRenderer>();
            var graphic=rect.gameObject.AddComponent<NativeCardLayer>();graphic.library=library;graphic.spell=spell;graphic.layerIndex=i;
            if(recipe[i].role!=NativeCardRole.Name)graphic.material=opaquePaint;
            graphic.raycastTarget=false;
        }
        foreach(var original in actor.GetComponentsInChildren<TMP_Text>())
        {
            NativeCardRole role=NativeCardRole.Description;
            string path=AnimationUtility.CalculateTransformPath(original.transform,actor.transform);
            if(path.Contains("CostUberText"))role=NativeCardRole.Cost;
            else if(path.Contains("AttackUberText"))role=NativeCardRole.Attack;
            else if(path.Contains("HealthUberText"))role=NativeCardRole.Health;
            else if(path.Contains("RaceUberText"))role=NativeCardRole.School;
            else if(!path.Contains("PowersUberText"))continue;
            var x=original.transform.TransformVector(Vector3.right);var y=original.transform.TransformVector(Vector3.up);
            float sx=new Vector2(x.x,x.z).magnitude,sy=new Vector2(y.x,y.z).magnitude;
            var size=Vector2.Scale(original.rectTransform.rect.size,new Vector2(sx,sy))*Scale;
            var rect=Rect(role.ToString(),root,Point(actor,original.transform.position),size);
            var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font=original.font;text.fontSharedMaterial=original.fontSharedMaterial;text.color=original.color;
            text.alignment=TextAlignmentOptions.Center;text.richText=true;text.raycastTarget=false;
            text.enableAutoSizing=true;text.fontSizeMin=.5f*.1f*Scale*sy;text.fontSizeMax=original.fontSizeMax*.1f*Scale*sy;
            text.fontSize=text.fontSizeMax;text.textWrappingMode=role==NativeCardRole.Description?TextWrappingModes.Normal:TextWrappingModes.NoWrap;
            text.lineSpacing=original.lineSpacing;labels.Add(new NativeCardText{text=text,role=role,ink=original.color});
        }
        return root.gameObject;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
}
