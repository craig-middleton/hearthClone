using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Editor-only typography bake onto native curved-name meshes.
public static class NativeCardTextBaker
{
    public static Texture2D Bake(string directory, string assetName, string value, TMP_FontAsset font, Color color, bool outline, int width = 1024, int height = 192)
    {
        var stage = new GameObject("Temporary native text baking stage");
        stage.transform.position = new Vector3(1000, 1000, 1000);
        RenderTexture texture = null; var before = RenderTexture.active;
        try
        {
            var textObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshPro));
            textObject.transform.SetParent(stage.transform, false);
            var text = textObject.GetComponent<TextMeshPro>(); text.font = font; text.text = value; text.color = color;
            text.rectTransform.sizeDelta = new Vector2(5.15f, .94f);
            text.alignment = TextAlignmentOptions.Center; text.enableAutoSizing = true; text.fontSizeMin = 1; text.fontSizeMax = 40;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Overflow;
            text.fontMaterial.SetFloat("_OutlineWidth", outline ? .14f : 0);
            text.fontMaterial.SetColor("_OutlineColor", new Color(.08f, .035f, .012f));
            if (outline) text.fontMaterial.EnableKeyword("OUTLINE_ON");
            text.ForceMeshUpdate();
            // Fit glyphs consistently into the native curved strip. Width-based
            // autosizing otherwise makes long titles much thinner than short ones.
            float inkHeight=text.textBounds.size.y;
            if(inkHeight>.0001f)text.transform.localScale=new Vector3(1,.82f/inkHeight,1);
            var camera = new GameObject("Name capture", typeof(Camera)).GetComponent<Camera>(); camera.transform.SetParent(stage.transform, false);
            camera.transform.localPosition = new Vector3(0, 0, -5); camera.orthographic = true; camera.orthographicSize = .5f;
            camera.aspect = (float)width / height; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            camera.nearClipPlane = .1f; camera.farClipPlane = 10; camera.GetUniversalAdditionalCameraData().SetRenderer(1);
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); camera.targetTexture = texture;
            camera.Render(); RenderTexture.active = texture;
            var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            string path = directory + "/" + assetName + ".png"; File.WriteAllBytes(path, pixels.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(pixels); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        finally
        {
            RenderTexture.active = before;
            foreach (var camera in stage.GetComponentsInChildren<Camera>()) camera.targetTexture = null;
            if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
            UnityEngine.Object.DestroyImmediate(stage);
        }
    }

}
