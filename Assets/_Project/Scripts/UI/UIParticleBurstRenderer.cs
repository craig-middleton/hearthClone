using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering;

namespace HearthstoneClone.UI
{
    // GameCanvas is Screen Space - Overlay, which only ever draws CanvasRenderer graphics
    // (Image, Text, ...) - a ParticleSystem's MeshRenderer can't composite with it directly,
    // and camera stacking doesn't help since an Overlay canvas always draws last regardless
    // of what any camera renders. The workaround: render the burst offscreen through a
    // dedicated camera into a RenderTexture, then display that texture on a RawImage that
    // lives inside GameCanvas like any other UI graphic - sorting then just follows normal
    // sibling order, same as everything else in the canvas.
    public class UIParticleBurstRenderer : MonoBehaviour
    {
        [Header("Offscreen Rendering")]
        public Camera burstCamera;
        public Transform burstSpawnPoint;
        public RawImage displayImage;

        [Header("Render Texture")]
        public int textureSize = 256;

        public Transform BurstSpawnPoint => burstSpawnPoint;

        private RenderTexture renderTexture;
        private RectTransform canvasRect;
        private RectTransform displayRect;

        // Snapshotted once in Awake, before ShowAtRegion ever runs - the reference point-burst
        // framing (size/orthographic-size/texture dimensions) that ShowAt restores on every
        // call. UIParticleBurstRenderer is one shared instance used by both ordinary point
        // bursts (Fire/Arcane/Frost) and board-wide sweeps (Blizzard) - without this restore,
        // a Blizzard cast would leave the renderer sized/framed for a board region, and the
        // NEXT ordinary spell burst would incorrectly render at board scale/aspect instead of
        // its normal small point-burst dimensions.
        private Vector2 originalDisplaySizeDelta;
        private float originalOrthographicSize;
        private int originalTextureSize;

        // World-space size of the last ShowAtRegion capture, at the same world-units-per-
        // canvas-unit ratio used to set orthographicSize - callers building a sweep effect
        // (which needs to know how far, in world units, "the full region width" actually is)
        // read these instead of re-deriving the ratio themselves.
        public float LastRegionWorldWidth { get; private set; }
        public float LastRegionWorldHeight { get; private set; }

        void Awake()
        {
            if (burstCamera == null || burstSpawnPoint == null || displayImage == null)
            {
                Debug.LogWarning("UIParticleBurstRenderer: 'burstCamera', 'burstSpawnPoint' or 'displayImage' not assigned - bursts will not render.", this);
                return;
            }

            displayRect = displayImage.rectTransform;
            canvasRect = displayRect.parent as RectTransform;

            originalDisplaySizeDelta = displayRect.sizeDelta;
            originalOrthographicSize = burstCamera.orthographicSize;
            originalTextureSize = textureSize;

            EnsureRenderTexture(textureSize, textureSize);
        }

        // Moves the display RawImage to sit over screenPosition and returns the transform
        // a burst's ParticleSystem should be parented to and played from. Callers own
        // instantiating/configuring/playing the actual ParticleSystem - this only owns
        // getting its render output onto the canvas in the right place.
        public Transform ShowAt(Vector2 screenPosition)
        {
            if (displayRect != null && canvasRect != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPoint))
            {
                displayRect.anchoredPosition = localPoint;
            }

            // Always restore point-burst framing, in case a prior ShowAtRegion call (a
            // Blizzard sweep) left size/orthographic-size/texture resized for a board region.
            RestorePointBurstFraming();

            return burstSpawnPoint;
        }

        // Resizes/repositions the display RawImage to cover screenBounds exactly (not just a
        // single point), widens the offscreen camera to match, and returns the same
        // burstSpawnPoint - callers (board-wide effects) spawn/play their ParticleSystem there
        // exactly as ShowAt's callers already do for point bursts.
        public Transform ShowAtRegion(Rect screenBounds)
        {
            if (displayRect == null || canvasRect == null || burstCamera == null) return burstSpawnPoint;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, new Vector2(screenBounds.xMin, screenBounds.yMin), null, out Vector2 minLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, new Vector2(screenBounds.xMax, screenBounds.yMax), null, out Vector2 maxLocal);

            Vector2 size = new Vector2(Mathf.Abs(maxLocal.x - minLocal.x), Mathf.Abs(maxLocal.y - minLocal.y));
            Vector2 center = (minLocal + maxLocal) * 0.5f;

            displayRect.sizeDelta = size;
            displayRect.anchoredPosition = center;

            // Preserve the original point-burst world-units-per-canvas-unit ratio, so a burst
            // authored (and tuned) in world units for the small point-burst case keeps the
            // same apparent on-screen scale now that it's captured over a much larger region -
            // only the region's own position/size/aspect changes here, not the burst itself.
            float worldUnitsPerCanvasUnit = originalDisplaySizeDelta.y > 0f
                ? (originalOrthographicSize * 2f) / originalDisplaySizeDelta.y
                : 0.01f;
            burstCamera.orthographicSize = Mathf.Max(0.01f, size.y * worldUnitsPerCanvasUnit * 0.5f);
            LastRegionWorldWidth = size.x * worldUnitsPerCanvasUnit;
            LastRegionWorldHeight = size.y * worldUnitsPerCanvasUnit;

            // Resize the render texture to match the region's aspect ratio (height pinned to
            // the authored textureSize, width scaled to fit) - a fixed SQUARE texture stretched
            // non-uniformly onto a wide board rect would distort every particle horizontally.
            float aspect = size.y > 0f ? size.x / size.y : 1f;
            int texHeight = originalTextureSize;
            int texWidth = Mathf.Max(1, Mathf.RoundToInt(texHeight * aspect));
            EnsureRenderTexture(texWidth, texHeight);

            return burstSpawnPoint;
        }

        private void RestorePointBurstFraming()
        {
            if (displayRect != null) displayRect.sizeDelta = originalDisplaySizeDelta;
            if (burstCamera != null) burstCamera.orthographicSize = originalOrthographicSize;
            EnsureRenderTexture(originalTextureSize, originalTextureSize);
        }

        // Depth/stencil is otherwise unused (a 2D burst needs no depth-testing), but the
        // render-graph path in this project's URP version refuses a camera targetTexture whose
        // RenderTextureDescriptor has depthStencilFormat left at None - it warns ("output
        // Render Texture must have a depth buffer") on every frame that camera renders, not
        // just once. SystemInfo.GetGraphicsFormat picks whatever depth/stencil format this
        // platform's pipeline actually supports, rather than hardcoding one (e.g. D24_UNorm)
        // that might not match. No-ops if a texture of the requested size already exists -
        // ShowAt calls this every point-burst to restore point-burst dimensions, so this must
        // stay cheap on the (overwhelmingly common) case where nothing actually changed.
        private void EnsureRenderTexture(int width, int height)
        {
            if (renderTexture != null && renderTexture.width == width && renderTexture.height == height) return;

            if (renderTexture != null) Destroy(renderTexture);

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height)
            {
                graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR),
                depthStencilFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil),
                msaaSamples = 1,
            };
            renderTexture = new RenderTexture(descriptor)
            {
                name = "SpellBurstRT"
            };
            burstCamera.targetTexture = renderTexture;
            displayImage.texture = renderTexture;
        }
    }
}
