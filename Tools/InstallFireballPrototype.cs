using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;

// Only Fireball's two phase prefab references/materials change. All other entries,
// source meshes, transforms, PS modules, sound/clock/viewport data remain intact.
public static class InstallFireballPrototype
{
    const string Folder="Assets/_Project/Art/FireballPrototype/";
    static FireballPrototypeSource source;
    static readonly Dictionary<string,FireballPrototypeResolvedMaterial> resolved=new Dictionary<string,FireballPrototypeResolvedMaterial>();

    static bool Active(FireballPrototypeSourceMaterial entry,string field) =>
        entry.activeProperties.Contains(field);

    static bool Saved(Material material,string group,string name)
    {
        var fields=new SerializedObject(material).FindProperty("m_SavedProperties."+group);
        for(int i=0;fields!=null&&i<fields.arraySize;i++)
            if(fields.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue==name)return true;
        return false;
    }

    static string Hash(string path)
    {
        using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
    }

    static void ValidateOriginal(Material original,FireballPrototypeSourceMaterial entry,string path)
    {
        try
        {
            // Export sessions remap GUIDs. Resolve by exact original name only
            // after checking every active saved scalar/color, texture bytes and UVs
            // against the complete export that identifies the real shader family.
            foreach(var field in entry.floats??Array.Empty<FireballPrototypeFloat>())
                if(!Mathf.Approximately(SourceMaterialProperties.Float(original,field.name),field.value))
                    throw new Exception("saved float differs: "+field.name);
            foreach(var field in entry.colors??Array.Empty<FireballPrototypeColor>())
            {
                var actual=SourceMaterialProperties.Color(original,field.name);
                if(Mathf.Abs(actual.r-field.value.r)>1e-5f||Mathf.Abs(actual.g-field.value.g)>1e-5f||
                    Mathf.Abs(actual.b-field.value.b)>1e-5f||Mathf.Abs(actual.a-field.value.a)>1e-5f)
                    throw new Exception("saved color differs: "+field.name);
            }
            foreach(var field in entry.textures??Array.Empty<FireballPrototypeTexture>())
            {
                var paint=SourceMaterialProperties.Texture(original,field.name);
                if(paint==null)throw new Exception("missing original paint: "+field.name);
                if(Vector2.Distance(SourceMaterialProperties.Scale(original,field.name),field.scale)>1e-5f||
                   Vector2.Distance(SourceMaterialProperties.Offset(original,field.name),field.offset)>1e-5f)
                    throw new Exception("original paint UV transform differs: "+field.name);
                if(!string.IsNullOrEmpty(field.sha256))
                {
                    var paintPath=AssetDatabase.GetAssetPath(paint);
                    if(!File.Exists(paintPath)||Hash(paintPath)!=field.sha256)
                        throw new Exception("original paint content differs: "+field.name+" at "+paintPath);
                }
                // Unity's built-in particle paint has no transferable file bytes;
                // it remains the preserved original imported material binding.
            }
        }
        catch(Exception error)
        {
            throw new Exception("Fireball source validation failed for '"+original.name+"' at '"+path+
                "', expected family '"+entry.sourceShader+"': "+error.Message,error);
        }
    }

