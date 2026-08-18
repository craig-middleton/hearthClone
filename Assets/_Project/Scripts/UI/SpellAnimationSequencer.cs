using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    // Vertical slice: travel effect (caster -> target) + target reaction only.
    // Card lift/zoom and discard-off are separate, later pieces - see PROJECT_STATUS.
    public class SpellAnimationSequencer : MonoBehaviour
    {
        [Header("Travel")]
        public float travelDuration = 0.35f;
        public AnimationCurve travelCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public Color projectileColor = Color.white;
        public Vector2 projectileSize = new Vector2(24, 24);

        // Takes a plain world position (not a Transform) for the source, since the CardView
        // that position was read from may already be destroyed by the time this coroutine
        // runs - HandDisplay.RenderHand rebuilds synchronously right after PlayCard. The
        // target Transform, by contrast, is a persistent view (FaceView today; a future
        // MinionView once minion targeting exists) and is safe to track live.
        public void PlayTravelAndReaction(Vector3 sourceWorldPosition, Transform targetView, bool isDamage)
        {
            if (targetView == null) return;
            StartCoroutine(TravelAndReactRoutine(sourceWorldPosition, targetView, isDamage));
        }

        private IEnumerator TravelAndReactRoutine(Vector3 sourceWorldPosition, Transform targetView, bool isDamage)
        {
            GameObject projectile = SpawnProjectile(sourceWorldPosition, targetView);
            RectTransform projectileRect = (RectTransform)projectile.transform;

            float elapsed = 0f;
            while (elapsed < travelDuration)
            {
                elapsed += Time.deltaTime;
                float t = travelCurve.Evaluate(Mathf.Clamp01(elapsed / travelDuration));
                // Resampled every frame rather than lerped once source->fixed-target, since
                // targetView (FaceView) has its own idle sway running concurrently.
                projectileRect.position = Vector3.Lerp(sourceWorldPosition, targetView.position, t);
                yield return null;
            }

            Destroy(projectile);

            if (isDamage)
            {
                FaceView faceView = targetView.GetComponent<FaceView>();
                if (faceView != null)
                {
                    faceView.PlayDamageReaction();
                }
                // No MinionView case yet - spells can't target minions today (see
                // EffectTester.ResolveViewTransform). No heal/buff reaction yet either -
                // there's no CardEffect subtype for it to key off (GainManaEffect is
                // self-targeted mana gain, not a heal).
            }
        }

        private GameObject SpawnProjectile(Vector3 sourceWorldPosition, Transform targetView)
        {
            GameObject projectile = new GameObject(
                "SpellProjectile",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            Canvas canvas = targetView.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : targetView;
            projectile.transform.SetParent(parent, worldPositionStays: true);

            RectTransform rect = (RectTransform)projectile.transform;
            rect.sizeDelta = projectileSize;
            rect.position = sourceWorldPosition;

            Image image = projectile.GetComponent<Image>();
            image.color = projectileColor;
            image.raycastTarget = false;

            return projectile;
        }
    }
}
