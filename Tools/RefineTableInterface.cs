using System;
using System.IO;
using System.Linq;
using HearthstoneClone.UI;
using HearthstoneClone.Cards;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Run in the open Editor after RefineHeroPortraits. This updates existing views only.
public static class RefineTableInterface
{
    const string Art = "Assets/_Project/Art/HearthstoneTable";
    const string Prefabs = "Assets/_Project/Prefabs/HearthstoneTable";
    static readonly string Source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Hearthstone Export", "Texture2D");
    static TMP_FontAsset font;
    static Material outline;
    static Material fullMana, spentMana, softGlow;
    static Sprite mana;

    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before authoring the interface.");
        if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/HearthstoneTable.unity") throw new InvalidOperationException("Open HearthstoneTable first.");
        foreach (string name in new[]{"Hero_Power_Frame.png", "Hero_Power_D.png", "Hero_Power_Silhouette.png", "Mage_power2_D.png", "ManaTray.png", "Card_Inhand_Minion_Rogue.png", "HS12-223_S.png"}) Import(name);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art + "/TableSerif.asset");
        outline = AssetDatabase.LoadAssetAtPath<Material>(Art + "/OutlinedNumbers.mat");
        mana = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/ManaHexagon.asset");
        fullMana = Paint("LitMana",1.65f,1.2f);
        spentMana = Paint("SpentMana",.75f,.06f);
        string glowPath = Art + "/SoftReadyGlow.mat";
        softGlow = AssetDatabase.LoadAssetAtPath<Material>(glowPath);
        if(softGlow==null){softGlow = new Material(Shader.Find("HearthClone/UI/Atlas Glow")); AssetDatabase.CreateAsset(softGlow,glowPath);}
        RefineArtwork();
        RefineCards();
        RefineMinions();
        RefineMana();
        RefinePower();
        var e = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        // A shorter fan leaves the hero and the right-side controls readable.
        var fan = e.handDisplay.handPanel.GetComponent<HandFanLayout>();
        fan.perCardStep = 112; fan.maxTotalWidth = 790; fan.maxTiltDegrees = 12;
        foreach(var display in new[]{e.boardDisplay,e.opponentBoardDisplay})
        {
            var layout = display.boardPanel.GetComponent<HorizontalLayoutGroup>();
            layout.childScaleWidth = layout.childScaleHeight = true;
        }
        foreach (var component in e.gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)))
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        return "Refined existing card faces, in-play frames, native mana tray/crystals and bronze hero power through the running Editor; no card or rule changes.";
    }

    static void Import(string name)
    {
        string path = Art + "/" + name;
        if (!File.Exists(path)) File.Copy(Path.Combine(Source, name), path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.None; importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
    }

    static Sprite Slice(string file, string name, float x, float top, float w, float h)
    {
        string path = Art + "/" + name + ".asset";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/" + file);
        var sprite = Sprite.Create(texture, new Rect(x, texture.height - top - h, w, h), new Vector2(.5f,.5f), 100, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing == null) { AssetDatabase.CreateAsset(sprite, path); return sprite; }
        EditorUtility.CopySerialized(sprite, existing); UnityEngine.Object.DestroyImmediate(sprite); EditorUtility.SetDirty(existing); return existing;
    }

    static Vector2 P(float x, float y) => new Vector2(x,y);
    static Material Paint(string name, float exposure, float saturation)
    {
        string path = Art + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material = new Material(Shader.Find("HearthClone/UI/Atlas Paint")); AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_Exposure",exposure); material.SetFloat("_Saturation",saturation); EditorUtility.SetDirty(material); return material;
    }
    static Vector2[] UV(float left, float top, float w, float h, params Vector2[] points)
        => points.Select(p => new Vector2((p.x-left)/w, 1-(p.y-top)/h)).ToArray();
    static Vector2[] Circle(int count = 64, float inset = .01f)
        => Enumerable.Range(0,count).Select(i => P(.5f+(.5f-inset)*Mathf.Cos(i*Mathf.PI*2/count), .5f+(.5f-inset)*Mathf.Sin(i*Mathf.PI*2/count))).ToArray();
    static readonly Vector2[] GemOutline = UV(0,0,151,156, P(42,9),P(106,9),P(150,82),P(107,154),P(42,154),P(1,83));
    static readonly Vector2[] HealthOutline = UV(20,2,86,122, P(65,8),P(82,36),P(96,65),P(102,85),P(97,105),P(82,118),P(64,123),P(44,117),P(30,104),P(25,88),P(30,68),P(44,41));
    static readonly Vector2[] AttackOutline = UV(0,0,128,128, P(18,1),P(27,0),P(39,12),P(35,22),P(47,33),P(55,30),P(68,29),P(88,34),P(105,47),P(116,67),P(117,85),P(112,99),P(122,119),P(98,115),P(80,122),P(59,119),P(43,109),P(32,94),P(28,74),P(31,54),P(22,48),P(18,54),P(7,43),P(8,32),P(14,27),P(3,17));
    static readonly Vector2[] CardOutline = UV(170,0,680,1024, P(510,8),P(568,18),P(625,48),P(651,70),P(777,70),P(821,77),P(839,101),P(841,978),P(823,1000),P(795,1010),P(215,1010),P(187,995),P(178,970),P(179,151),P(180,111),P(196,83),P(230,71),P(374,71),P(418,39),P(469,15));
    static readonly Vector2[] SpellOutline = UV(170,62,680,950, P(226,72),P(792,72),P(824,84),P(839,110),P(839,971),P(821,997),P(789,1010),P(225,1010),P(192,997),P(177,972),P(177,110),P(191,85));
    static readonly Vector2[] BackOutline = new[]{P(.06f,.01f),P(.94f,.01f),P(.99f,.04f),P(.99f,.96f),P(.94f,.99f),P(.06f,.99f),P(.01f,.96f),P(.01f,.04f)};

    static void Contour(Image image, Vector2[] points)
    {
        image.material = null;
        var contour = image.GetComponent<AtlasContour>(); if (contour == null) contour = image.gameObject.AddComponent<AtlasContour>();
        contour.outline = points; image.SetVerticesDirty();
    }
    static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = P(.5f,.5f); rect.anchoredPosition = P(x,y); rect.sizeDelta = P(w,h);
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
    }
    static Image Image(string name, Transform parent, Sprite sprite, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>(); Place(image.rectTransform,x,y,w,h); image.sprite = sprite; image.raycastTarget = false; return image;
    }
    static TMP_Text Text(string name, Transform parent, string value, float size, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var text = go.GetComponent<TMP_Text>(); Place(text.rectTransform,x,y,w,h); text.font = font; text.fontSharedMaterial = outline;
        text.text = value; text.fontSize = size; text.fontStyle = FontStyles.Bold; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
    }
    static void StyleNumber(TMP_Text text, float size)
    {
        text.fontSharedMaterial = outline; text.color = Color.white; text.fontSize = size; text.fontStyle = FontStyles.Bold;
        text.characterSpacing = -2; text.alignment = TextAlignmentOptions.Center;
    }

    static void RefineCards()
    {
        string path = Prefabs + "/TableCard.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<CardView>();
            // Grey native stone gives the existing neutral creatures a quieter frame.
            view.styledMinionFrame = Slice("Card_Inhand_Minion_Rogue.png", "StoneMinionCard",170,0,680,1024);
            view.cardBackground.sprite = view.styledMinionFrame;
            Contour(view.cardBackground, CardOutline); view.frameContour = view.cardBackground.GetComponent<AtlasContour>();
            view.minionOutline = CardOutline; view.spellOutline = SpellOutline; view.backOutline = BackOutline;
            view.minionPortraitSize = P(111,147); view.minionPortraitPosition = P(0,50);
            view.spellPortraitSize = P(142,119); view.spellPortraitPosition = P(0,58);
            view.spellPortraitOutline = new[]{P(0,.1f),P(0,.83f),P(.15f,.94f),P(.34f,.99f),P(.5f,1),P(.7f,.97f),P(.88f,.9f),P(1,.82f),P(1,.1f),P(.82f,.025f),P(.5f,0),P(.18f,.025f)};
            Place(view.portraitMask.rectTransform,0,50,111,147);
            var parchment = view.faceUpDetails.transform.Find("Parchment").GetComponent<Image>();
            // Interior parchment only, so there is no second rectangular border over the card.
            parchment.sprite = Slice("Card_Inhand_Ability_Neutral.png", "CleanParchment",280,724,455,196);
            Place(parchment.rectTransform,0,-79,140,72); parchment.material = null; parchment.color = new Color(1,.96f,.87f);
            var scroll = view.faceUpDetails.transform.Find("NameScroll").GetComponent<Image>();
            Contour(scroll, UV(877,57,130,565,P(895,61),P(947,57),P(954,82),P(987,87),P(1006,112),P(1007,593),P(986,610),P(943,613),P(921,622),P(892,612),P(886,574),P(877,554),P(878,103)));
            Place(scroll.rectTransform,0,-27,34,176); scroll.rectTransform.localRotation = Quaternion.Euler(0,0,90);
            Place(view.nameText.rectTransform,0,-27,162,32); StyleNumber(view.nameText,16.5f);
            view.nameText.enableAutoSizing = true; view.nameText.fontSizeMin = 10; view.nameText.fontSizeMax = 16.5f; view.nameText.textWrappingMode = TextWrappingModes.NoWrap;
            Place(view.descriptionText.rectTransform,0,-80,135,65); view.descriptionText.fontSize = 14.5f;
            view.descriptionText.fontSizeMax = 14.5f; view.descriptionText.fontSizeMin = 10.5f; view.descriptionText.lineSpacing = -3;
            var cost = view.costText.transform.parent.GetComponent<Image>(); Contour(cost,GemOutline); Place(cost.rectTransform,-74,103,53,55);
            cost.material = fullMana;
            StyleNumber(view.costText,34); Place(view.costText.rectTransform,0,-1,52,50);
            var attack = view.attackText.transform.parent.GetComponent<Image>(); Contour(attack,AttackOutline); Place(attack.rectTransform,-70,-111,53,53);
            StyleNumber(view.attackText,32); Place(view.attackText.rectTransform,4,-3,46,44);
            var hp = view.healthText.transform.parent.GetComponent<Image>(); Contour(hp,HealthOutline); Place(hp.rectTransform,71,-111,42,57);
            StyleNumber(view.healthText,32); Place(view.healthText.rectTransform,0,-5,50,47);
            view.dragGhostAlpha = 1;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void RefineArtwork()
    {
        // Coordinates only: retain the source pixels, removing export borders and material alpha.
        var crops = new System.Collections.Generic.Dictionary<string,Rect>{
            {"HS16-003.png",new Rect(65,1,382,510)}, {"HS14-321.png",new Rect(1,35,510,442)},
            {"goblin_blastmage_D.png",new Rect(0,0,512,512)}, {"HS16-018.png",new Rect(70,1,373,510)},
            {"HS12-254.png",new Rect(1,49,510,414)}, {"HS14-306.png",new Rect(75,12,373,499)},
            {"HS16-006.png",new Rect(48,1,417,510)}, {"HS6-049.png",new Rect(16,24,495,487)},
            {"HS12-223_S.png",new Rect(0,84,355,370)}, {"HS13-024.png",new Rect(0,0,512,512)},
            {"HS6-002.png",new Rect(0,0,512,512)}, {"HS10-090.png",new Rect(65,1,382,510)},
            {"HS11-027.png",new Rect(65,1,382,510)}, {"HS14-233.png",new Rect(1,44,510,425)},
            {"HS23-054_M_Horse.png",new Rect(71,9,376,502)}, {"HS11-046.png",new Rect(0,0,512,512)},
            {"HS14-231.png",new Rect(1,49,510,414)}, {"HS8-075.png",new Rect(0,0,512,512)},
            {"HS14-238.png",new Rect(1,49,510,414)}, {"HS18-128_Wolf.png",new Rect(1,50,510,413)}
        };
        var e = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        foreach(var card in e.cardPool)
        {
            string sourcePath = AssetDatabase.GetAssetPath(card.artwork.texture);
            string file = Path.GetFileName(sourcePath);
            if(card.cardName=="Bear" || card.cardName=="Ironfur Grizzly") file = "HS12-223_S.png";
            if(!crops.TryGetValue(file,out Rect rect)) continue;
            // Keep imported artwork together; the original Desktop exports remain untouched.
            Import(file);
            card.artwork = Slice(file,"Portrait_"+Path.GetFileNameWithoutExtension(file),rect.x,rect.y,rect.width,rect.height);
            EditorUtility.SetDirty(card);
        }
    }

    static void RefineMinions()
    {
        string path = Prefabs + "/TableMinion.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<MinionView>();
            root.transform.localScale = Vector3.one*.87f;
            Contour(view.minionBackground, Circle(80,.015f));
            var shield = view.tauntVisual.GetComponent<Image>();
            Contour(shield, UV(30,5,195,248,P(38,24),P(94,8),P(127,5),P(162,9),P(216,24),P(223,121),P(215,167),P(193,206),P(158,236),P(129,252),P(98,237),P(65,209),P(42,169),P(31,122)));
            Contour(view.attackText.transform.parent.GetComponent<Image>(),AttackOutline);
            Contour(view.healthText.transform.parent.GetComponent<Image>(),HealthOutline);
            StyleNumber(view.attackText,32); StyleNumber(view.healthText,32);
            var glow = view.readyGlow.GetComponent<Image>(); glow.material = softGlow; glow.color = new Color(.15f,1,.06f,.9f);
            Place(glow.rectTransform,0,0,150,194);
            var frozen = view.frozenVisual.GetComponent<Image>();
            frozen.sprite = Slice("Card_InPlay_AllyFrame_Frozen.png","FrozenOval",16,0,100,124);
            Contour(frozen,Circle(80,.01f)); Place(frozen.rectTransform,0,0,134,177);
            view.cannotAttackColor = Color.white; view.selectedColor = Color.white; view.frozenColor = Color.white;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void RefineMana()
    {
        string path = Prefabs + "/TableMana.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var image = root.GetComponent<Image>(); image.sprite = mana; image.color = Color.white; Contour(image,GemOutline);
            Place(image.rectTransform,0,0,34,36); PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var e = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        var serialized = new SerializedObject(e.faceView);
        serialized.FindProperty("manaCrystalFullSprite").objectReferenceValue = mana;
        serialized.FindProperty("manaCrystalEmptySprite").objectReferenceValue = mana; serialized.ApplyModifiedPropertiesWithoutUndo();
        serialized.FindProperty("manaCrystalFullMaterial").objectReferenceValue = fullMana;
        serialized.FindProperty("manaCrystalEmptyMaterial").objectReferenceValue = spentMana; serialized.ApplyModifiedPropertiesWithoutUndo();
        var surface = GameObject.Find("TableSurface").transform;
        var tray = surface.Find("ManaTray").GetComponent<Image>();
        tray.sprite = Slice("ManaTray.png", "NativeManaTray",423,0,89,512); tray.color = new Color(1,.91f,.72f);
        Place(tray.rectTransform,615,-484,57,470); tray.rectTransform.localRotation = Quaternion.Euler(0,0,-90);
        Contour(tray,new[]{P(.16f,.01f),P(.82f,.01f),P(.99f,.045f),P(.99f,.955f),P(.82f,.99f),P(.16f,.99f),P(.02f,.955f),P(.02f,.045f)});
        var row = (RectTransform)surface.Find("ManaCrystalRow"); Place(row,658,-484,365,40);
        var layout = row.GetComponent<HorizontalLayoutGroup>(); layout.spacing = 1; layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = layout.childControlWidth = false; layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var manaText = surface.Find("PlayerMana").GetComponent<TMP_Text>(); Place(manaText.rectTransform,421,-484,84,40); StyleNumber(manaText,25);
        var topText = surface.Find("OpponentMana").GetComponent<TMP_Text>(); Place(topText.rectTransform,205,360,90,45); StyleNumber(topText,23);
    }

    static void RefinePower()
    {
        var e = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        var button = e.heroPowerButton;
        foreach (Transform child in button.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        var image = button.GetComponent<Image>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Hero_Power_Frame.png");
        image.color = Color.white; Contour(image,Circle(80,.025f)); image.raycastTarget = true;
        Place(image.rectTransform,202,-312,139,139);
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1,.95f,.75f); colors.pressedColor = new Color(.75f,.67f,.48f); button.colors = colors;
        // Use the foreground fireball in the supplied Mage power art, not a tiny unrelated spell image.
        var portrait = Image("Fireblast",button.transform,Slice("Mage_power2_D.png","FireblastPower",125,120,350,350),0,-3,89,89);
        Contour(portrait,Circle(64,.005f));
        var used = Image("UsedPower",button.transform,Slice("Hero_Power_D.png","UsedPowerBack",28,27,207,207),0,-3,90,90); Contour(used,Circle(64,.015f)); used.enabled = false;
        var glow = Image("ReadyGlow",button.transform,AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/Hero_Power_Silhouette.png"),0,-2,171,171);
        glow.color = new Color(.16f,1,.06f,.85f); glow.material = softGlow; glow.transform.SetAsFirstSibling();
        var gem = Image("PowerCost",button.transform,mana,0,64,43,45); Contour(gem,GemOutline);
        gem.material = fullMana;
        Text("Cost",gem.transform,"2",30,0,-1,48,45);
        var presentation = button.GetComponent<HeroPowerPresentation>(); if(presentation==null) presentation = button.gameObject.AddComponent<HeroPowerPresentation>();
        presentation.artwork = portrait; presentation.usedBack = used; presentation.readyGlow = glow.gameObject;
        glow.gameObject.SetActive(false);
    }
}
