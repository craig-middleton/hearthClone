using UnityEngine;
using UnityEngine.UI;
namespace HearthstoneClone.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class NativeMulliganPart : MaskableGraphic
    {
        public NativeMulliganArt library;
        public string shape;
        public override Texture mainTexture=>library!=null&&library.Get(shape)!=null?library.Get(shape).paint:base.mainTexture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(library==null)return;var data=library.Get(shape);if(data==null)return;
            var rect=rectTransform.rect;
            for(int i=0;i<data.points.Length;i++)vh.AddVert(rect.center+Vector2.Scale(data.points[i],rect.size),color,data.uv[i]);
            for(int i=0;i<data.triangles.Length;i+=3)vh.AddTriangle(data.triangles[i],data.triangles[i+1],data.triangles[i+2]);
        }
    }
}
