using System;
using UnityEngine;

namespace HearthstoneClone.UI
{
    /// <summary>Math recovered from the original card-motion metadata; see Docs/Recovery/ORIGINAL_CARD_MOTION.md.</summary>
    public static class NativeCardMotionMath
    {
        // Original iTween easeOutExpo/easeInExpo have these nonzero endpoint residuals.
        // Their transform channels snap to the target separately when they complete.
        public static float OutExpo(float t) => 1f - Mathf.Pow(2f, -10f * t);
        public static float InExpo(float t) => Mathf.Pow(2f, 10f * (t - 1f));

        // The unknown-opponent draw uses powers 4 (position), 3 (rotation), and 5 (scale).
        public static float InOutPower(float t, int power)
        {
            if (power < 1) throw new ArgumentOutOfRangeException(nameof(power));
            return t < .5f
                ? .5f * Mathf.Pow(2f * t, power)
                : 1f - .5f * Mathf.Pow(2f - 2f * t, power);
        }

        /// <summary>Original open-path, uniform Catmull-Rom evaluation with reflected endpoint controls.</summary>
        public static Vector3 Spline(Vector3[] nodes, float parameter)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (nodes.Length < 2) throw new ArgumentException("A card-motion path needs at least two nodes.", nameof(nodes));

            // iTween.GenerateMoveToPathTargets pads the endpoints; CRSpline.Interp divides
            // normalized parameter evenly between segments, without arc-length resampling.
            float scaled = Mathf.Clamp01(parameter) * (nodes.Length - 1);
            int segment = Mathf.Min(Mathf.FloorToInt(scaled), nodes.Length - 2);
            float t = scaled - segment;
            Vector3 b = nodes[segment];
            Vector3 c = nodes[segment + 1];
            Vector3 a = segment == 0 ? 2f * b - c : nodes[segment - 1];
            Vector3 d = segment + 2 == nodes.Length ? 2f * c - b : nodes[segment + 2];

            return .5f * ((-a + 3f * b - 3f * c + d) * (t * t * t)
                + (2f * a - 5f * b + 4f * c - d) * (t * t)
                + (-a + c) * t + 2f * b);
        }

        /// <summary>Original shortest-axis Euler interpolation followed by Quaternion.Euler.</summary>
        public static Quaternion Euler(Vector3 from, Vector3 to, float progress)
        {
            // Source GenerateRotateToTargets prepares the closest angular endpoint for each
            // axis; ApplyRotateToTargets interpolates those axes, rather than using Slerp.
            return Quaternion.Euler(
                from.x + ShortestDelta(from.x, to.x) * progress,
                from.y + ShortestDelta(from.y, to.y) * progress,
                from.z + ShortestDelta(from.z, to.z) * progress);
        }

        static float ShortestDelta(float from, float to)
        {
            float delta = to - from;
            float wrapped = Mathf.Repeat(delta + 180f, 360f) - 180f;
            // Preserve the source clerp's original direction at an exact half-turn.
            return wrapped == -180f && delta > 0f ? 180f : wrapped;
        }

        // Prefab-root positions from BoardStandardGame/ZoneMgr/*_Deck/TopOfDeckBones.
        // These include all source parent transforms. The count describes the visible
        // source deck at draw start; its active thickness determines the starting bone.
        public static Vector3 FriendlyDeckTop(int remaining) =>
            new Vector3(DeckTopX(remaining), -.11390358f, -5.9850144175f);

        public static Vector3 OpponentDeckTop(int remaining) =>
            new Vector3(DeckTopX(remaining), -.11390358f, -2.6487117175f);

        static float DeckTopX(int remaining)
        {
            // ZoneDeck.UpdateThicknessWithCount uses count/26 with strict thresholds.
            // The source 25% and one-card top bones share the same position.
            float fraction = remaining / 26f;
            if (fraction > .75f) return -.666496f;
            if (fraction > .5f) return -.564746f;
            if (fraction > .25f) return -.487746f;
            return -.386648949f;
        }
    }
}
