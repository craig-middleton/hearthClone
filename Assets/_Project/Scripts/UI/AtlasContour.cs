using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    /// <summary>
    /// Cuts an opaque exported sprite by its painted outline rather than by colour.
    /// Points follow the sprite's normalized UV rectangle and must be visible from its centre.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("UI/Atlas Contour")]
    public class AtlasContour : BaseMeshEffect
    {
        public Vector2[] outline;

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || outline == null || outline.Length < 3) return;
            var image = (Image)graphic;
            if (image.sprite == null) return;
            Rect rect = image.GetPixelAdjustedRect();
            Vector4 uv = DataUtility.GetOuterUV(image.sprite);
            vertices.Clear();
            AddVertex(vertices, rect, uv, new Vector2(.5f, .5f), image.color);
            foreach (Vector2 point in outline) AddVertex(vertices, rect, uv, point, image.color);
            for (int i = 1; i <= outline.Length; i++)
                vertices.AddTriangle(0, i, i == outline.Length ? 1 : i + 1);
        }

        static void AddVertex(VertexHelper vertices, Rect rect, Vector4 uv, Vector2 point, Color color)
        {
            vertices.AddVert(new Vector2(rect.xMin + point.x * rect.width, rect.yMin + point.y * rect.height),
                color, new Vector2(Mathf.Lerp(uv.x, uv.z, point.x), Mathf.Lerp(uv.y, uv.w, point.y)));
        }
    }
}
