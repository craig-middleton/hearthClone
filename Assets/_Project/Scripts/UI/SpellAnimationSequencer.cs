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
        // target Transform, by contrast, is a persistent view (FaceView or MinionView)
        // and is safe to track live.
        public void PlayTravelAndReaction(Vector3 sourceWorldPosition, Transform targetView, bool isDamage)
        {
            if (targetView == null) return;
            StartCoroutine(TravelAndReactRoutine(sourceWorldPosition, targetView, isDamage));
        }

        private IEnumerator TravelAndReactRoutine(Vector3 sourceWorldPosition, Transform targetView, bool isDamage)
        {
            GameObject projectile = SpawnProjectile(sourceWorldPosition, targetView);
            RectTransform projectileRect = (RectTransform)projectile.transform;

            Vector3 lastKnownTargetPosition = targetView.position;

            float elapsed = 0f;
            while (elapsed < travelDuration)
            {
                elapsed += Time.deltaTime;
                float t = travelCurve.Evaluate(Mathf.Clamp01(elapsed / travelDuration));

                // A lethal hit kills its target the same tick this coroutine starts, and
                // AfterGameAction's RefreshAll() destroys and rebuilds the MinionView (and
                // its Transform) that same frame - so targetView can already be gone by the
                // next yield. `!= null` here is Unity's overloaded check, which correctly
                // detects a destroyed-but-not-yet-nulled Object; falling back to the last
                // sampled position lets the projectile finish its travel instead of throwing.
                // FaceView is never destroyed by a refresh (RefreshFaceDisplay only calls
                // SetPlayer on the existing instance), so this branch is minion-only in
                // practice - face targets keep swaying for the full travel even on a kill.
                if (targetView != null)
                {
                    lastKnownTargetPosition = targetView.position;
                }

                // Resampled every frame rather than lerped once source->fixed-target, since
                // targetView (FaceView) has its own idle sway running concurrently.
                projectileRect.position = Vector3.Lerp(sourceWorldPosition, lastKnownTargetPosition, t);
                yield return null;
            }

            Destroy(projectile);

            // A destroyed target has nothing left to react - skip rather than chase it.
            if (isDamage && targetView != null)
            {
                FaceView faceView = targetView.GetComponent<FaceView>();
                if (faceView != null)
                {
                    faceView.PlayDamageReaction();
                }
                else
                {
                    MinionView minionView = targetView.GetComponent<MinionView>();
                    if (minionView != null)
                    {
                        minionView.PlayDamageReaction();
                    }
                }
                // No heal/buff reaction yet - there's no CardEffect subtype for it to key
                // off (GainManaEffect is self-targeted mana gain, not a heal).
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
