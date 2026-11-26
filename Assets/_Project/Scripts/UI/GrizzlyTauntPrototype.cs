using System.Linq;
using UnityEngine;

namespace HearthstoneClone.UI
{
    // Runs after NativeMinionPresentation creates and positions its original model.
    [DefaultExecutionOrder(150)]
    public sealed class GrizzlyTauntPrototype : MonoBehaviour
    {
        public GrizzlyTauntPrototypeLibrary library;
        MinionView view;
        NativeMinionPresentation native;
        GrizzlyTauntPrototypeDirector director;
        Transform shield;
        Renderer shieldRenderer;
        Transform attackGem, healthGem;
        Vector3 restingPosition, restingScale;
        Quaternion restingRotation;
        bool restingVisible;
        public bool IsPrototypeActive { get; private set; }
        public float SourceAge { get; private set; }

        void Awake()
        {
            view = GetComponent<MinionView>();
            native = GetComponent<NativeMinionPresentation>();
        }
        void LateUpdate()
        {
            var minion = view != null ? view.Minion : null;
            bool matches = library != null && minion != null && minion.HasTaunt &&
                minion.MinionName == "Ironfur Grizzly" && minion.Artwork == library.grizzlyArtwork;
            if (!matches && IsPrototypeActive && native != null && native.actor != null && shield != null)
            {
                // Direct SetMinion rebinding must not leave a non-study card in
                // the source birth pose, even if future views are pooled.
                native.actor.localPosition = restingPosition;
                native.actor.localRotation = restingRotation;
                shield.localScale = restingScale;
                shieldRenderer.enabled = restingVisible;
            }
            IsPrototypeActive = matches;
            if (!IsPrototypeActive || native == null || native.taunt == null) return;
            if (director == null)
                director = native.stage.GetComponentInParent<GrizzlyTauntPrototypeDirector>();
            if (director == null) return;
            if (shield == null)
            {
                shield = native.taunt.GetComponentsInChildren<Transform>(true)
                    .Single(t => t.name == "Taunt_Shield_mesh");
                shieldRenderer = shield.GetComponent<Renderer>();
                restingPosition = native.actor.localPosition;
                restingRotation = native.actor.localRotation;
                restingScale = shield.localScale;
                restingVisible = shieldRenderer.enabled;
            }
            SourceAge = director.Observe(minion, library);
            ApplyOriginalSpawn(Mathf.Min(SourceAge, library.spawnDuration));
            float progress = (SourceAge - library.idleDelay) / library.scaleDuration;
            shieldRenderer.enabled = progress >= 0;
            float scale = progress >= 1 ? library.settledScale :
                Mathf.LerpUnclamped(library.initialScale, library.settledScale, SourceSpring(progress));
            shield.localScale = Vector3.one * scale;
        }

        void ApplyOriginalSpawn(float time)
        {
            if (library.spawnChannels == null || library.spawnChannels.Length == 0) return;
            var position = Vector3.zero;
            var rotation = Quaternion.identity;
            foreach (var channel in library.spawnChannels)
            {
                string property = channel.property;
                int axis = property.EndsWith("x") ? 0 : property.EndsWith("y") ? 1 : property.EndsWith("w") ? 3 : 2;
                if (property.StartsWith("m_LocalPosition.")) position[axis] = channel.curve.Evaluate(time);
                else if (property.StartsWith("m_LocalRotation.")) rotation[axis] = channel.curve.Evaluate(time);
            }
            native.actor.localPosition = position;
            native.actor.localRotation = rotation.normalized;
            // Base adapter projects labels before our later source motion. Update
            // them in this frame so original gems and live numbers travel together.
            if (attackGem == null) attackGem = native.actor.Find("RootObject/AttackObject/Gem_Attack");
            if (healthGem == null) healthGem = native.actor.Find("RootObject/HealthObject/Gem_Health");
            PlaceLabel(view.attackText, attackGem);
            PlaceLabel(view.healthText, healthGem);
        }
        void PlaceLabel(TMPro.TMP_Text label, Transform gem)
        {
            if (label != null && gem != null)
                label.rectTransform.position = native.boardCamera.WorldToScreenPoint(gem.GetComponent<Renderer>().bounds.center);
        }

        // Exact installed Blizzard.T5.iTween::spring IL. Scale application snaps
        // to its target on completion separately, matching original iTween.
        public static float SourceSpring(float progress)
        {
            float t = Mathf.Clamp01(progress);
            return (Mathf.Sin(t * Mathf.PI * (.2f + 2.5f * t * t * t)) *
                    Mathf.Pow(1 - t, 2.2f) + t) * (1 + 1.2f * (1 - t));
        }
    }
}
