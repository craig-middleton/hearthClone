using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    // Geometry/UVs are exported from individual source meshes, not a complete card bitmap.
    [RequireComponent(typeof(CanvasRenderer))]
    public class NativeCardLayer : MaskableGraphic
    {
        public NativeCardLibrary library;
        public bool spell;
        public int layerIndex;
        public Sprite artwork;
        public Texture nameTexture;
        public NativeCardMesh Definition => (spell?library.spell:library.minion)[layerIndex];
        public override Texture mainTexture => Definition.role==NativeCardRole.Artwork&&artwork!=null?artwork.texture:
            Definition.role==NativeCardRole.Name&&nameTexture!=null?nameTexture:Definition.paint;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(library==null)return;
            var data=Definition;var rect=rectTransform.rect;
            bool hasSecondary=data.secondaryUV!=null&&data.secondaryUV.Length==data.points.Length;
            bool hasNormals=data.sourceNormals!=null&&data.sourceNormals.Length==data.points.Length;
            float scale=Mathf.Min(rect.width/180,rect.height/270);
            Vector4 crop=artwork!=null?DataUtility.GetOuterUV(artwork):new Vector4(0,0,1,1);
            for(int i=0;i<data.points.Length;i++)
            {
                var uv=data.uv[i];
                if(data.role==NativeCardRole.Artwork)uv=new Vector2(Mathf.Lerp(crop.x,crop.z,uv.x),Mathf.Lerp(crop.y,crop.w,uv.y));
                var vertex=UIVertex.simpleVert;
                vertex.position=rect.center+data.points[i]*scale;vertex.color=color;vertex.uv0=uv;
                vertex.uv1=hasSecondary?data.secondaryUV[i]:Vector2.zero;
                vertex.normal=hasNormals?data.sourceNormals[i]:Vector3.back;
                vh.AddVert(vertex);
            }
            for(int i=0;i<data.triangles.Length;i+=3)vh.AddTriangle(data.triangles[i],data.triangles[i+1],data.triangles[i+2]);
        }
    }
}
