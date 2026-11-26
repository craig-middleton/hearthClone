using System;
using System.IO;
using HearthstoneClone.Cards;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Target-only source NameUberText replacement. Dimensions, colors, source outline
// size and fit policy are recovered; UberText-to-TMP font units and the circular
// outline raster kernel below are compatibility conversions, not original code.
public static class PrototypeCardTypographyBaker
{
    const int Layer = 28;
    const int Supersample = 2;
    const int SourceResolution = 300;
    const float SourceWidth = 1.9f;
    static readonly Color OutlineColor = new Color(.0392156877f, .030397566f, .021530181f, 1);

    public static Texture2D Bake(CardData data, bool spell, string path)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode required.");
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
            throw new ArgumentException("An asset PNG path is required.", nameof(path));
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/_Project/RecoveredInterface/Proof/Belwe.asset");
        if (font == null) throw new InvalidOperationException("Original Belwe font asset is missing.");

        float sourceHeight = spell ? .28f : .3f;
        int width = SourceResolution * Supersample;
        int height = Mathf.RoundToInt(width * sourceHeight / SourceWidth);
        int outlineRadius = 3 * Supersample;
        var stage = new GameObject("Temporary prototype source name baking")
            { layer = Layer, hideFlags = HideFlags.HideAndDontSave };
        // Layer isolation handles other geometry; stay close enough to preserve
        // subpixel glyph precision when object/world matrices are evaluated.
        stage.transform.position = new Vector3(1000, 1000, 1000);
        RenderTexture target = null;
        Texture2D pixels = null;
        Material fontMaterial = null;
        Camera camera = null;
        var previous = RenderTexture.active;
        try
        {
            var go = new GameObject("Original NameUberText", typeof(RectTransform), typeof(TextMeshPro))
                { layer = Layer, hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(stage.transform, false);
            var text = go.GetComponent<TextMeshPro>();
            text.font = font;
            fontMaterial = new Material(font.material)
                { name = "Temporary prototype Belwe material", hideFlags = HideFlags.HideAndDontSave };
            fontMaterial.SetFloat("_OutlineWidth", 0);
            fontMaterial.SetFloat("_FaceDilate", 0);
            fontMaterial.DisableKeyword("OUTLINE_ON");
            fontMaterial.DisableKeyword("UNDERLAY_ON");
            fontMaterial.DisableKeyword("UNDERLAY_INNER");
            text.fontSharedMaterial = fontMaterial;
            text.text = data.cardName;
            text.color = Color.white;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(SourceWidth, sourceHeight);
            // Source NameUberText uses font40/min20 (min30 for spell), fit but
            // never grow, no wrap, no shadow. .1 is our documented conversion
            // from UberText font size to TMP world text units. No Y-only stretch.
            text.enableAutoSizing = true;
            text.fontSizeMax = 40 * .1f;
            text.fontSizeMin = (spell ? 30 : 20) * .1f;
            text.fontSize = text.fontSizeMax;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            float pad = (outlineRadius + 1) * SourceWidth / width;
            text.margin = new Vector4(pad, pad, pad, pad);
            text.ForceMeshUpdate();
            foreach (var child in stage.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = Layer;

            var cameraObject = new GameObject("Isolated prototype name camera", typeof(Camera))
                { layer = Layer, hideFlags = HideFlags.HideAndDontSave };
            cameraObject.transform.SetParent(stage.transform, false);
            camera = cameraObject.GetComponent<Camera>();
            camera.transform.localPosition = new Vector3(0, 0, -2);
            camera.orthographic = true;
            camera.orthographicSize = sourceHeight * .5f;
            camera.aspect = SourceWidth / sourceHeight;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 4;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << Layer;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.enabled = false;
            camera.GetUniversalAdditionalCameraData().SetRenderer(1);
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Temporary prototype name target",
                hideFlags = HideFlags.HideAndDontSave
            };
            target.Create();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            pixels.Apply();
            pixels.SetPixels32(Outline(pixels.GetPixels32(), width, height, outlineRadius));
            pixels.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        finally
        {
            RenderTexture.active = previous;
            if (camera != null) camera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(stage);
            if (fontMaterial != null) UnityEngine.Object.DestroyImmediate(fontMaterial);
        }
    }

    static Color32[] Outline(Color32[] ink, int width, int height, int radius)
    {
        var result = new Color32[ink.Length];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            byte edge = 0;
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy > radius * radius) continue;
                int sx = x + dx, sy = y + dy;
                if (sx < 0 || sx >= width || sy < 0 || sy >= height) continue;
                edge = (byte)Mathf.Max(edge, ink[sy * width + sx].a);
            }
            float inside = ink[y * width + x].a / 255f;
            float outside = edge / 255f * (1 - inside);
            float alpha = inside + outside;
            if (alpha <= 0) { result[y * width + x] = new Color32(0, 0, 0, 0); continue; }
            var rgb = (Color.white * inside + OutlineColor * outside) / alpha;
            rgb.a = alpha;
            result[y * width + x] = rgb;
        }
        return result;
    }
}
