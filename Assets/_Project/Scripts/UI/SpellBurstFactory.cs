using UnityEngine;

namespace HearthstoneClone.UI
{
    // Builds each school's particle burst procedurally at runtime rather than as a
    // hand-authored ParticleSystem prefab asset - a ParticleSystem's many modules are exactly
    // the kind of thing that's easy to get subtly wrong hand-serializing, the same caution
    // Live Constraint 20 (PROJECT_STATUS) raised for ProjectSettings YAML. Scripting it via the
    // public API instead means every school shares one discipline instead of each re-deriving
    // it: AddComponent<ParticleSystem>() defaults to Play On Awake, which starts the system
    // simulating synchronously before any configuration code runs - setting main-module fields
    // (e.g. main.duration) on an already-playing system throws ("Setting the duration while
    // system is still playing is not supported"). Every builder routes through CreateBase,
    // which stops and disables playOnAwake before handing back a system safe to configure.
    public static class SpellBurstFactory
    {
        public static ParticleSystem CreateFireBurst(Transform parent)
        {
            ParticleSystem particles = CreateBase("FireBurst", parent);

            var main = particles.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 2.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            // Negative gravity drifts embers upward instead of falling - no real gravity needed
            // for a screen-space UI burst, just enough pull to bend the burst's tail.
            main.gravityModifier = -0.3f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(25f, 35f)) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.05f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.05f), 0.4f),
                    new GradientColorKey(new Color(0.4f, 0.05f, 0.02f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            ApplyUnlitMaterial(particles);
            particles.Play();
            return particles;
        }

        private static ParticleSystem CreateBase(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(ParticleSystem));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;

            ParticleSystem particles = go.GetComponent<ParticleSystem>();
            EnsureStoppedForConfiguration(particles);
            return particles;
        }

        private static void EnsureStoppedForConfiguration(ParticleSystem particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
        }

        private static void ApplyUnlitMaterial(ParticleSystem particles)
        {
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("SpellBurstFactory: no compatible unlit particle shader found.");
                return;
            }
            renderer.material = new Material(shader);
        }
    }
}
