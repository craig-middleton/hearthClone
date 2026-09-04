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

        // Board-wide sweep (Blizzard) - shares CreateFrostBurst's color/diamond-rotation/
        // shrink-to-fade identity, but the shape/emission are built for TRAVELING across a
        // region rather than bursting outward from one point: continuous rateOverTime (not a
        // single Burst) so a steady stream trails the whole way, and a thin vertical Box shape
        // (not a Circle) so particles emit along a line the caller then translates left-to-right
        // over `duration` (see SpellAnimationSequencer.BoardSweepRoutine) - the moving Box IS
        // the sweep; this factory only builds what emits from wherever it's currently parented.
        public static ParticleSystem CreateFrostSweep(Transform parent, float regionHeight, float duration)
        {
            ParticleSystem particles = CreateBase("FrostSweep", parent);

            var main = particles.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            // Slower and more radial-jitter than a directed burst - the sweep's own translation
            // (driven by the caller moving this system's transform) supplies the actual leftward-
            // to-rightward motion, so particles themselves only need a gentle outward drift.
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startRotation = new ParticleSystem.MinMaxCurve(30f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);
            main.gravityModifier = 0.15f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = particles.emission;
            // Continuous, not a Burst - a moving Burst-only emitter would leave visible gaps
            // between spawn points as it translates; a steady rate keeps the trail unbroken.
            emission.rateOverTime = 40f;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            // Thin in X (a line, not an area) and tall enough in Y to span the full captured
            // region height regardless of board size - the caller supplies the actual height so
            // this always covers whatever's currently in view, never clipping top/bottom rows.
            shape.scale = new Vector3(0.05f, Mathf.Max(0.1f, regionHeight), 1f);

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(0.75f, 0.9f, 1f), 0.5f),
                    new GradientColorKey(new Color(0.55f, 0.78f, 1f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

            ApplyUnlitMaterial(particles);
            particles.Play();
            return particles;
        }

        public static ParticleSystem CreateArcaneBurst(Transform parent)
        {
            ParticleSystem particles = CreateBase("ArcaneBurst", parent);

            var main = particles.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.5f;
            // Fewer, larger, slower motes than Fire's chaotic burst - Arcane reads as
            // deliberate/magical rather than explosive, and the swirl (Noise module below) is
            // what should read as its distinct identity, not raw particle count.
            main.startSpeed = 1f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.gravityModifier = 0f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(12f, 18f)) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            // The one school that should visibly curl/spiral rather than burst in straight
            // lines like Fire/Frost - turbulence via the Noise module is the distinguishing
            // visual identity here, not color alone.
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 1.2f;
            noise.frequency = 0.8f;
            noise.scrollSpeed = 1f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.95f, 0.85f, 1f), 0f),
                    new GradientColorKey(new Color(0.65f, 0.25f, 0.95f), 0.4f),
                    new GradientColorKey(new Color(0.85f, 0.2f, 0.6f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            ApplyUnlitMaterial(particles);
            particles.Play();
            return particles;
        }

        public static ParticleSystem CreateFrostBurst(Transform parent)
        {
            ParticleSystem particles = CreateBase("FrostBurst", parent);

            var main = particles.main;
            main.duration = 0.9f;
            main.loop = false;
            // Longer than Fire's 0.5s and Arcane's 0.5s - shards should visibly hang and
            // drift rather than vanish quickly, per the "fading sparkle" finish spec.
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            // Diamond silhouette without a custom mesh/texture asset: the renderer's default
            // quad (no Sprite assigned, same as Fire/Arcane) reads as a square at rotation 0 -
            // locking rotation near 45 degrees turns that same quad into a diamond, staying
            // inside the procedural-only discipline CreateBase/ApplyUnlitMaterial already
            // established (no new asset pipeline for a custom shard mesh).
            main.startRotation = new ParticleSystem.MinMaxCurve(30f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);
            // Low, positive (downward) gravity - shards hang near their burst point and settle
            // slightly as they fade, unlike Fire's upward drift or Arcane's zero-gravity float.
            main.gravityModifier = 0.15f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(16f, 22f)) });

            // Circle shape + no directional cone = radial burst outward in every direction,
            // distinct from Fire's narrow upward cone.
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(0.75f, 0.9f, 1f), 0.5f),
                    new GradientColorKey(new Color(0.55f, 0.78f, 1f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            // Shrinks further and steeper than Fire/Arcane (down to 0.1x, not 0.2x/0.3x) -
            // the "fading sparkle" finish, shards visibly dwindling rather than just dimming.
            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

            ApplyUnlitMaterial(particles);
            particles.Play();
            return particles;
        }

        public static ParticleSystem CreateNatureBurst(Transform parent)
        {
            ParticleSystem particles = CreateBase("NatureBurst", parent);

            var main = particles.main;
            main.duration = 0.9f;
            main.loop = false;
            // Longest lifetime and lowest speed of the four schools - Nature is the "softest"
            // burst, particles should drift into place rather than fly outward.
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            // Positive gravity (unlike Fire's negative/upward or Arcane's zero) - leaves/spores
            // settle downward, the opposite drift direction from Fire's embers.
            main.gravityModifier = 0.25f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(14f, 20f)) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            // Gentle sideways sway rather than Arcane's stronger curling turbulence - low
            // strength/frequency so it reads as a light drift, not a swirl.
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.5f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.8f, 1f, 0.5f), 0f),
                    new GradientColorKey(new Color(0.45f, 0.8f, 0.25f), 0.5f),
                    new GradientColorKey(new Color(0.2f, 0.5f, 0.15f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.25f));

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
