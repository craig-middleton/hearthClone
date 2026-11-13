using System;
using System.IO;
using System.Linq;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InstallFrontEnd
{
    const string Art="Assets/_Project/Art/FrontEnd",TableArt="Assets/_Project/Art/HearthstoneTable",Prefabs="Assets/_Project/Prefabs/HearthstoneTable";
    static TMP_FontAsset font; static Material outline; static Sprite button,page,header,row,box;
    static Color ink=new Color(.20f,.12f,.065f);
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        foreach(var name in new[]{"TheBox_Top.png","CollectionManagerFrame_Texture.png","Lettuce_Collections_Page.png"})
        {
            string path=Art+"/"+name;
            if(!File.Exists(path))File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Hearthstone Export","Texture2D",name),path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaSource=TextureImporterAlphaSource.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TableArt+"/TableSerif.asset");outline=AssetDatabase.LoadAssetAtPath<Material>(TableArt+"/OutlinedNumbers.mat");
        button=Slice("TheBox_Top.png","MenuButton",595,780,307,61);
        box=Slice("TheBox_Top.png","Chest",72,6,905,670);
        page=Slice("Lettuce_Collections_Page.png","CollectionPage",0,0,1024,900,new Vector4(75,75,75,75));
        header=Slice("CollectionManagerFrame_Texture.png","CollectionHeader",12,215,937,155,new Vector4(85,45,85,45));row=header;
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();var canvas=GameObject.Find("GameCanvas").transform;
        var old=canvas.Find("FrontEnd");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=Rect("FrontEnd",canvas,0,0,1920,1080).gameObject;
        var layer=root.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=30;root.AddComponent<GraphicRaycaster>();
        var ui=root.AddComponent<FrontEndController>();e.frontEnd=ui;ui.tableSurface=canvas.Find("TableSurface").gameObject;
        var main=Rect("MainMenu",root.transform,0,0,1920,1080);ui.mainMenu=main.gameObject;
        var bg=Img("Table",main,AssetDatabase.LoadAssetAtPath<Sprite>(TableArt+"/Table.asset"),0,0,1920,1080);bg.color=new Color(.31f,.26f,.22f);bg.raycastTarget=true;
        Img("Chest",main,box,-330,30,1060,785);
        Txt("Title",main,"HEARTHCLONE",53,460,265,630,90,new Color(1,.84f,.43f),true);
        Txt("Subtitle",main,"Take your seat at the table",23,460,188,560,45,new Color(.91f,.83f,.66f));
        ui.playButton=Button("PlayGame",main,"PLAY GAME",460,55,380,82,32);
        ui.chooseDeckButton=Button("ChooseDeck",main,"CHOOSE DECK",460,-65,380,82,30);
        ui.menuDeckLabel=Txt("ActiveDeck",main,"Random deck - 30 cards",23,460,-163,530,55,new Color(.95f,.86f,.66f));
        var deck=Rect("DeckScreen",root.transform,0,0,1920,1080);ui.deckScreen=deck.gameObject;
        var paper=Img("CollectionPage",deck,page,0,0,1900,1050);paper.type=Image.Type.Sliced;paper.raycastTarget=true;
        Txt("Heading",deck,"CHOOSE DECK",43,-280,447,1100,75,ink);
        Txt("Rules",deck,"30 cards. Up to 2 copies of each card.",22,-280,389,1050,45,ink);
        var listPaper=Img("DeckPaper",deck,page,662,8,475,950);listPaper.type=Image.Type.Sliced;listPaper.color=new Color(.79f,.7f,.54f);listPaper.pixelsPerUnitMultiplier=2;
        var hdr=Img("DeckHeader",deck,header,662,427,424,68);hdr.type=Image.Type.Sliced;hdr.pixelsPerUnitMultiplier=3;
        Txt("DeckTitle",deck,"YOUR DECK",26,662,427,350,60,ink);
        ui.deckCount=Txt("DeckCount",deck,"0 / 30",34,662,356,300,55,ink);
        var grid=Rect("CollectionGrid",deck,-277,10,1050,710);ui.collectionGrid=grid;
        var layout=grid.gameObject.AddComponent<GridLayoutGroup>();layout.cellSize=new Vector2(245,350);layout.spacing=new Vector2(20,10);layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount=4;layout.childAlignment=TextAnchor.MiddleCenter;
        var scrollRect=Rect("SelectedScroll",deck,662,-15,420,650);ui.selectedScroll=scrollRect.gameObject.AddComponent<ScrollRect>();ui.selectedScroll.horizontal=false;
        var viewport=Rect("Viewport",scrollRect,0,0,420,650);viewport.gameObject.AddComponent<RectMask2D>();
        var content=Rect("Cards",viewport,0,0,420,650);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=new Vector2(0,0);
        var vl=content.gameObject.AddComponent<VerticalLayoutGroup>();vl.spacing=4;vl.childControlWidth=true;vl.childControlHeight=true;vl.childForceExpandHeight=false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        ui.selectedScroll.viewport=viewport;ui.selectedScroll.content=content;ui.selectedList=content;
        ui.deckHint=Txt("Hint",deck,"Click a card to add it. Click a deck row to remove a copy.",20,-277,-365,1040,50,ink);
        ui.previousButton=Button("PreviousPage",deck,"PREVIOUS",-510,-428,190,54,21);
        ui.nextButton=Button("NextPage",deck,"NEXT",-45,-428,190,54,21);
        ui.pageLabel=Txt("Page",deck,"Page 1 / 4",23,-277,-428,235,48,ink);
        ui.backButton=Button("Back",deck,"BACK",-790,-472,180,55,24);
        ui.clearButton=Button("ClearDeck",deck,"CLEAR DECK",662,-382,245,48,20);
        ui.saveButton=Button("SaveDeck",deck,"SAVE DECK",662,-462,330,72,28);
        ui.collectionEntryPrefab=CollectionPrefab();ui.selectedEntryPrefab=RowPrefab();
        var existing=ui.tableSurface.transform.Find("MatchMenuButton");if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
        ui.matchMenuButton=Button("MatchMenuButton",ui.tableSurface.transform,"MENU",-850,-495,140,46,20);
        ((RectTransform)e.handDisplay.handPanel).anchoredPosition=new Vector2(-30,-505);
        ((RectTransform)e.opponentHandDisplay.handPanel).anchoredPosition=new Vector2(0,575);
        main.gameObject.SetActive(true);deck.gameObject.SetActive(false);ui.tableSurface.SetActive(false);
        foreach(var component in e.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
            if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorUtility.SetDirty(e);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Installed native-asset main menu, 30-card deck builder and corrected resting hand positions.";
    }
    static Sprite Slice(string file,string name,float x,float top,float w,float h,Vector4 border=default)
    {
        string path=Art+"/"+name+".asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+file);
        var sprite=Sprite.Create(texture,new Rect(x,texture.height-top-h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);sprite.name=name;
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing==null){AssetDatabase.CreateAsset(sprite,path);return sprite;}
        EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);return existing;
    }
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;
        rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);return rect;
    }
    static Image Img(string name,Transform parent,Sprite sprite,float x,float y,float w,float h)
    {
        var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;
        if(sprite==button)image.gameObject.AddComponent<AtlasContour>().outline=new[]{new Vector2(.075f,.984f),new Vector2(.91f,.984f),new Vector2(.99f,.065f),new Vector2(.006f,.065f)};
        return image;
    }
    static TMP_Text Txt(string name,Transform parent,string value,float size,float x,float y,float w,float h,Color color,bool outlined=false)
    {
        var text=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;if(outlined)text.fontSharedMaterial=outline;
        text.text=value;text.fontSize=size;text.fontStyle=FontStyles.Bold;text.alignment=TextAlignmentOptions.Center;text.color=color;text.raycastTarget=false;return text;
    }
    static Button Button(string name,Transform parent,string title,float x,float y,float w,float h,float size)
    {
        var image=Img(name,parent,button,x,y,w,h);image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;
        var colors=b.colors;colors.highlightedColor=new Color(1,.91f,.66f);colors.pressedColor=new Color(.7f,.58f,.36f);colors.disabledColor=new Color(.5f,.5f,.5f);b.colors=colors;
        Txt("Label",b.transform,title,size,0,0,w-20,h-4,ink);return b;
    }
    static DeckCollectionEntry CollectionPrefab()
    {
        var root=Rect("DeckCollectionEntry",null,0,0,245,350).gameObject;var entry=root.AddComponent<DeckCollectionEntry>();
        var card=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/TableCard.prefab"));card.transform.SetParent(root.transform,false);
        card.transform.localPosition=new Vector3(0,20);card.transform.localScale=Vector3.one*1.1f;entry.view=card.GetComponent<CardView>();
        var badge=Img("CopiesBadge",root.transform,button,0,-145,95,34);entry.copies=Txt("Copies",badge.transform,"0 / 2",22,0,0,95,34,ink);
        var saved=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/DeckCollectionEntry.prefab");UnityEngine.Object.DestroyImmediate(root);return saved.GetComponent<DeckCollectionEntry>();
    }
    static DeckListEntry RowPrefab()
    {
        var root=Rect("DeckListEntry",null,0,0,420,42).gameObject;root.AddComponent<LayoutElement>().preferredHeight=42;var entry=root.AddComponent<DeckListEntry>();
        var image=root.AddComponent<Image>();image.sprite=row;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=3;entry.button=root.AddComponent<Button>();entry.button.targetGraphic=image;
        entry.artwork=Img("Artwork",root.transform,null,-132,0,58,34);
        var gem=Img("CostGem",root.transform,AssetDatabase.LoadAssetAtPath<Sprite>(TableArt+"/ManaHexagon.asset"),-188,0,31,33);
        var contour=gem.gameObject.AddComponent<AtlasContour>();contour.outline=new[]{new Vector2(.28f,.94f),new Vector2(.7f,.94f),new Vector2(.99f,.47f),new Vector2(.7f,.01f),new Vector2(.28f,.01f),new Vector2(.01f,.47f)};
        gem.material=AssetDatabase.LoadAssetAtPath<Material>(TableArt+"/LitMana.mat");entry.mana=Txt("Mana",gem.transform,"0",23,0,0,34,34,Color.white,true);
        entry.cardName=Txt("Name",root.transform,"",20,22,0,225,37,ink);entry.cardName.enableAutoSizing=true;entry.cardName.fontSizeMin=13;entry.cardName.fontSizeMax=20;entry.cardName.textWrappingMode=TextWrappingModes.NoWrap;
        entry.quantity=Txt("Quantity",root.transform,"x1",22,183,0,48,37,ink);
        var saved=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/DeckListEntry.prefab");UnityEngine.Object.DestroyImmediate(root);return saved.GetComponent<DeckListEntry>();
    }
}
