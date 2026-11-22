using System;
using UnityEditor;
using UnityEngine;

// Material.GetColor/GetFloat consult the current shader and can return zero when
// an exported original shader is missing. Read the preserved saved values instead.
public static class SourceMaterialProperties
{
    static SerializedProperty Value(Material material, string group, string name)
    {
        var serialized = new SerializedObject(material);
        var entries = serialized.FindProperty("m_SavedProperties." + group);
        for (int i = 0; entries != null && i < entries.arraySize; i++)
        {
            var pair = entries.GetArrayElementAtIndex(i);
            if (pair.FindPropertyRelative("first").stringValue == name)
                return pair.FindPropertyRelative("second");
        }
        throw new InvalidOperationException("Original material " + material.name + " has no saved " + name);
    }

    public static Color Color(Material material, string name) => Value(material, "m_Colors", name).colorValue;
    public static float Float(Material material, string name) => Value(material, "m_Floats", name).floatValue;
    public static Texture Texture(Material material, string name) =>
        Value(material, "m_TexEnvs", name).FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
    public static Vector2 Scale(Material material, string name) =>
        Value(material, "m_TexEnvs", name).FindPropertyRelative("m_Scale").vector2Value;
    public static Vector2 Offset(Material material, string name) =>
        Value(material, "m_TexEnvs", name).FindPropertyRelative("m_Offset").vector2Value;
}
