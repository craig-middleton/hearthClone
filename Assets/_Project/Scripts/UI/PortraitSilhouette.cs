using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    /// <summary>Stencil geometry for card ovals and arched hero portraits.</summary>
    [AddComponentMenu("UI/Portrait Silhouette")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class PortraitSilhouette : MaskableGraphic
    {
        public bool arch;
        public bool rectangle;
        [Tooltip("Optional convex outline in normalized 0..1 coordinates. Leaves existing oval/arch masks unchanged when empty.")]
        public Vector2[] outline;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (outline != null && outline.Length >= 3)
            {
                vh.AddVert(r.center, color, Vector2.zero);
                foreach (Vector2 p in outline)
                    vh.AddVert(new Vector2(r.xMin + p.x * r.width, r.yMin + p.y * r.height), color, Vector2.zero);
                for (int i = 1; i <= outline.Length; i++)
                    vh.AddTriangle(0, i, i == outline.Length ? 1 : i + 1);
                return;
            }
            if (rectangle)
            {
                vh.AddVert(new Vector2(r.xMin,r.yMin),color,Vector2.zero);
                vh.AddVert(new Vector2(r.xMax,r.yMin),color,Vector2.zero);
                vh.AddVert(new Vector2(r.xMax,r.yMax),color,Vector2.zero);
                vh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);
                vh.AddTriangle(0,1,2); vh.AddTriangle(2,3,0); return;
            }
            Vector2 center = r.center;
            vh.AddVert(center, color, Vector2.zero);
            const int steps = 64;
            if (arch)
            {
                vh.AddVert(new Vector2(r.xMin, r.yMin), color, Vector2.zero);
                vh.AddVert(new Vector2(r.xMax, r.yMin), color, Vector2.zero);
                for (int i = 0; i <= steps; i++)
                {
                    float angle = Mathf.PI * i / steps;
                    vh.AddVert(center + new Vector2(Mathf.Cos(angle) * r.width * .5f,
                        Mathf.Sin(angle) * r.height * .5f), color, Vector2.zero);
                }
            }
            else
            {
                for (int i = 0; i < steps; i++)
                {
                    float angle = 2 * Mathf.PI * i / steps;
                    vh.AddVert(center + new Vector2(Mathf.Cos(angle) * r.width * .5f,
                        Mathf.Sin(angle) * r.height * .5f), color, Vector2.zero);
                }
            }
            int count = vh.currentVertCount - 1;
            for (int i = 1; i <= count; i++) vh.AddTriangle(0, i, i == count ? 1 : i + 1);
        }
    }
}
