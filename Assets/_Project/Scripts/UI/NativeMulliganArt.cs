using System;
using UnityEngine;
namespace HearthstoneClone.UI
{
    [Serializable] public class NativeMulliganShape
    {
        public string key,source;
        public Vector2[] points,uv;
        public int[] triangles;
        public Texture paint;
        public float aspect;
    }
    public class NativeMulliganArt : ScriptableObject
    {
        public NativeMulliganShape[] shapes;
        public NativeMulliganShape Get(string key)=>Array.Find(shapes,s=>s.key==key);
    }
}
