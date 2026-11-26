using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Source property readers operate on saved values because original shaders are
// unavailable. Called only by the two-card prototype installer, not at runtime.
public static class PrototypeCardMaterialBaker
{
    public enum SurfaceMode { Unlit = 0, SecondUvMultiply = 1, GemMaskScroller = 2 }

    public static SurfaceMode ModeFor(Material source)
    {
        switch (source.name)
        {
            case "Card_InHand_Ally-Card_InPlay_AllyPortrait":
            case "Card_InHand_Ability-portrait":
            case "Card_InHand_BannerAtlasMod2x":
            case "Card_InHand_Ability_BannerAtlasMod2x":
                return SurfaceMode.SecondUvMultiply;
            case "Gem_Mana_Blue":
            case "Gem_Attack_Gem InHand":
            case "Gem_Health_Gem InHand":
                return SurfaceMode.GemMaskScroller;
            default: return SurfaceMode.Unlit;
        }
    }

    public static Material Bake(Material source, string outputDirectory, string key,
        bool enableCompatibilityScroller = true)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        Directory.CreateDirectory(outputDirectory);
        string path = outputDirectory.TrimEnd('/') + "/" + key + ".mat";
        var shader = Shader.Find("HearthClone/UI/Prototype Original Card Surface");
        if (shader == null) throw new InvalidOperationException("Import prototype shader first.");
        var result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (result == null)
        {
            result = new Material(shader);
            AssetDatabase.CreateAsset(result, path);
        }
        result.shader = shader;
        var mode = ModeFor(source);
        result.SetFloat("_SurfaceMode", (int)mode);
        result.SetTexture("_MainTex", SourceMaterialProperties.Texture(source, "_MainTex"));
        // The original unlit dual-class frame shader does not declare _Color.
        // Stored _Color/_Shininess/etc can outlive a material's shader: only use
        // the actual shader's declared inputs for the applicable mode.
        result.SetColor("_Color", mode == SurfaceMode.SecondUvMultiply
            ? SourceMaterialProperties.Color(source, "_Color") : Color.white);
        if (mode == SurfaceMode.SecondUvMultiply)
        {
            result.SetTexture("_SecondTex", SourceMaterialProperties.Texture(source, "_SecondTex"));
            result.SetTextureScale("_SecondTex", SourceMaterialProperties.Scale(source, "_SecondTex"));
            result.SetTextureOffset("_SecondTex", SourceMaterialProperties.Offset(source, "_SecondTex"));
            result.SetColor("_SecondTint", SourceMaterialProperties.Color(source, "_SecondTint"));
            result.SetFloat("_BlendIntensity", SourceMaterialProperties.Float(source, "_BlendIntensity"));
        }
        else if (mode == SurfaceMode.GemMaskScroller)
        {
            result.SetTexture("_ScrollingTex", SourceMaterialProperties.Texture(source, "_ScrollingTex"));
            result.SetTextureScale("_ScrollingTex", SourceMaterialProperties.Scale(source, "_ScrollingTex"));
            result.SetTextureOffset("_ScrollingTex", SourceMaterialProperties.Offset(source, "_ScrollingTex"));
            foreach (string name in new[] { "_TintColor", "_tint", "_Fade" })
                result.SetColor(name, SourceMaterialProperties.Color(source, name));
            foreach (string name in new[] { "_Intensity", "_ScaleX", "_ScaleY", "_XSpeed", "_YSpeed" })
                result.SetFloat(name, SourceMaterialProperties.Float(source, name));
        }
        result.SetFloat("_SourceClockEnabled", enableCompatibilityScroller ? 1 : 0);
        EditorUtility.SetDirty(result);
        return result;
    }
}
