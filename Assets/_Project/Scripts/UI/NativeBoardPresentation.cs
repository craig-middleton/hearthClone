using UnityEngine;

namespace HearthstoneClone.UI
{
    // Lives inside the match UI; the native hierarchy stays in world space.
    [ExecuteAlways]
    public class NativeBoardPresentation : MonoBehaviour
    {
        public GameObject boardRoot;
        public Camera boardCamera;
        public const float NativeVerticalFieldOfView = 34.870445f;
        public const float ReferenceAspect = 16f / 9f;

        void OnEnable()
        {
            if (boardRoot != null) boardRoot.SetActive(true);
            FitCamera();
        }

        void OnDisable()
        {
            if (boardRoot != null) boardRoot.SetActive(false);
        }

        void LateUpdate() => FitCamera();

        public void FitCamera()
        {
            if (boardCamera == null) return;
            boardCamera.ResetAspect();
            // Match the overlay CanvasScaler's Expand policy at narrow window sizes.
            float widthFit = Mathf.Max(1f, ReferenceAspect / boardCamera.aspect);
            boardCamera.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(
                NativeVerticalFieldOfView * Mathf.Deg2Rad * .5f) * widthFit) * Mathf.Rad2Deg;
        }
    }
}
