using System;
using System.IO;
using System.Linq;
using HearthstoneClone.Cards;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

// Isolated visual source study. Never changes the playable scene or its card assets.
public static class CreateRecoveredCardProof
{
    public const string Root = "Assets/_Project/RecoveredInterface";
    public const string Proof = Root + "/Proof";
    const string Bundles = Root + "/AssetBundles/";
    const string Actors = Bundles + "essential_base_global-prefab-0.unity3d/";
    const string Meshes = Bundles + "essential_base_global-mesh-0.unity3d/";
    const string Fonts = Bundles + "essential_base_global-content-0.unity3d/";
    const string ScenePath = "Assets/Scenes/RecoveredInterfaceProof.unity";

    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before creating the source proof.");
        // Scene replacement can unload unreferenced ScriptableObjects. Load assets afterwards.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Directory.CreateDirectory(Proof); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var belwe = CreateProofFont("Belwe", Fonts + "Belwe.ttf");
        var franklin = CreateProofFont("FranklinGothic", Fonts + "FranklinGothic.ttf");
        var neutral = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/SourcePaint/Card_Inhand_Generic.png");
        var mage = AssetDatabase.LoadAssetAtPath<Texture2D>(Bundles + "essential_base_global-texture-0.unity3d/Card_Inhand_Ability_Mage.png");
        if (neutral == null || mage == null) throw new InvalidOperationException("The verified generic/Mage source frame textures are missing.");
        var minionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Actors + "Card_Hand_Ally.prefab");
        var spellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Actors + "Card_Hand_Ability.prefab");
        if (minionPrefab == null || spellPrefab == null) throw new InvalidOperationException("The sanitized full source card actors are missing.");
        var wolf = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_Project/ScriptableObjects/KeywordMinions/WolfRider.asset");
        var fireball = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_Project/Art/HearthstoneTable/Cards/TestCard_Fireball.asset");
        if (wolf == null || fireball == null) throw new InvalidOperationException("Existing proof card data missing.");

        var camera = new GameObject("Recovered Interface Camera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 2.15f; camera.aspect = 16f / 9f;
        camera.transform.position = new Vector3(0, 10, 0); camera.transform.rotation = Quaternion.Euler(90, 0, 0);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.045f, .060f, .075f);
        camera.nearClipPlane = .1f; camera.farClipPlane = 30; camera.GetUniversalAdditionalCameraData().SetRenderer(1);
        var ally = (GameObject)PrefabUtility.InstantiatePrefab(minionPrefab, scene);
        var spell = (GameObject)PrefabUtility.InstantiatePrefab(spellPrefab, scene);
        ally.name = "Native minion — Wolf Rider"; spell.name = "Native spell — Fireball";
        ally.transform.position = new Vector3(-2.65f, 0, 0); spell.transform.position = new Vector3(-.20f, 0, 0);
        Configure(ally, wolf, false, neutral, belwe, franklin);
        Configure(spell, fireball, true, mage, belwe, franklin);
        foreach (var component in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)))
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = ally;
        return "Created isolated original-card proof: native generic minion + Mage spell meshes, exact frame/portrait slots, existing card art/data, genuine Belwe/Franklin text and names baked onto original curved-text meshes. Right-hand space reserved for targeting/button proof. No live game or build settings changed.";
    }

    static void Configure(GameObject actor, CardData card, bool spell, Texture frame, TMP_FontAsset belwe, TMP_FontAsset franklin)
    {
        foreach (var animation in actor.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
        // Source Actor scripts normally perform these switches. Retain only the normal card branch.
        foreach (Transform child in actor.transform) child.gameObject.SetActive(child.name == "RootObject");
        var root = actor.transform.Find("RootObject");
        foreach (Transform child in root)
            child.gameObject.SetActive(spell ? child.name == "NonQuestObjects" :
                child.name == "Mesh" || child.name == "AttackObject" || child.name == "HealthObject" || child.name == "CostObject");
        var normal = spell ? root.Find("NonQuestObjects") : root;
        if (spell)
            foreach (Transform child in normal) child.gameObject.SetActive(child.name == "Mesh" || child.name == "CostObject");
        var mesh = normal.Find("Mesh");
        foreach (var transform in mesh.GetComponentsInChildren<Transform>(true))
            if (transform.name.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                transform.name.IndexOf("Dragon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                transform.name.IndexOf("Faction", StringComparison.OrdinalIgnoreCase) >= 0 ||
                transform.name.IndexOf("Rarity", StringComparison.OrdinalIgnoreCase) >= 0)
                transform.gameObject.SetActive(false);

        // Copy materials into the proof folder; original imported materials remain untouched.
        foreach (var renderer in actor.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.gameObject.activeInHierarchy))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] != null)
                {
                    materials[i] = CopyPaint(materials[i], card.name + "_" + materials[i].name);
                    // Spell banners/description also share the dynamically assigned class atlas.
                    var sourceTexture = materials[i].mainTexture;
                    if (sourceTexture != null && (sourceTexture.name.StartsWith("Card_Inhand_Ability_") || sourceTexture.name.StartsWith("Card_Inhand_Minion_")))
                        materials[i].mainTexture = frame;
                }
            renderer.sharedMaterials = materials;
        }
        var baseRenderer = mesh.GetComponent<MeshRenderer>();
        var baseMaterials = baseRenderer.sharedMaterials;
        baseMaterials[0].mainTexture = frame; baseMaterials[0].mainTextureScale = Vector2.one; baseMaterials[0].mainTextureOffset = Vector2.zero;
        var portrait = mesh.Find("PortraitFrame_mesh").GetComponent<MeshRenderer>();
        var portraitMaterials = portrait.sharedMaterials;
        int portraitIndex = spell ? 1 : 0, frameIndex = spell ? 0 : 1;
        portraitMaterials[frameIndex].mainTexture = frame;
        portraitMaterials[frameIndex].mainTextureScale = Vector2.one; portraitMaterials[frameIndex].mainTextureOffset = Vector2.zero;
        var art = card.artwork;
        portraitMaterials[portraitIndex].mainTexture = art.texture;
        portraitMaterials[portraitIndex].mainTextureScale = new Vector2(art.rect.width / art.texture.width, art.rect.height / art.texture.height);
        portraitMaterials[portraitIndex].mainTextureOffset = new Vector2(art.rect.x / art.texture.width, art.rect.y / art.texture.height);
        baseRenderer.sharedMaterials = baseMaterials; portrait.sharedMaterials = portraitMaterials;

        var nameBanner = mesh.Find("NameBanner_mesh");
        var curved = nameBanner.Find("Card_InHand_Ally_CurvedText").GetComponent<MeshRenderer>();
        var nameTexture = BakeTextTexture(card.name + "_CurvedName", card.cardName, belwe, Color.white, true);
        var nameMaterial = Paint(card.name + "_CurvedName", nameTexture, true);
        curved.sharedMaterial = nameMaterial; curved.enabled = true;
        var obsoleteName = nameBanner.Find("NameUberText"); if (obsoleteName != null) obsoleteName.gameObject.SetActive(false);
        Text(normal.Find("CostObject/CostUberText"), card.manaCost.ToString(), belwe, .69f, .67f, Color.white, true);
        if (!spell)
        {
            Text(normal.Find("AttackObject/AttackUberText"), card.attack.ToString(), belwe, .45f, .35f, Color.white, true);
            Text(normal.Find("HealthObject/HealthUberText"), card.health.ToString(), belwe, .45f, .35f, Color.white, true);
        }
        var description = mesh.Find("Description_mesh");
        if (spell)
        {
            var schoolMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Meshes + "Card_InHand_Ability_Description_School_mesh.asset");
            if (schoolMesh != null) description.GetComponent<MeshFilter>().sharedMesh = schoolMesh;
            var school = description.Find("RaceUberText");
            var schoolText = Text(school, card.spellSchool.ToString(), belwe, .85f, .17f, Color.white, true);
            // Actor adjusts this anchor when switching from the neutral description mesh.
            schoolText.transform.position += Vector3.forward * .13f;
        }
        Text(description.Find("PowersUberText"), card.description, franklin, 1.7f, .82f, new Color(.118f, .091f, .065f), false);
        foreach (var renderer in actor.GetComponentsInChildren<MeshRenderer>(true))
            foreach (var material in renderer.sharedMaterials) if (material != null && AssetDatabase.GetAssetPath(material).StartsWith(Proof)) EditorUtility.SetDirty(material);
    }

    static TMP_Text Text(Transform anchor, string value, TMP_FontAsset font, float width, float height, Color color, bool outline)
    {
        if (anchor == null) throw new InvalidOperationException("Native source text anchor missing.");
        var go = new GameObject("Recovered TMP text", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(anchor, false);
        var text = go.GetComponent<TextMeshPro>(); text.font = font; text.text = value; text.color = color;
        text.rectTransform.sizeDelta = new Vector2(width, height);
        text.alignment = TextAlignmentOptions.Center; text.enableAutoSizing = true; text.fontSizeMin = .5f; text.fontSizeMax = 8;
        if (!outline) { text.fontSizeMax = 2.6f; text.lineSpacing = -3; }
        text.textWrappingMode = outline ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow; text.richText = true; text.margin = Vector4.zero;
        var paint = new Material(font.material); paint.name = font.name + (outline ? " outlined proof" : " proof ink");
        paint.SetFloat("_OutlineWidth", outline ? .22f : 0); paint.SetColor("_OutlineColor", new Color(.08f, .035f, .012f));
        if (outline) paint.EnableKeyword("OUTLINE_ON");
        string path = Proof + "/" + font.name + (outline ? "Outlined.mat" : "Ink.mat");
        paint.name = Path.GetFileNameWithoutExtension(path);
        var old = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (old == null) { AssetDatabase.CreateAsset(paint, path); old = paint; }
        else { EditorUtility.CopySerialized(paint, old); UnityEngine.Object.DestroyImmediate(paint); }
        text.fontSharedMaterial = old; text.ForceMeshUpdate(); return text;
    }

    public static TMP_FontAsset CreateProofFont(string name, string sourceFontPath)
    {
        Directory.CreateDirectory(Proof); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string path = Proof + "/" + name + ".asset";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font != null) return font;
        var source = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
        if (source == null) throw new InvalidOperationException("Recovered source font is missing: " + sourceFontPath);
        font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        font.name = name; AssetDatabase.CreateAsset(font, path);
        foreach (var atlas in font.atlasTextures) { atlas.name = name + " source glyph atlas"; AssetDatabase.AddObjectToAsset(atlas, font); }
        font.material.name = name + " source glyph material"; AssetDatabase.AddObjectToAsset(font.material, font);
        font.TryAddCharacters(new string(Enumerable.Range(32, 95).Select(i => (char)i).ToArray()));
        font.atlasPopulationMode = AtlasPopulationMode.Static; EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
        return font;
    }

    // Bake only dynamic typography. Card frames/art stay on their original individual meshes.
    public static Texture2D BakeTextTexture(string assetName, string value, TMP_FontAsset font, Color color, bool outline, int width = 1024, int height = 192)
        => NativeCardTextBaker.Bake(Proof, assetName, value, font, color, outline, width, height);

    static Material CopyPaint(Material original, string name)
    {
        var properties = new SerializedObject(original); var pairs = properties.FindProperty("m_SavedProperties.m_TexEnvs");
        Texture texture = null; Vector2 scale = Vector2.one, offset = Vector2.zero;
        foreach (string property in new[] { "_MainTex", "_BotTex", "_TopTex" })
        {
            for (int i = 0; i < pairs.arraySize; i++)
            {
                var pair = pairs.GetArrayElementAtIndex(i); if (pair.FindPropertyRelative("first").stringValue != property) continue;
                var value = pair.FindPropertyRelative("second"); var candidate = (Texture)value.FindPropertyRelative("m_Texture").objectReferenceValue;
                if (candidate == null) continue;
                texture = candidate; scale = value.FindPropertyRelative("m_Scale").vector2Value; offset = value.FindPropertyRelative("m_Offset").vector2Value; break;
            }
            if (texture != null) break;
        }
        var material = Paint(name, texture, false); material.mainTextureScale = scale; material.mainTextureOffset = offset; return material;
    }

    static Material Paint(string name, Texture texture, bool transparent)
    {
        string path = Proof + "/" + name.Replace('/', '_').Replace('\\', '_') + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find(transparent ? "Unlit/Transparent" : "Unlit/Texture")); AssetDatabase.CreateAsset(material, path); }
        material.shader = Shader.Find(transparent ? "Unlit/Transparent" : "Unlit/Texture"); material.mainTexture = texture;
        material.renderQueue = transparent ? 3000 : 2000; EditorUtility.SetDirty(material); return material;
    }
}