    static Material Adapt(Material original)
    {
        string importedPath=AssetDatabase.GetAssetPath(original);
        if(string.IsNullOrEmpty(importedPath)||original.name.EndsWith("_Compatibility",StringComparison.Ordinal)||
            importedPath.StartsWith(Folder,StringComparison.Ordinal))
            throw new Exception("Expected preserved original Fireball material, got '"+original.name+"' at '"+importedPath+"'");
        var guid=AssetDatabase.AssetPathToGUID(importedPath);
        var matches=source.materials.Where(m=>m.guid==guid).ToArray();
        bool byName=matches.Length==0;
        if(byName)matches=source.materials.Where(m=>m.name==original.name).ToArray();
        if(matches.Length!=1)
            throw new Exception("No unique Fireball source family for material '"+original.name+"', path='"+importedPath+"', guid="+guid);
        var entry=matches[0];
        ValidateOriginal(original,entry,importedPath);
        resolved[importedPath]=new FireballPrototypeResolvedMaterial
        {
            importedPath=importedPath,importedGuid=guid,sourceGuid=entry.guid,
            sourceShader=entry.sourceShader,sourceSha256=entry.sourceSha256,
            matchedByValidatedName=byName
        };
        string shader=entry.sourceShader;
        int kind=shader.EndsWith("Unlit_Cutout",StringComparison.Ordinal)?3:
            shader=="Hero/Particle/Particle Multiply"?4:
            shader=="Hero/Particle/Particles_Transparent"?5:
            shader.Contains("2Layer")?2:
            shader.StartsWith("Hero/Particle/",StringComparison.Ordinal)?1:0;
        string path=Folder+original.name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        var compatibility=Shader.Find("HearthClone/Prototype/Fireball Source Surface");
        if(compatibility==null||!compatibility.isSupported)throw new Exception("Fireball prototype shader unavailable");
        if(material==null){material=new Material(compatibility);AssetDatabase.CreateAsset(material,path);}
        material.shader=compatibility;
        foreach(string field in new[]{"_MainTex","_AlphaNoise"})
        {
            if(!Active(entry,field))continue;
            var paint=SourceMaterialProperties.Texture(original,field);
            if(paint==null)throw new Exception("Unresolved original Fireball paint: "+entry.name+" "+field);
            material.SetTexture(field,paint);
            material.SetTextureScale(field,SourceMaterialProperties.Scale(original,field));
            material.SetTextureOffset(field,SourceMaterialProperties.Offset(original,field));
        }
        foreach(string field in new[]{"_Color","_TintColor"})
            material.SetColor(field,Active(entry,field)&&Saved(original,"m_Colors",field)?
                SourceMaterialProperties.Color(original,field):Color.white);
        foreach(string field in new[]{"_ColorMul","_Intensity","_Alpha","_AlphaIntensity","_Scale"})
            material.SetFloat(field,Active(entry,field)&&Saved(original,"m_Floats",field)?
                SourceMaterialProperties.Float(original,field):1);
        foreach(string field in new[]{"_USpeed","_VSpeed","_offsetU","_offsetV","_Cutoff"})
            material.SetFloat(field,Active(entry,field)&&Saved(original,"m_Floats",field)?
                SourceMaterialProperties.Float(original,field):0);
        // Shader declarations distinguish _Color from stale _TintColor values.
        if(kind==1||kind==2)
            material.SetFloat("_ColorMul",Active(entry,"_ColorMul")?
                SourceMaterialProperties.Float(original,"_ColorMul"):2);
        material.SetFloat("_Kind",kind);material.SetFloat("_SourceAlpha",1);
        material.SetFloat("_SrcBlend",kind==3?1:kind==4?2:
            Active(entry,"_RgbSrcBlend")?SourceMaterialProperties.Float(original,"_RgbSrcBlend"):5);
        material.SetFloat("_DstBlend",kind==3||kind==4?0:
            Active(entry,"_RgbDstBlend")?SourceMaterialProperties.Float(original,"_RgbDstBlend"):1);
        material.SetFloat("_ZWrite",kind==3?1:0);
        material.renderQueue=kind==3?2450:3000;
        EditorUtility.SetDirty(material);return material;
    }

    static GameObject Phase(NativeSpellLibrary.Phase phase)
    {
        var template=AssetDatabase.LoadAssetAtPath<GameObject>(phase.sourcePrefab);
        if(template==null)throw new Exception("Source Fireball phase missing: "+phase.sourcePrefab);
        var actor=UnityEngine.Object.Instantiate(template);
        try
        {
            actor.name=phase.name+"_Prototype";actor.transform.position=Vector3.zero;
            foreach(var animation in actor.GetComponentsInChildren<Animation>(true))
            {animation.enabled=false;animation.playAutomatically=false;}
            foreach(var animator in actor.GetComponentsInChildren<Animator>(true))animator.enabled=false;
            foreach(var renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select((original,slot)=>
                {
                    if(original!=null)return Adapt(original);
                    if(renderer is ParticleSystemRenderer&&slot==1&&
                        !renderer.GetComponent<ParticleSystem>().trails.enabled)return null;
                    throw new Exception("Null active Fireball material: "+renderer.name+" slot"+slot);
                }).ToArray();
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }
            return PrefabUtility.SaveAsPrefabAsset(actor,Folder+actor.name+".prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(actor);}
    }

    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Clean Edit required");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        resolved.Clear();
        source=JsonUtility.FromJson<FireballPrototypeSource>(File.ReadAllText(Folder+"SourceMaterialFamilies.json"));
        if(source?.materials?.Length!=11)throw new Exception("Prepare Fireball source family manifest first");
        var library=AssetDatabase.LoadAssetAtPath<NativeSpellLibrary>("Assets/_Project/Art/NativeSpells/NativeSpells.asset");
        var fireball=library.entries.Single(e=>e.cardName=="Fireball");
        string frostBefore=JsonUtility.ToJson(library.entries.Single(e=>e.cardName=="Frostbolt"));
        var missile=Phase(fireball.missile);var impact=Phase(fireball.impact);
        fireball.missile.prefab=missile;fireball.impact.prefab=impact;
        if(frostBefore!=JsonUtility.ToJson(library.entries.Single(e=>e.cardName=="Frostbolt")))
            throw new Exception("Non-prototype spell changed");
        File.WriteAllText(Folder+"ResolvedMaterialBindings.json",JsonUtility.ToJson(
            new FireballPrototypeResolvedBindings{materials=resolved.Values.OrderBy(m=>m.importedPath).ToArray()},true)+"\n");
        AssetDatabase.ImportAsset(Folder+"ResolvedMaterialBindings.json");
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        return "Fireball-only source material-family prototype installed: original cutout cores, active particle tint/multiplier/noise controls, transparent smoke and multiply material. Frostbolt and source timing/PS/audio unchanged; compiled kernel equations remain compatibility reconstruction.";
    }
}
