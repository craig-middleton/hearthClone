using System;

namespace HearthstoneClone.UI
{
    [Serializable] public sealed class NativeMenuSourceConfiguration
    {
        public NativeMenuSourceMaterial[] materials;
    }
    [Serializable] public sealed class NativeMenuSourceMaterial
    {
        public string guid, name, sourceShader;
    }
}
