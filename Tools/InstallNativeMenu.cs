using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class InstallNativeMenu
{
    const string Folder="Assets/_Project/Art/NativeMenu/";
    const int MenuLayer=30;
    static NativeMenuSourceConfiguration config;
    static readonly Color Ink=new Color(.25490198f,.1882353f,.09411765f,1);
    static Color ColorValue(Material source,string field,Color fallback)
    {try{return SourceMaterialProperties.Color(source,field);}catch(InvalidOperationException){return fallback;}}
    static float FloatValue(Material source,string field,float fallback)
    {try{return SourceMaterialProperties.Float(source,field);}catch(InvalidOperationException){return fallback;}}
    static Texture Paint(Material source)
    {try{return SourceMaterialProperties.Texture(source,"_MainTex");}catch(InvalidOperationException){return null;}}
    static Material Map(Material source,Renderer renderer,out bool supported)
    {
        string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
        var info=config.materials.SingleOrDefault(m=>m.guid==guid);
        string shader=info?.sourceShader??"";
        bool shadow=shader.Contains("Multiply")&&!shader.Contains("AddMultiply");
        bool transparent=shader.Contains("Transparent");
        bool additive=shader.Contains("Additive")||shader.Contains("AddMultiply")||shader.Contains("Particle");
        // Recovered selection masks and scrolling FX need their original runtime
        // inputs. Keep their geometry in the source archive, omit unsupported
        // placeholder renders instead of inventing motion or opaque quads.
        supported=!shader.Contains("Selection")&&!additive&&!source.name.Contains("GlowEdge")&&!source.name.Contains("Comet")&&source.name!="BlackColorOnly";
        string path=Folder+guid+"_Compatibility.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        var targetShader=Shader.Find(shadow?"HearthClone/Compatibility/Source White Neutral Shadow":"HearthClone/Compatibility/Source Menu Surface");
        if(mat==null){mat=new Material(targetShader);AssetDatabase.CreateAsset(mat,path);}mat.shader=targetShader;
        Texture paint=Paint(source);
        if(paint==null&&!shadow)supported=false;
        if(shadow)
        {
            mat.SetTexture("_MainTex",paint);mat.SetFloat("_Opacity",ColorValue(source,"_Color",Color.white).a);
        }
        else
        {
            mat.SetTexture("_MainTex",paint!=null?paint:Texture2D.whiteTexture);
            var tint=ColorValue(source,"_Color",Color.white);
            if(shader.EndsWith("Unlit_Texture",StringComparison.Ordinal))tint=Color.white;
            mat.SetColor("_Color",tint);
            if(paint!=null)
            {
                mat.SetTextureScale("_MainTex",SourceMaterialProperties.Scale(source,"_MainTex"));
                mat.SetTextureOffset("_MainTex",SourceMaterialProperties.Offset(source,"_MainTex"));
            }
            mat.SetFloat("_SrcBlend",transparent?(float)BlendMode.SrcAlpha:(float)BlendMode.One);
            mat.SetFloat("_DstBlend",transparent?(float)BlendMode.OneMinusSrcAlpha:(float)BlendMode.Zero);
            mat.SetFloat("_ZWrite",transparent?0:1);mat.SetFloat("_UseAlpha",transparent?1:0);
            mat.SetFloat("_Cutoff",transparent?FloatValue(source,"_Cutoff",0):0);
        }
        mat.renderQueue=shadow?2450:transparent?3000:2000;
        EditorUtility.SetDirty(mat);return mat;
    }
    static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
    static void Hide(Transform root,string name)
    {
        foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name==name))t.gameObject.SetActive(false);
    }
    static NativeMenuPresentation.Slot Bind(Button button,Renderer source,string value)
    {
        var image=button.GetComponent<Image>();image.sprite=null;image.color=Color.clear;image.raycastTarget=true;
        var contour=button.GetComponent<AtlasContour>();if(contour!=null)UnityEngine.Object.DestroyImmediate(contour);
        button.transition=Selectable.Transition.None;
        var text=button.GetComponentInChildren<TMP_Text>(true);text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/RecoveredInterface/Proof/Belwe.asset");
        text.fontSharedMaterial=text.font.material;text.text=value;text.color=Ink;text.fontStyle=FontStyles.Normal;text.fontSize=36;text.enableAutoSizing=true;text.fontSizeMin=20;text.fontSizeMax=36;
        text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
        var points=source.GetComponent<MeshFilter>().sharedMesh.vertices;
        if(points.Length==0)throw new Exception("Original menu slot points could not be baked in Edit mode: "+source.name);
        return new NativeMenuPresentation.Slot{source=source,sourcePoints=points,hitbox=(RectTransform)button.transform,label=text};
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Clean Edit mode required.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        config=JsonUtility.FromJson<NativeMenuSourceConfiguration>(File.ReadAllText(Folder+"SourceConfiguration.json"));
        if(config?.materials==null||config.materials.Length==0)throw new Exception("Original menu source material configuration could not be read.");
        var game=UnityEngine.Object.FindObjectsByType<EffectTester>(FindObjectsInactive.Include).Single();var front=game.frontEnd;
        var board=UnityEngine.Object.FindObjectsByType<NativeBoardPresentation>(FindObjectsInactive.Include).Single();
        var previous=front.mainMenu.GetComponent<NativeMenuPresentation>();
        if(previous!=null&&previous.menuRoot!=null)UnityEngine.Object.DestroyImmediate(previous.menuRoot);
        var original=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Source/TheBox.prefab");
        if(original==null)throw new Exception("Run Tools/PrepareNativeMenu.py first.");
        var root=UnityEngine.Object.Instantiate(original);root.name="Original TheBox Menu";root.transform.position=new Vector3(0,0,1200);
        try
        {
        // Preserve the original root scale, every mesh, UV and local transform.
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=MenuLayer;
        foreach(var camera in root.GetComponentsInChildren<Camera>(true))camera.enabled=false;
        var nativeCamera=Find(root.transform,"TheBoxCamera").GetComponent<Camera>();
        nativeCamera.enabled=true;nativeCamera.tag="Untagged";nativeCamera.cullingMask=1<<MenuLayer;nativeCamera.depth=10;
        nativeCamera.clearFlags=CameraClearFlags.SolidColor;nativeCamera.backgroundColor=Color.black;
        var urp=nativeCamera.GetComponent<UniversalAdditionalCameraData>();if(urp==null)urp=nativeCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();urp.SetRenderer(1);urp.renderPostProcessing=false;urp.renderShadows=false;
        foreach(var light in root.GetComponentsInChildren<Light>(true))light.cullingMask=1<<MenuLayer;
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            bool enabled=r.enabled;var originals=r.sharedMaterials;var mapped=new List<Material>();
            foreach(var material in originals)
            {
                if(material==null){enabled=false;mapped.Add(null);continue;}
                mapped.Add(Map(material,r,out bool supported));enabled&=supported;
            }
            r.sharedMaterials=mapped.ToArray();r.enabled=enabled;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
        // PC main-menu state from original TheBox.m_StateInfoList. The spinner
        // loading pose is zero; the authored MainMenu disk rotation is Z=180.
        Find(root.transform,"TheBox_CenterDisk").localRotation=Quaternion.Euler(0,0,180);
        var drawer=Find(root.transform,"TheBox_Drawer");var drawerBone=Find(root.transform,"DrawerOpened");drawer.localPosition=drawerBone.localPosition;
        // Original Startup_Hub State2 sets Logo _Color=(1,1,1,0).
        // Keep this loading-only paint hidden on the interactive hub.
        // Original runtime hides new-player/mobile/download decorations on PC.
        foreach(string name in new[]{"Logo","Mobile_Drawer","NewPlayerModeBanner","BlackFillQuad","BnetBarBg","TheBox_SecondButtonCover","TheBox_ThirdButtonCover","EmptyButtonCover","Popup","PopupShadow_mesh","StoreClosed","Box_Button_Journal","Box_Button_LuckyDraw","QuestLogButton","GameModeButtonPRTB","JournalButton"})Hide(root.transform,name);
        foreach(var child in front.mainMenu.transform.Cast<Transform>().Where(t=>t.name is "Table" or "Chest" or "Title" or "Subtitle"))child.gameObject.SetActive(false);
        var native=previous!=null?previous:front.mainMenu.AddComponent<NativeMenuPresentation>();
        native.menuRoot=root;native.menuCamera=nativeCamera;native.gameplayCamera=board.boardCamera;
        native.drawer=drawer;native.drawerOpenPosition=drawerBone.localPosition;
        native.originalCameraClip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Source/StartOfGame_BoxCameraPerspective.anim");
        native.cameraChannels=AnimationUtility.GetCurveBindings(native.originalCameraClip)
            .Where(b=>b.type==typeof(Transform)&&string.IsNullOrEmpty(b.path)&&(b.propertyName.StartsWith("m_LocalPosition.",StringComparison.Ordinal)||b.propertyName.StartsWith("m_LocalRotation.",StringComparison.Ordinal)))
            .Select(b=>new NativeMenuPresentation.Channel{property=b.propertyName,curve=AnimationUtility.GetEditorCurve(native.originalCameraClip,b)}).ToArray();
        if(native.cameraChannels.Length!=7)throw new Exception("Original camera transform channels incomplete: "+native.cameraChannels.Length);
        var disk=Find(root.transform,"TheBox_CenterDisk_mesh");
        var play=disk.Find("FirstButton/TournamentButton_Visual").GetComponent<Renderer>();
        var choose=disk.Find("SecondButton/SecondButton_Visual/Mesh").GetComponent<Renderer>();
        native.slots=new[]{Bind(front.playButton,play,"Play Game"),Bind(front.chooseDeckButton,choose,"Choose Deck")};
        native.deckLabel=front.menuDeckLabel.rectTransform;front.menuDeckLabel.fontSize=22;front.menuDeckLabel.color=new Color(.91f,.83f,.65f);native.deckLabel.sizeDelta=new Vector2(480,44);
        // Lower source slots are decoration; do not create unavailable game modes.
        foreach(var item in disk.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.Contains("glow",StringComparison.OrdinalIgnoreCase)))item.enabled=false;
        nativeCamera.transform.localPosition=native.restingCameraPosition;nativeCamera.transform.localRotation=native.restingCameraRotation;
        front.mainMenu.SetActive(true);front.deckScreen.SetActive(false);front.tableSurface.SetActive(false);root.SetActive(true);
        native.RefreshLayout();
        EditorUtility.SetDirty(native);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);
        return "Original TheBox native menu installed: source chest/table/disk/button/logo geometry, isolated source camera, original 4.983s camera intro and existing Play/Choose Deck actions projected onto original slots. Compatibility unlit/multiply surfaces; unsupported runtime glow/scrolling FX remain disabled.";
        }
        catch
        {
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            throw;
        }
    }
}
