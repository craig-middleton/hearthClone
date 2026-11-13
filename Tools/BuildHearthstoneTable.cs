// Executed inside the running Unity Editor through script run; kept outside Assets to avoid reloads.
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using HearthstoneClone.UI;
using HearthstoneClone.Cards;

public static class BuildHearthstoneTable
{
    const string Art = "Assets/_Project/Art/HearthstoneTable";
    const string Prefabs = "Assets/_Project/Prefabs/HearthstoneTable";
    static readonly string Source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Hearthstone Export", "Texture2D");
    static Material dark, white, gray, healthMat, blueGem;
    static Sprite frame, minionFrame, back, health, attack, mana, banner, cardFrame, table;
    static TMP_FontAsset font;
    static readonly Color Ink = new Color(.18f,.12f,.08f);

    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building the styled scene.");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before building.");
        Directory.CreateDirectory(Art); Directory.CreateDirectory(Prefabs);
        foreach (string n in new [] { "GenFX_ally_taunt_d.png", "Card_InPlay_AllyFrame_Frozen.png", "Card_Inhand_Ability_Mage.png", "Card_Inhand_Ability_Neutral.png", "HS10-090.png", "HS12-254.png", "HS16-018.png", "HS14-306.png", "HS11-027.png", "HS23-054_M_Horse.png", "HS16-003.png", "HS13-024.png", "HS14-233.png", "HS16-006.png", "HS14-321.png", "HS6-049.png", "HS11-046.png", "HS14-231.png", "HS8-075.png", "HS14-238.png", "HS6-002.png", "TableTop_NomisKitchen.png", "Card_Inhand_Minion_Mage.png", "GenFX_Card_InPlay_AllyFrame.png", "HeroFrame_Silver.png", "Card_Back_Default_D.png", "Board_EndTurnButton.png", "Gem_Attack_D_Fake.png", "Gem_Health_D_Fake.png", "Gem_Mana_Large_phone.png", "DT_Mage_Jaina.png", "DT_Warrior_Garrosh.png", "goblin_blastmage_D.png", "HS12-223_S_Bear.png", "murloc.png", "Coin_Mana.png" })
        {
            string dest=Art+"/"+n;
            if (!File.Exists(dest)) File.Copy(Path.Combine(Source,n), dest);
        }
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Art,"*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType==TextureImporterType.Sprite && importer.spriteImportMode==SpriteImportMode.Single && !importer.mipmapEnabled && (!path.EndsWith("Gem_Mana_Large_phone.png") || importer.alphaSource==TextureImporterAlphaSource.None)) continue;
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            if(path.EndsWith("Gem_Mana_Large_phone.png")) importer.alphaSource=TextureImporterAlphaSource.None;
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        dark=Material("DarkAtlas",new Color(35f/255,36f/255,35f/255),.04f);
        white=Material("WhiteAtlas",Color.white,.04f);
        gray=Material("GrayAtlas",new Color(.502f,.502f,.502f),.045f);
        blueGem=Material("BlueGem",Color.black,.025f);
        healthMat=Material("HealthAtlas",new Color(41f/255,40f/255,33f/255),.04f);
        frame=Slice("HeroFrame_Silver.png","HeroArch",48,18,418,466);
        minionFrame=Slice("GenFX_Card_InPlay_AllyFrame.png","MinionOval",64,0,399,510);
        back=Slice("Card_Back_Default_D.png","ClassicBack",97,0,328,512);
        health=Slice("Gem_Health_D_Fake.png","HealthGem",20,2,86,122);
        attack=Slice("Gem_Attack_D_Fake.png","AttackGem",0,0,128,128);
        mana=Slice("Gem_Mana_Large_phone.png","ManaHexagon",0,0,151,156);
        banner=Slice("Card_Inhand_Minion_Mage.png","NameScroll",877,57,130,565);
        cardFrame=Slice("Card_Inhand_Minion_Mage.png","MinionCard",170,0,680,1024);
        table=Slice("TableTop_NomisKitchen.png","Table",32,142,960,580);
        font=MakeFont();
        GameObject cardPrefab=MakeCard();
        GameObject minionPrefab=MakeMinion();
        EditorSceneManager.OpenScene("Assets/Scenes/TestScene.unity");
        var scene=EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/HearthstoneTable.unity",true);
        EditorSceneManager.OpenScene("Assets/Scenes/HearthstoneTable.unity");
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        var canvas=GameObject.Find("GameCanvas").GetComponent<Canvas>();
        var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var bg=e.boardBackgroundImage;
        bg.sprite=table; bg.color=Color.white; bg.material=null; bg.raycastTarget=false;
        Stretch(bg.rectTransform); bg.transform.SetAsFirstSibling();
        e.boardBackgrounds=new System.Collections.Generic.List<Sprite>{table};
        StyleCards(e);
        e.handDisplay.cardViewPrefab=cardPrefab; e.opponentHandDisplay.cardViewPrefab=cardPrefab; e.cardViewPrefab=cardPrefab;
        e.boardDisplay.minionViewPrefab=minionPrefab; e.opponentBoardDisplay.minionViewPrefab=minionPrefab;
        Panel(e.boardDisplay.boardPanel,0,-80,1060,185,true);
        Panel(e.opponentBoardDisplay.boardPanel,0,120,1060,185,true);
        Hand(e.handDisplay.handPanel,-30,-445,false);
        Hand(e.opponentHandDisplay.handPanel,0,510,true);
        Panel(e.mulliganPanel,0,-40,900,300,false);
        var ml=e.mulliganPanel.GetComponent<HorizontalLayoutGroup>(); ml.spacing=60;
        var overlay=Image("MulliganShade",canvas.transform,null,new Color(0,0,0,.55f),0,0,1920,1080);
        overlay.transform.SetSiblingIndex(e.mulliganPanel.GetSiblingIndex()); overlay.raycastTarget=false;
        // Heading and shade follow the existing panel visibility, including restart.
        var heading=new GameObject("OpeningHandOverlay",typeof(RectTransform)); heading.transform.SetParent(canvas.transform,false);
        overlay.transform.SetParent(heading.transform,false);
        Text("OpeningHand",heading.transform,"STARTING HAND",42,new Color(.95f,.82f,.50f),0,180,700,60,true);
        Text("MulliganHint",heading.transform,"Choose cards to replace",24,new Color(.92f,.89f,.80f),0,135,700,40);
        heading.transform.SetSiblingIndex(e.mulliganPanel.GetSiblingIndex());
        StyleFace(e.faceView,-5,-240,Load("DT_Mage_Jaina.png"));
        StyleFace(e.opponentFaceView,-5,340,Load("DT_Warrior_Garrosh.png"));
        StyleButton(e.endTurnButton,550,0,160,72,"END TURN",24,Slice("Board_EndTurnButton.png","EndTurn",8,102,244,137),Material("ButtonAtlas",new Color(.26f,.22f,.14f),.07f));
        StyleButton(e.heroPowerButton,190,-310,96,106,"",22,Load("Coin_Mana.png"),null);
        Text("PowerGlyph",e.heroPowerButton.transform,"",47,new Color(.15f,.5f,1),0,2,65,65,true);
        Portrait(e.heroPowerButton.transform,Load("HS14-321.png"),0,5,60,63);
        var powerCost=Image("PowerCost",e.heroPowerButton.transform,mana,Color.white,0,-46,43,43,blueGem);
        Text("Cost",powerCost.transform,"2",27,Color.white,0,0,40,40,true);
        
        StyleButton(e.confirmMulliganButton,0,-235,230,70,"CONFIRM",27,Slice("Board_EndTurnButton.png","Confirm",8,102,244,137),Material("ButtonAtlas",new Color(.26f,.22f,.14f),.07f));
        StyleButton(e.playAgainButton,0,-130,240,70,"PLAY AGAIN",25,Slice("Board_EndTurnButton.png","Confirm",8,102,244,137),Material("ButtonAtlas",new Color(.26f,.22f,.14f),.07f));
        Position(e.gameOverText.rectTransform,0,0,900,150); e.gameOverText.fontSize=66; e.gameOverText.color=new Color(1,.85f,.4f); e.gameOverText.alignment=TextAlignmentOptions.Center;
        var deck=GameObject.Find("DeckPile");
        deck.transform.SetParent(canvas.transform,false); Position((RectTransform)deck.transform,700,-135,85,130);
        foreach(var img in deck.GetComponentsInChildren<Image>(true)) {img.sprite=back;img.material=null;Position(img.rectTransform,img.rectTransform.anchoredPosition.x,img.rectTransform.anchoredPosition.y,85,130);}
        var deckText=deck.GetComponentInChildren<TMP_Text>(true); Position(deckText.rectTransform,0,-84,100,36); deckText.fontSize=25; deckText.color=new Color(.96f,.85f,.62f); Outline(deckText);
        var row=GameObject.Find("ManaCrystalRow"); row.transform.SetParent(canvas.transform,false); Position((RectTransform)row.transform,630,-485,430,52);
        var rowLayout=row.GetComponent<HorizontalLayoutGroup>(); rowLayout.spacing=-3; rowLayout.childControlWidth=false;rowLayout.childControlHeight=false;rowLayout.childForceExpandWidth=false;rowLayout.childForceExpandHeight=false;rowLayout.childAlignment=TextAnchor.MiddleRight;
        var tray=Image("ManaTray",canvas.transform,null,new Color(.035f,.08f,.15f,.85f),600,-485,560,58); tray.transform.SetSiblingIndex(row.transform.GetSiblingIndex());
        var manaPrefab=MakeMana();
        var so=new SerializedObject(e.faceView); so.FindProperty("manaCrystalPrefab").objectReferenceValue=manaPrefab;so.FindProperty("manaCrystalFullSprite").objectReferenceValue=mana;so.FindProperty("manaCrystalEmptySprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/mana/ManaCrystalEmpty.png");so.ApplyModifiedPropertiesWithoutUndo();
        e.handDisplay.handPanel.SetAsLastSibling();heading.transform.SetAsLastSibling();e.mulliganPanel.SetAsLastSibling();e.confirmMulliganButton.transform.SetAsLastSibling();
        var hud=canvas.gameObject.AddComponent<MatchPresentation>(); hud.playerFace=e.faceView;hud.opponentFace=e.opponentFaceView;hud.mulliganPanel=e.mulliganPanel.gameObject;hud.mulliganHeading=heading;
        hud.playerMana=Text("PlayerMana",canvas.transform,"0/0",27,Color.white,350,-485,90,48,true);
        hud.opponentMana=Text("OpponentMana",canvas.transform,"0/0",22,new Color(.72f,.86f,1),205,360,90,45,true);
        var surface=Rect("TableSurface",canvas.transform,0,0,1920,1080);
        foreach(var child in canvas.transform.Cast<Transform>().Where(t=>t!=surface).ToArray())child.SetParent(surface,false);
        Camera.main.backgroundColor=new Color(.015f,.012f,.01f);Camera.main.clearFlags=CameraClearFlags.SolidColor;
        foreach(var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var component in go.GetComponentsInChildren<Component>(true))
                if(component!=null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/HearthstoneTable.unity",true),new EditorBuildSettingsScene("Assets/Scenes/TestScene.unity",false)};
        EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        Selection.activeGameObject=canvas.gameObject;
        if(SceneView.lastActiveSceneView!=null) SceneView.lastActiveSceneView.FrameSelected();
        return "Created HearthstoneTable scene and styled prefabs in the running Editor. Gameplay assets and TestScene retained.";
    }
    static void StyleCards(EffectTester e)
    {
        string dir=Art+"/Cards";Directory.CreateDirectory(dir);AssetDatabase.Refresh();
        var names=new System.Collections.Generic.Dictionary<string,string> {
            {"Murloc","HS10-090.png"},{"Goblin","goblin_blastmage_D.png"},{"Wisp","HS12-254.png"},
            {"River Crocodile","HS16-018.png"},{"Watchman","HS14-306.png"},{"Shieldbearer","HS11-027.png"},
            {"Warhorse","HS23-054_M_Horse.png"},{"Bear","HS12-223_S_Bear.png"},{"Boulderfist","HS16-003.png"},
            {"Charging Rhino","HS13-024.png"},{"Stone Guardian","HS14-233.png"},{"Ancient Colossus","HS16-006.png"},
            {"Fireball","HS14-321.png"},{"Arcane Bolt","HS6-049.png"},{"Frostbolt","HS11-046.png"},
            {"Blizzard","HS14-231.png"},{"Verdant Growth","HS8-075.png"},{"Rebirth's Touch","HS14-238.png"},
            {"Mage Pupil","HS6-002.png"},{"The Coin","Coin_Mana.png"}
        };
        var cards=e.cardPool.Concat(new[]{e.coinCard}).Where(c=>c!=null).ToArray();
        var result=new System.Collections.Generic.List<CardData>();
        foreach(var original in cards)
        {
            string path=dir+"/"+original.name+".asset";
            var c=AssetDatabase.LoadAssetAtPath<CardData>(path);
            if(c==null){c=ScriptableObject.CreateInstance<CardData>();AssetDatabase.CreateAsset(c,path);}
            EditorUtility.CopySerialized(original,c);c.name=original.name;
            if(names.TryGetValue(c.cardName,out string art))c.artwork=Load(art);
            if(string.IsNullOrWhiteSpace(c.description))
            {
                if(c.hasTaunt)c.description="<b>Taunt</b>";
                else if(c.onPlayEffect is HearthstoneClone.Effects.FrostDamageEffect frost)c.description=$"Deal {frost.damageAmount} damage. <b>Freeze</b> a minion hit.";
                else if(c.onPlayEffect is HearthstoneClone.Effects.DealDamageEffect dmg)c.description=$"Deal {dmg.damageAmount} damage.";
                else if(c.onPlayEffect is HearthstoneClone.Effects.GainManaEffect gain)c.description=$"Gain {gain.manaAmount} Mana this turn.";
            }
            EditorUtility.SetDirty(c);
            if(original==e.coinCard)e.coinCard=c;else result.Add(c);
        }
        e.cardPool=result;
    }
    static TMP_FontAsset MakeFont()
    {
        const string path=Art+"/TableSerif.asset";
        var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(existing!=null){existing.atlasPopulationMode=AtlasPopulationMode.Dynamic;existing.TryAddCharacters(new string(Enumerable.Range(32,95).Select(i=>(char)i).ToArray()));existing.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(existing);return existing;}
        string source="/usr/share/fonts/truetype/noto/NotoSerif-Bold.ttf";
        string dest=Art+"/NotoSerif-Bold.ttf";
        if(!File.Exists(dest))File.Copy(source,dest);
        if(!File.Exists(Art+"/NotoSerif-LICENSE.txt"))File.Copy("/usr/share/doc/fonts-noto-core/copyright",Art+"/NotoSerif-LICENSE.txt");
        AssetDatabase.ImportAsset(dest);
        var f=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(dest),60,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,false);
        f.name="Table Serif";AssetDatabase.CreateAsset(f,path);
        foreach(var texture in f.atlasTextures){texture.name="Table Serif Atlas";AssetDatabase.AddObjectToAsset(texture,f);}
        f.material.name="Table Serif Material";AssetDatabase.AddObjectToAsset(f.material,f);
        f.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?'-/+()<>=");
        f.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(f);AssetDatabase.SaveAssets();return f;
    }
    static Sprite Load(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/"+n);
    static Sprite Slice(string file,string name,int x,int top,int w,int h)
    {
        string path=Art+"/"+name+".asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+file);
        var s=Sprite.Create(texture,new Rect(x,texture.height-top-h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect); s.name=name;
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(existing!=null){EditorUtility.CopySerialized(s,existing);UnityEngine.Object.DestroyImmediate(s);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(s,path); return s;
    }
    static Material Material(string n,Color key,float threshold)
    {
        string path=Art+"/"+n+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("HearthClone/UI/Atlas Cutout"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_KeyColor",key);m.SetFloat("_Threshold",threshold);EditorUtility.SetDirty(m);return m;
    }
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);
        var r=(RectTransform)g.transform;Position(r,x,y,w,h);return r;
    }
    static void Position(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static Image Image(string n,Transform p,Sprite s,Color c,float x,float y,float w,float h,Material mat=null)
    {var im=Rect(n,p,x,y,w,h).gameObject.AddComponent<Image>();im.sprite=s;im.color=c;im.material=mat;im.raycastTarget=false;return im;}
    static TMP_Text Text(string n,Transform p,string text,float size,Color c,float x,float y,float w,float h,bool outline=false)
    {var t=Rect(n,p,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.color=c;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.fontStyle=FontStyles.Bold;t.textWrappingMode=TextWrappingModes.Normal;if(outline)Outline(t);return t;}
    static void Outline(TMP_Text t){string path=Art+"/OutlinedNumbers.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(t.fontSharedMaterial);m.SetFloat("_OutlineWidth",.22f);m.SetColor("_OutlineColor",new Color(.12f,.07f,.035f));AssetDatabase.CreateAsset(m,path);}m.CopyPropertiesFromMaterial(t.font.material);m.SetFloat("_OutlineWidth",.2f);m.SetColor("_OutlineColor",new Color(.12f,.07f,.035f));EditorUtility.SetDirty(m);t.fontSharedMaterial=m;}
    static Image Portrait(Transform p,Sprite s,float x,float y,float w,float h,bool arch=false)
    {var r=Rect("PortraitMask",p,x,y,w,h);r.gameObject.AddComponent<CanvasRenderer>();r.gameObject.AddComponent<PortraitSilhouette>().arch=arch;r.gameObject.AddComponent<Mask>().showMaskGraphic=false;var im=Image("Artwork",r,s,Color.white,0,0,w,h);var fit=im.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=1;return im;}
    static void Panel(Transform t,float x,float y,float w,float h,bool board)
    {Position((RectTransform)t,x,y,w,h);var im=t.GetComponent<Image>();if(im!=null){im.color=Color.clear;im.raycastTarget=board;}
     var l=t.GetComponent<HorizontalLayoutGroup>();if(l!=null){l.childAlignment=TextAnchor.MiddleCenter;l.childControlWidth=false;l.childControlHeight=false;l.childForceExpandWidth=false;l.childForceExpandHeight=false;l.spacing=board?12:50;}}
    static void Hand(Transform t,float x,float y,bool opponent)
    {Panel(t,x,y,1050,280,false);var l=t.GetComponent<HorizontalLayoutGroup>();if(l!=null)l.enabled=false;var fan=t.GetComponent<HandFanLayout>();if(fan==null)fan=t.gameObject.AddComponent<HandFanLayout>();fan.perCardStep=opponent?110:115;fan.maxTotalWidth=opponent?650:800;fan.arcDrop=opponent?30:48;fan.maxTiltDegrees=opponent?10:13;fan.invertVertical=opponent;fan.liftHeight=135;fan.hoverScale=1.5f;t.localScale=Vector3.one*(opponent?.65f:.8f);}
    static GameObject MakeCard()
    {
        var root=Rect("TableCard",null,0,0,180,270).gameObject;
        var im=root.AddComponent<Image>();im.sprite=cardFrame;im.material=dark;im.raycastTarget=true;
        var b=root.AddComponent<Button>();b.targetGraphic=im;b.transition=Selectable.Transition.None;
        var v=root.AddComponent<CardView>();v.cardBackground=im;v.button=b;v.styledMinionFrame=cardFrame;v.styledSpellFrame=Slice("Card_Inhand_Ability_Mage.png","SpellCard",170,62,680,950);
        var detail=Rect("Face",root.transform,0,0,180,270);v.faceUpDetails=detail.gameObject;
        v.artworkImage=Portrait(detail,Load("goblin_blastmage_D.png"),0,49,113,149);v.portraitMask=v.artworkImage.transform.parent.GetComponent<PortraitSilhouette>();
        Image("Parchment",detail,Slice("Card_Inhand_Ability_Neutral.png","Parchment",245,695,500,230),Color.white,0,-78,146,76);
        var scroll=Image("NameScroll",detail,banner,Color.white,0,-28,34,174,dark);scroll.rectTransform.localRotation=Quaternion.Euler(0,0,90);
        v.nameText=Text("Name",detail,"",17,Ink,0,-27,160,40);v.nameText.enableAutoSizing=true;v.nameText.fontSizeMin=10;v.nameText.fontSizeMax=17;
        v.descriptionText=Text("Description",detail,"",14,Ink,0,-78,143,70);v.descriptionText.fontStyle=FontStyles.Normal;v.descriptionText.enableAutoSizing=true;v.descriptionText.fontSizeMin=10;v.descriptionText.fontSizeMax=14;
        var cost=Image("ManaGem",detail,mana,Color.white,-76,103,56,56,blueGem);v.costText=Text("Cost",cost.transform,"",33,Color.white,0,0,50,50,true);
        var a=Image("AttackGem",detail,attack,Color.white,-72,-113,55,55,gray);v.attackText=Text("Attack",a.transform,"",31,Color.white,4,-3,45,45,true);
        var hp=Image("HealthGem",detail,health,Color.white,73,-113,43,57,healthMat);v.healthText=Text("Health",hp.transform,"",31,Color.white,0,-5,46,45,true);
        v.statsText=Text("LegacyStats",detail,"",1,Color.clear,0,0,1,1);v.statsText.gameObject.SetActive(false);
        var so=new SerializedObject(v);so.FindProperty("cardBackSprite").objectReferenceValue=back;so.ApplyModifiedPropertiesWithoutUndo();
        root.AddComponent<LayoutElement>().preferredWidth=180;root.GetComponent<LayoutElement>().preferredHeight=270;
        var temporaryParent=new GameObject("TemporaryFanParent");temporaryParent.AddComponent<HandFanLayout>();root.transform.SetParent(temporaryParent.transform,false);root.AddComponent<CardHoverFan>();
        var result=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/TableCard.prefab");UnityEngine.Object.DestroyImmediate(temporaryParent);return result;
    }
    static GameObject MakeMinion()
    {
        var root=Rect("TableMinion",null,0,0,132,175).gameObject;
        var im=root.AddComponent<Image>();im.sprite=minionFrame;im.material=dark;im.raycastTarget=true;
        var b=root.AddComponent<Button>();b.targetGraphic=im;b.transition=Selectable.Transition.None;
        var v=root.AddComponent<MinionView>();v.button=b;v.minionBackground=im;
        v.readyGlow=Image("ReadyGlow",root.transform,minionFrame,new Color(.15f,1,.25f),0,0,145,190,dark).gameObject;
        v.tauntVisual=Image("TauntShield",root.transform,Slice("GenFX_ally_taunt_d.png","TauntShield",30,5,195,248),Color.white,0,-2,145,184,Material("TauntAtlas",new Color(123f/255,129f/255,135f/255),.05f)).gameObject;
        v.frozenVisual=Image("FrozenFrame",root.transform,Load("Card_InPlay_AllyFrame_Frozen.png"),Color.white,0,0,165,187,Material("FrozenAtlas",new Color(101f/255,196f/255,231f/255),.04f)).gameObject;
        v.artworkImage=Portrait(root.transform,Load("goblin_blastmage_D.png"),-2,0,103,139);
        // Frame draws over the portrait rim, with its dark interior covered by the oval.
        var a=Image("AttackGem",root.transform,attack,Color.white,-51,-66,50,50,gray);v.attackText=Text("Attack",a.transform,"",31,Color.white,3,-3,45,45,true);
        var hp=Image("HealthGem",root.transform,health,Color.white,49,-66,39,53,healthMat);v.healthText=Text("Health",hp.transform,"",31,Color.white,0,-5,45,45,true);
        v.nameText=Text("Name",root.transform,"",1,Color.clear,0,0,1,1);v.nameText.gameObject.SetActive(false);
        v.statsText=Text("Stats",root.transform,"",1,Color.clear,0,0,1,1);v.statsText.gameObject.SetActive(false);
        v.cannotAttackColor=new Color(.7f,.7f,.7f);v.selectedColor=new Color(.4f,1,.35f);v.frozenColor=new Color(.45f,.85f,1);
        var result=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/TableMinion.prefab");UnityEngine.Object.DestroyImmediate(root);return result;
    }
    static GameObject MakeMana()
    {var root=Rect("TableMana",null,0,0,39,44).gameObject;var im=root.AddComponent<Image>();im.sprite=mana;im.material=blueGem;im.raycastTarget=false;var result=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/TableMana.prefab");UnityEngine.Object.DestroyImmediate(root);return result;}
    static void StyleFace(FaceView f,float x,float y,Sprite portrait)
    {
        Position((RectTransform)f.transform,x,y,174,212); f.breathScaleAmount=.008f;f.swayAmount=.4f;
        foreach(var img in f.GetComponentsInChildren<Image>(true)) if(img==f.avatarImage || img.name.Contains("HealthGem")) img.gameObject.SetActive(false);
        var rootImage=f.GetComponent<Image>();rootImage.color=Color.clear;f.button.transition=Selectable.Transition.None;
        var rim=Image("HeroArch",f.transform,frame,Color.white,0,0,174,212,white);
        f.avatarImage=Portrait(f.transform,portrait,0,0,144,180,true);
        var hp=Image("StyledHealthGem",f.transform,health,Color.white,73,-84,53,72,healthMat);
        var number=Text("Health",hp.transform,"30",37,Color.white,0,-6,65,55,true);
        var so=new SerializedObject(f);so.FindProperty("healthGemImage").objectReferenceValue=hp;so.FindProperty("healthNumberText").objectReferenceValue=number;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void StyleButton(Button b,float x,float y,float w,float h,string title,float size,Sprite sprite,Material material)
    {Position((RectTransform)b.transform,x,y,w,h);var im=b.GetComponent<Image>();im.sprite=sprite;im.material=material;im.color=Color.white;b.transition=Selectable.Transition.ColorTint;var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.95f,.7f);colors.pressedColor=new Color(.7f,.65f,.45f);b.colors=colors;
     foreach(var t in b.GetComponentsInChildren<TMP_Text>(true)){t.font=font;t.text=title;t.fontSize=size;t.color=Ink;t.fontStyle=FontStyles.Bold;t.alignment=TextAlignmentOptions.Center;Stretch(t.rectTransform);}}
}
