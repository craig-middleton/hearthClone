using System;
using HearthstoneClone.Cards;
using UnityEngine;

namespace HearthstoneClone.UI
{
    public enum NativeCardRole { Paint, Artwork, Name, Cost, Attack, Health, Description, School }
    [Serializable] public class NativeCardMesh
    {
        public string source;
        public NativeCardRole role;
        public Vector2[] points, uv;
        public Vector2[] secondaryUV;
        public Vector3[] sourceNormals;
        public Material sourceMaterial;
        public int[] triangles;
        public Texture paint;
    }
    [Serializable] public class NativeCardName
    {
        public CardData card;
        public Texture texture;
    }
    public class NativeCardLibrary : ScriptableObject
    {
        public NativeCardMesh[] minion, spell;
        public NativeCardName[] names;
        public Texture NameFor(CardData card)
        {
            foreach(var item in names)if(item.card==card)return item.texture;
            return null;
        }
    }
}
