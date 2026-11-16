using UnityEngine;
using UnityEngine.UI;
namespace HearthstoneClone.UI
{
    // Original ManaGem top-surface topology/UVs, fitted into the existing UI tray.
    public class NativeManaCrystal : Image
    {
        public Mesh sourceMesh;
        public Texture paint;
        public override Texture mainTexture => paint!=null?paint:base.mainTexture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(sourceMesh==null)return;
            var points=sourceMesh.vertices;var uv=sourceMesh.uv;var b=sourceMesh.bounds;var rect=rectTransform.rect;
            float scale=Mathf.Min(rect.width/b.size.x,rect.height/b.size.z);
            for(int i=0;i<points.Length;i++)vh.AddVert(rect.center+new Vector2(points[i].x-b.center.x,points[i].z-b.center.z)*scale,color,uv[i]);
            var triangles=sourceMesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],c=triangles[i+1],d=triangles[i+2];
                if(Vector3.Cross(points[c]-points[a],points[d]-points[a]).y>0)vh.AddTriangle(a,c,d);
            }
        }
    }
}
