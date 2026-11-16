using System;
using System.Collections.Generic;
using System.Linq;
using HearthstoneClone.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    // Source motion is evaluated in its original world coordinates. A world-space Canvas
    // lets the recovered board camera project every card vertex, including the flip.
    // See Docs/Recovery/ORIGINAL_CARD_MOTION.md for provenance and retargeting limits.
    public class NativeCardDrawPresentation : MonoBehaviour
    {
        public Camera boardCamera;
        public bool opponent;
        public event Action Completed;
        public bool IsBusy => pending.Count != 0 || flights.Count != 0;
        public int ActiveCount => flights.Count;
        const float Units = 180f / 2.17f;
        static readonly Quaternion Plane = Quaternion.Euler(90, 0, 0);
        static readonly Vector3 Center = new Vector3(-.055f, 0, .041f);
        static readonly Vector3 FriendlyReveal = new Vector3(-4.3f, 8, -5.303798f);
        static readonly Vector3 OpponentReveal = new Vector3(-3.598883f, 2.072181f, -2.346226f);
        static readonly Vector3 InitialEuler = new Vector3(270, 270, 0);
        readonly Dictionary<CardInstance, int> pending = new Dictionary<CardInstance, int>();
        readonly Dictionary<CardInstance, Flight> flights = new Dictionary<CardInstance, Flight>();

        sealed class Flight
        {
            public CardInstance card;
            public CardView target;
            public CanvasGroup hidden;
            public float alpha;
            public bool blocks, interactable;
            public GameObject root, front, back;
            public Vector3[] nodes;
            public float started;
            public Vector3 reflowStart;
            public bool reflow;
        }

        // Called only by PlayerHand.DrawCard after a successful, non-burned draw. Generated
        // cards and initial/mulligan hands must not masquerade as deck draws.
        public void Queue(CardInstance card, int remaining)
        {
            if (card != null && boardCamera != null && isActiveAndEnabled) pending[card] = remaining;
        }

        public void Bind(IReadOnlyList<CardView> views, bool faceDown)
        {
            var present = new HashSet<CardInstance>(views.Select(v => v.Card));
            foreach (var card in pending.Keys.Where(c => !present.Contains(c)).ToArray()) pending.Remove(card);
            foreach (var card in flights.Keys.Where(c => !present.Contains(c)).ToArray()) Finish(card);
            foreach (var view in views)
            {
                if (flights.TryGetValue(view.Card, out var running)) Hide(running, view);
                else if (pending.TryGetValue(view.Card, out int remaining))
                {
                    pending.Remove(view.Card);
                    // Manual opponent control reveals its hand; that separate source draw
                    // override is not recovered, so keep its existing immediate presentation.
                    if (opponent && !faceDown) continue;
                    var f = Create(view, remaining);
                    flights.Add(view.Card, f);
                    Hide(f, view);
                    Sample(f, 0);
                }
            }
        }

        Flight Create(CardView view, int remaining)
        {
            var root = new GameObject("Original card draw — " + (opponent ? "opponent" : view.Card.Data.cardName), typeof(RectTransform), typeof(Canvas));
            root.layer = boardCamera.gameObject.layer;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = boardCamera;
            var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(180, 270);
            var f = new Flight { card = view.Card, root = root, started = Time.unscaledTime };
            f.front = Snapshot(view, root.transform, false);
            f.back = Snapshot(view, root.transform, true);
            f.back.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Vector3 deck = opponent ? NativeCardMotionMath.OpponentDeckTop(remaining + 1) : NativeCardMotionMath.FriendlyDeckTop(remaining + 1);
            f.nodes = opponent ? new[] { deck, deck + new Vector3(-.3f, 3.6f, 0), OpponentReveal, Vector3.zero } :
                new[] { deck, deck + new Vector3(-.3f, 3.6f, 0), FriendlyReveal };
            return f;
        }

        GameObject Snapshot(CardView original, Transform parent, bool back)
        {
            var clone = Instantiate(original.gameObject, parent, false);
            clone.name = back ? "Original card back" : "Native layered card front";
            var rect = (RectTransform)clone.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = new Vector2(180, 270);
            rect.localPosition = new Vector3(Center.x * Units, Center.z * Units, 0);
            rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one;
            clone.GetComponent<CardView>().SetCard(original.Card, faceDown: back);
            foreach (var behaviour in clone.GetComponents<Behaviour>())
                if (behaviour is CardView || behaviour is CardHoverFan || behaviour is Selectable) behaviour.enabled = false;
            foreach (var graphic in clone.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            var group = clone.GetComponent<CanvasGroup>(); if (group == null) group = clone.AddComponent<CanvasGroup>();
            group.alpha = 1; group.interactable = false; group.blocksRaycasts = false;
            return clone;
        }

        static void Restore(Flight f)
        {
            if (f.hidden == null) return;
            f.hidden.alpha = f.alpha; f.hidden.blocksRaycasts = f.blocks; f.hidden.interactable = f.interactable;
            f.hidden = null;
        }
        static void Hide(Flight f, CardView view)
        {
            if (f.target == view && f.hidden != null) return;
            Restore(f); f.target = view;
            var group = view.GetComponent<CanvasGroup>(); if (group == null) group = view.gameObject.AddComponent<CanvasGroup>();
            f.hidden = group; f.alpha = group.alpha; f.blocks = group.blocksRaycasts; f.interactable = group.interactable;
            group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
        }

        void LateUpdate()
        {
            if (boardCamera == null) { ResetDraws(); return; }
            bool wasBusy = IsBusy;
            foreach (var pair in flights.ToArray())
            {
                var f = pair.Value;
                if (f.target == null) { Finish(pair.Key); continue; }
                float elapsed = Time.unscaledTime - f.started;
                Sample(f, elapsed);
                if (elapsed >= (opponent ? 1.75f : 2f)) Finish(pair.Key);
            }
            if (wasBusy && !IsBusy) Completed?.Invoke();
        }

        // The final slot is retargeted to the existing UI fan. Native draw bones, paths,
        // Euler interpolation, easing, and source durations are otherwise unchanged.
        void TargetPose(Flight f, out Vector3 position, out Quaternion rotation, out float scale)
        {
            var rect = (RectTransform)f.target.transform;
            var screen = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            float depth = boardCamera.WorldToScreenPoint(opponent ? new Vector3(-7.75350189f, .60495643f, 1.261893f) : new Vector3(-8.1490879f, 2.87161303f, -9.713717f)).z;
            var center = boardCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            // UI cards lie parallel to the camera image plane at the retargeted hand slot.
            var canvasRotation = boardCamera.transform.rotation * rect.rotation;
            if (opponent) canvasRotation *= Quaternion.Euler(0, 180, 0);
            rotation = canvasRotation * Quaternion.Inverse(Plane);
            float worldHeight = 2 * depth * Mathf.Tan(boardCamera.fieldOfView * .5f * Mathf.Deg2Rad);
            float pixelHeight = rect.rect.height * rect.lossyScale.y;
            scale = pixelHeight / Screen.height * worldHeight / (270f / Units);
            position = center - rotation * (Center * scale);
        }

        void Sample(Flight f, float elapsed)
        {
            TargetPose(f, out var target, out var targetRotation, out var targetScale);
            float duration = opponent ? 1.75f : 1.5f;
            Vector3 p; Quaternion q; float s;
            if (opponent)
            {
                f.nodes[3] = target;
                p = NativeCardMotionMath.Spline(f.nodes, NativeCardMotionMath.InOutPower(Mathf.Clamp01(elapsed / duration), 4));
                float r = elapsed < .8f ? 0 : NativeCardMotionMath.InOutPower(Mathf.Clamp01((elapsed - .8f) / .7f), 3);
                q = NativeCardMotionMath.Euler(InitialEuler, targetRotation.eulerAngles, r);
                s = Mathf.LerpUnclamped(.88f, targetScale, elapsed < .8f ? 0 : NativeCardMotionMath.InOutPower(Mathf.Clamp01((elapsed - .8f) / .7f), 5));
            }
            else if (elapsed <= duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                p = NativeCardMotionMath.Spline(f.nodes, t <= 0 ? 0 : NativeCardMotionMath.OutExpo(t));
                float r = elapsed < .15f ? 0 : elapsed >= 1.5f ? 1 : NativeCardMotionMath.OutExpo((elapsed - .15f) / 1.35f);
                q = NativeCardMotionMath.Euler(InitialEuler, new Vector3(0, 0, 357), r);
                float sc = elapsed < .15f ? 0 : elapsed >= .9f ? 1 : NativeCardMotionMath.OutExpo((elapsed - .15f) / .75f);
                s = Mathf.LerpUnclamped(.88f, 1, sc);
            }
            else
            {
                if (!f.reflow) { f.reflow = true; f.reflowStart = NativeCardMotionMath.Spline(f.nodes, NativeCardMotionMath.OutExpo(1)); }
                float t = elapsed >= 2 ? 1 : NativeCardMotionMath.InExpo(Mathf.Clamp01((elapsed - 1.5f) / .5f));
                p = Vector3.LerpUnclamped(f.reflowStart, target, t);
                q = NativeCardMotionMath.Euler(new Vector3(0, 0, 357), targetRotation.eulerAngles, t);
                s = Mathf.LerpUnclamped(1, targetScale, t);
            }
            f.root.transform.SetPositionAndRotation(p, q * Plane);
            f.root.transform.localScale = Vector3.one * (s / Units);
            bool front = !opponent && Vector3.Dot(q * Vector3.up, boardCamera.transform.position - p) >= 0;
            f.front.SetActive(front); f.back.SetActive(!front);
        }

        void Finish(CardInstance card)
        {
            if (!flights.TryGetValue(card, out var f)) return;
            Restore(f); f.root.SetActive(false); Destroy(f.root); flights.Remove(card);
        }
        public void ResetDraws()
        {
            pending.Clear();
            foreach (var f in flights.Values) { Restore(f); if (f.root != null) { f.root.SetActive(false); Destroy(f.root); } }
            flights.Clear();
        }
        void OnDisable() => ResetDraws();
        void OnDestroy() => ResetDraws();
    }
}
