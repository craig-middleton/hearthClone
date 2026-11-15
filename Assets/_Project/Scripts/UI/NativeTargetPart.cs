using UnityEngine;
using UnityEngine.UI;
namespace HearthstoneClone.UI
{
    public class NativeTargetPart : MaskableGraphic
    {
        public Vector2[] points;
        public Vector2[] uv;
        public int[] triangles;
        public Texture paint;
        public override Texture mainTexture => paint;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if(points==null)return;
            for(int i=0;i<points.Length;i++) vh.AddVert(points[i],color,uv[i]);
            for(int i=0;i<triangles.Length;i+=3)vh.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
        }
    }
}
