using System;
using System.Linq;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InstallBoardPlates
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before editing the saved board.");
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();var surface=GameObject.Find("TableSurface").transform;
        var old=surface.Find("BoardThemes");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("BoardThemes",typeof(RectTransform),typeof(RandomBoardTheme));root.transform.SetParent(surface,false);
        var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(1920,1080);root.transform.SetAsFirstSibling();
        var themes=root.GetComponent<RandomBoardTheme>();
        foreach(string name in new[]{"Orgrimmar","Winterveil","HarvestTavern"})
        {
            string path="Assets/_Project/Art/Boards/"+name+".png";AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false;importer.alphaSource=TextureImporterAlphaSource.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(root.transform,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(1920,1080);rect.anchoredPosition=Vector2.zero;
            var im=go.GetComponent<Image>();im.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);im.raycastTarget=false;
            themes.boards.Add(go);
        }
        themes.ShowBoard(0);e.boardThemes=themes;e.boardBackgroundImage.enabled=false;
        e.gameOverText.gameObject.SetActive(false);e.playAgainButton.gameObject.SetActive(false);
        e.mulliganPanel.gameObject.SetActive(false);e.confirmMulliganButton.gameObject.SetActive(false);
        var opening=surface.Find("OpeningHandOverlay");if(opening!=null)opening.gameObject.SetActive(false);
        var burst=e.spellAnimationSequencer.burstRenderer.displayImage;burst.enabled=false;
        var effectCanvas=burst.GetComponent<Canvas>();if(effectCanvas==null)effectCanvas=burst.gameObject.AddComponent<Canvas>();
        effectCanvas.overrideSorting=true;effectCanvas.sortingOrder=5;
        e.boardBackgroundImage.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/HearthstoneTable/Table.asset");
        e.boardBackgrounds=new System.Collections.Generic.List<Sprite>{e.boardBackgroundImage.sprite};
        FitHero(e.faceView,-290);FitHero(e.opponentFaceView,365);
        ((RectTransform)e.endTurnButton.transform).anchoredPosition=new Vector2(695,55);
        ((RectTransform)e.endTurnButton.transform).sizeDelta=new Vector2(180,76);
        var healthLayer=surface.Find("HeroHealthOverlay");
        if(healthLayer==null){var layer=new GameObject("HeroHealthOverlay",typeof(RectTransform));layer.transform.SetParent(surface,false);healthLayer=layer.transform;}
        var hand=surface.Find("HandPanel");healthLayer.SetSiblingIndex(hand.GetSiblingIndex()+1);
        foreach(var face in new[]{e.faceView,e.opponentFaceView})
        {
            var serialized=new SerializedObject(face);
            var health=(Image)serialized.FindProperty("healthGemImage").objectReferenceValue;
            if(health!=null)health.transform.SetParent(healthLayer,true);
        }
        var deck=surface.Find("DeckPile");if(deck!=null){var dr=(RectTransform)deck;dr.anchoredPosition=new Vector2(820,-215);dr.localScale=Vector3.one*.8f;}
        // The earlier raw-UV prototype is no longer referenced. Its source export is untouched.
        if(AssetDatabase.IsValidFolder("Assets/_Project/Art/BoardThemes"))AssetDatabase.DeleteAsset("Assets/_Project/Art/BoardThemes");
        foreach(var component in e.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
            if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorUtility.SetDirty(e);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Installed classic, winter and harvest board plates with live Unity portraits/cards/controls and per-match random selection.";
    }
    static void FitHero(FaceView face,float y)
    {
        face.transform.localScale=Vector3.one;((RectTransform)face.transform).anchoredPosition=new Vector2(-5,y);
        var rim=face.transform.Find("HeroArch");if(rim!=null)rim.gameObject.SetActive(false);
        var mask=face.avatarImage.transform.parent as RectTransform;if(mask!=null)mask.sizeDelta=new Vector2(158,190);
        var im=face.GetComponent<Image>();if(im!=null)im.color=Color.clear;
    }
}
