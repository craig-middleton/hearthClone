using UnityEngine;
using UnityEngine.UI;
namespace HearthstoneClone.UI
{
    // Same recovered source vertices/UVs as the world crack quad, projected over
    // the overlay hero. Preserve its authored vertex colors/edge alpha as well.
    public class NativeHeroCrackPart:NativeTargetPart
    {
        public Color[] sourceColors;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(points==null)return;
            for(int i=0;i<points.Length;i++)vh.AddVert(points[i],(sourceColors!=null&&sourceColors.Length==points.Length?sourceColors[i]:Color.white)*color,uv[i]);
            for(int i=0;i<triangles.Length;i+=3)vh.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
        }
    }
}
