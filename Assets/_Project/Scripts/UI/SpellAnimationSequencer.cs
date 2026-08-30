using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HearthstoneClone.Cards;

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

        [Header("Spell VFX")]
        public UIParticleBurstRenderer burstRenderer;

        // Takes a plain world position (not a Transform) for the source, since the CardView
        // that position was read from may already be destroyed by the time this coroutine
        // runs - HandDisplay.RenderHand rebuilds synchronously right after PlayCard. The
        // target Transform, by contrast, is a persistent view (FaceView or MinionView)
        // and is safe to track live.
        public void PlayTravelAndReaction(Vector3 sourceWorldPosition, Transform targetView, bool isDamage, SpellSchool school)
        {
            if (targetView == null) return;
            StartCoroutine(TravelAndReactRoutine(sourceWorldPosition, targetView, isDamage, school));
        }

        private IEnumerator TravelAndReactRoutine(Vector3 sourceWorldPosition, Transform targetView, bool isDamage, SpellSchool school)
        {
            // A lethal hit already removed this Minion from the model synchronously, in
            // PlayerHand.PlayCard, before this coroutine started - so Minion.IsDead here
            // reflects the kill this same spell is about to animate. Holding keeps
            // BoardDisplay.RenderBoard from destroying the view out from under this sequence
            // (Constraint 15/16's whole problem). Only held when actually dead: holding a
            // still-alive minion's view would make RenderBoard's rebuild loop spawn a second,
            // fresh view for the same (still-present) Minion while the held one lingers too -
            // a visible duplicate. A non-lethal hit needs no hold; its view was never at risk.
            MinionView heldMinionView = null;
            if (isDamage && targetView != null)
            {
                MinionView candidateView = targetView.GetComponent<MinionView>();
                if (candidateView != null && candidateView.Minion != null && candidateView.Minion.IsDead)
                {
                    heldMinionView = candidateView;
                    heldMinionView.BeginHold(travelDuration + heldMinionView.reactionDuration + 1f);
                }
            }

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

            // Purely additive on top of the flash reaction above, not a replacement - the
            // burst is visual flavor keyed off the card's school, independent of whatever
            // state PlayDamageReaction encodes. Uses lastKnownTargetPosition rather than
            // chasing a possibly-destroyed targetView.
            PlaySchoolBurst(school, lastKnownTargetPosition);

            // The hold kept BoardDisplay.RenderBoard from destroying this view earlier -
            // nothing else will ever destroy it now (a held view is skipped on every refresh
            // until EndHold), so this sequence owns doing it explicitly once it's done with
            // it. The kill already happened (Board.RemoveDeadMinions ran synchronously back
            // in PlayerHand.PlayCard); this is only the deferred view cleanup, not a deferred
            // kill.
            if (heldMinionView != null)
            {
                heldMinionView.EndHold();
                Destroy(heldMinionView.gameObject);
            }
        }

        // Fire and Arcane are wired up (spell VFX plan steps 2-3) - Frost/Nature fall through
        // the switch as a deliberate no-op until their factory methods exist (step 4).
        private void PlaySchoolBurst(SpellSchool school, Vector3 screenPosition)
        {
            if (burstRenderer == null) return;

            // GameCanvas is Screen Space - Overlay, where a UI RectTransform's world .position
            // already *is* the screen-space pixel position (no camera transform involved) -
            // the same value SpawnProjectile above assigns straight to projectileRect.position.
            // Converting it again through a camera (e.g. Camera.main.WorldToScreenPoint) would
            // double-transform it.
            Transform spawnPoint = burstRenderer.ShowAt(screenPosition);

            switch (school)
            {
                case SpellSchool.Fire:
                    SpellBurstFactory.CreateFireBurst(spawnPoint);
                    break;
                case SpellSchool.Arcane:
                    SpellBurstFactory.CreateArcaneBurst(spawnPoint);
                    break;
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
