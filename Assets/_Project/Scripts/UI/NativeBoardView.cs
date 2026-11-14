using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    /// <summary>Displays the independently authored board meshes behind the existing interactive UI.</summary>
    [ExecuteAlways]
    public sealed class NativeBoardView : MonoBehaviour
    {
        public RawImage display;
        public Camera boardCamera;
        public Transform stage;
        public GameObject[] replacedDecoration;
        RenderTexture texture;

        void OnEnable()
        {
            if (replacedDecoration != null) foreach (var decoration in replacedDecoration)
                if (decoration != null) decoration.SetActive(false);
            Prepare();
        }
        void LateUpdate() { Prepare(); }

        void Prepare()
        {
            if (display == null || boardCamera == null || stage == null) return;
            // A screen-space canvas changes world scale with the window. The capture stage must not.
            Vector3 parentScale = transform.lossyScale;
            stage.localScale = new Vector3(1 / parentScale.x, 1 / parentScale.y, 1 / parentScale.z);
            stage.position = new Vector3(12000, 12000, 12000);
            stage.rotation = Quaternion.identity;
            if (texture == null)
            {
                texture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Native board capture",
                    antiAliasing = 4,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.Create();
                display.texture = texture;
                boardCamera.targetTexture = texture;
            }
            boardCamera.aspect = 16f / 9f;
            boardCamera.enabled = true;
        }

        void OnDisable()
        {
            if (replacedDecoration != null) foreach (var decoration in replacedDecoration)
                if (decoration != null) decoration.SetActive(true);
            if (boardCamera != null) { boardCamera.enabled = false; boardCamera.targetTexture = null; }
            if (display != null) display.texture = null;
            if (texture == null) return;
            texture.Release();
            if (Application.isPlaying) Destroy(texture); else DestroyImmediate(texture);
            texture = null;
        }
    }
}
