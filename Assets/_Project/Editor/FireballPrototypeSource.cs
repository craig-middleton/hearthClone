using System;
using UnityEngine;

// Stable Editor-assembly DTOs avoid transient-script JsonUtility type discovery.
[Serializable] public sealed class FireballPrototypeSource
{
    public FireballPrototypeSourceMaterial[] materials;
}
[Serializable] public sealed class FireballPrototypeSourceMaterial
{
    public string name, guid, sourceShader, sourceSha256;
    public FireballPrototypeFloat[] floats;
    public FireballPrototypeColor[] colors;
    public FireballPrototypeTexture[] textures;
    public string[] activeProperties;
}

[Serializable] public sealed class FireballPrototypeFloat { public string name; public float value; }
[Serializable] public sealed class FireballPrototypeColor { public string name; public Color value; }
[Serializable] public sealed class FireballPrototypeTexture
{
    public string name, textureName, sha256;
    public long builtinId;
    public Vector2 scale, offset;
}

[Serializable] public sealed class FireballPrototypeResolvedBindings
{
    public FireballPrototypeResolvedMaterial[] materials;
}
[Serializable] public sealed class FireballPrototypeResolvedMaterial
{
    public string importedPath, importedGuid, sourceGuid, sourceShader, sourceSha256;
    public bool matchedByValidatedName;
}
