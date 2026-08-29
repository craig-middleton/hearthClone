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

        void Awake()
        {
            if (burstCamera == null || burstSpawnPoint == null || displayImage == null)
            {
                Debug.LogWarning("UIParticleBurstRenderer: 'burstCamera', 'burstSpawnPoint' or 'displayImage' not assigned - bursts will not render.", this);
                return;
            }

            // Depth/stencil is otherwise unused (a 2D burst needs no depth-testing), but the
            // render-graph path in this project's URP version refuses a camera targetTexture
            // whose RenderTextureDescriptor has depthStencilFormat left at None - it warns
            // ("output Render Texture must have a depth buffer") on every frame that camera
            // renders, not just once. SystemInfo.GetGraphicsFormat picks whatever depth/stencil
            // format this platform's pipeline actually supports, rather than hardcoding one
            // (e.g. D24_UNorm) that might not match.
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(textureSize, textureSize)
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

            displayRect = displayImage.rectTransform;
            canvasRect = displayRect.parent as RectTransform;
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

            return burstSpawnPoint;
        }
    }
}
