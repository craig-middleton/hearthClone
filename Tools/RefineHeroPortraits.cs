using System;
using System.Linq;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Visual-only authoring pass. Coordinates are measured from the 1672x941 board plates.
// The existing paintings, FaceView bindings, buttons and rules are retained.
public static class RefineHeroPortraits
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before fitting portraits.");
        if(EditorSceneManager.GetActiveScene().path!="Assets/Scenes/HearthstoneTable.unity")throw new InvalidOperationException("Open HearthstoneTable first.");
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        var top=new[]{P(836,69),P(857,75),P(866,80),P(880,90),P(890,100),P(897,110),P(903,120),P(907,130),P(910,140),P(911,150),P(910,225),P(761,225),P(761,150),P(762,140),P(764,130),P(768,120),P(774,110),P(782,100),P(792,90),P(806,80),P(816,75)};
        var bottom=new[]{P(836,648),P(849,650),P(859,655),P(868,660),P(881,670),P(891,680),P(899,690),P(904,700),P(909,710),P(912,720),P(914,740),P(914,812),P(758,812),P(757,740),P(759,720),P(763,710),P(767,700),P(772,690),P(780,680),P(790,670),P(804,660),P(812,655),P(822,650)};
        Fit(e.opponentFaceView,top);
        Fit(e.faceView,bottom);
        foreach(var root in e.gameObject.scene.GetRootGameObjects())
            foreach(var component in root.GetComponentsInChildren<Component>(true))
                if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Fitted existing hero paintings to the measured pointed openings, with individually sized seats and aligned health badges.";
    }
    static Vector2 P(float x,float y)=>new Vector2(x,y);
    static Vector2 Canvas(Vector2 source)=>new Vector2((source.x/1672f-.5f)*1920f,(.5f-source.y/941f)*1080f);
    static void Fit(FaceView face,Vector2[] sourceOutline)
    {
        float left=sourceOutline.Min(p=>p.x),right=sourceOutline.Max(p=>p.x),top=sourceOutline.Min(p=>p.y),bottom=sourceOutline.Max(p=>p.y);
        var sourceSize=new Vector2(right-left,bottom-top);
        var size=new Vector2(sourceSize.x/1672f*1920,sourceSize.y/941f*1080);
        var position=Canvas(new Vector2((left+right)*.5f,(top+bottom)*.5f));
        var root=(RectTransform)face.transform;root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);
        root.anchoredPosition=position;root.sizeDelta=new Vector2(Mathf.Max(174,size.x),212);root.localScale=Vector3.one;
        var mask=(RectTransform)face.avatarImage.transform.parent;mask.anchorMin=mask.anchorMax=mask.pivot=new Vector2(.5f,.5f);
        mask.anchoredPosition=Vector2.zero;mask.sizeDelta=size;
        var shape=mask.GetComponent<PortraitSilhouette>();
        shape.outline=sourceOutline.Select(p=>new Vector2((p.x-left)/sourceSize.x,(bottom-p.y)/sourceSize.y)).ToArray();
        shape.raycastTarget=false;shape.SetVerticesDirty();
        var image=face.avatarImage;image.enabled=true;image.color=Color.white;image.raycastTarget=false;
        var fit=image.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio=image.sprite.rect.width/image.sprite.rect.height;
        face.breathScaleAmount=0;face.swayAmount=0;
        var rim=root.Find("HeroArch");if(rim!=null)rim.gameObject.SetActive(false);
        var hit=face.GetComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
        Health(face,position+new Vector2(size.x*.5f-4,-size.y*.5f+8));
        EditorUtility.SetDirty(face);EditorUtility.SetDirty(shape);
    }
    static void Health(FaceView face,Vector2 center)
    {
        var properties=new SerializedObject(face);
        var image=(Image)properties.FindProperty("healthGemImage").objectReferenceValue;
        var number=(TMP_Text)properties.FindProperty("healthNumberText").objectReferenceValue;
        var layer=GameObject.Find("HeroHealthOverlay").transform;
        string name=face.name+" HealthClip";
        var existing=layer.Find(name);
        RectTransform clip;
        if(existing==null)
        {
            clip=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(PortraitSilhouette),typeof(Mask)).GetComponent<RectTransform>();
            clip.SetParent(layer,false);
        }
        else clip=(RectTransform)existing;
        clip.anchorMin=clip.anchorMax=clip.pivot=new Vector2(.5f,.5f);clip.anchoredPosition=center;clip.sizeDelta=new Vector2(46,64);
        var shape=clip.GetComponent<PortraitSilhouette>();shape.raycastTarget=false;
        // The original texture has unrelated UV strips to the left; stencil only its teardrop.
        var contour=new[]{P(65,8),P(82,36),P(96,65),P(102,85),P(97,105),P(82,118),P(64,123),P(44,117),P(30,104),P(25,88),P(30,68),P(44,41)};
        shape.outline=contour.Select(p=>new Vector2((p.x-20)/86f,(124-p.y)/122f)).ToArray();shape.SetVerticesDirty();
        clip.GetComponent<Mask>().showMaskGraphic=false;
        // Keep digits outside the stencil: the outlined number may extend past the gem edge.
        number.transform.SetParent(layer,false);
        var textRect=number.rectTransform;textRect.anchorMin=textRect.anchorMax=textRect.pivot=new Vector2(.5f,.5f);
        textRect.anchoredPosition=center+new Vector2(0,-5);textRect.sizeDelta=new Vector2(74,55);
        number.fontSize=32;number.alignment=TextAlignmentOptions.Center;number.raycastTarget=false;
        image.transform.SetParent(clip,false);
        var rect=image.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        image.material=null;image.raycastTarget=false;image.color=Color.white;
    }
}
