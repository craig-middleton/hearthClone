using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using A = HearthstoneClone.UI.NativeCornerLibrary.Action;
using P = HearthstoneClone.UI.NativeCornerLibrary.Parameter;

namespace HearthstoneClone.UI
{
    public class NativeCornerInteractions : MonoBehaviour
    {
        public Transform sourceRoot;
        public Camera boardCamera;
        public NativeCornerLibrary library;
        public Pose[] resetPoses;
        public RendererState[] resetRenderers;
        public ColliderState[] resetColliders;
        [Serializable] public class Pose { public Transform target; public Vector3 position, scale; public Quaternion rotation; }
        [Serializable] public class RendererState { public Renderer target; public bool enabled; }
        [Serializable] public class ColliderState { public Collider target; public bool enabled; }

        class Running
        {
            public NativeCornerLibrary.Machine data;
            public NativeCornerLibrary.State state;
            public float entered;
            public bool[] done;
            public string pending;
            public int clicks;
            public readonly Dictionary<string, float> floats = new();
            public readonly Dictionary<string, GameObject> objects = new();
            public readonly Dictionary<A, Gate> gates = new();
            public readonly Dictionary<A, AnimationState> animations = new();
            public readonly Dictionary<A, float> rotations = new();
        }
        class Gate { public int count, threshold; public bool opened = true; }
        readonly List<Running> machines = new();
        readonly Dictionary<string, Transform> paths = new();
        readonly Dictionary<A, AudioSource> voices = new();
        Vector2 pointer;
        public int TotalClicks => machines.Sum(m => m.clicks);
        public string StateOf(string corner) => machines.First(m => m.data.path.EndsWith("/" + corner)).state.name;
        public int ClicksOf(string corner) => machines.First(m => m.data.path.EndsWith("/" + corner)).clicks;

        Transform Find(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (paths.TryGetValue(path, out var value)) return value;
            var relative = path == "ORG" ? "" : path.Substring(4);
            value = relative.Length == 0 ? sourceRoot : sourceRoot.Find(relative);
            if (value == null) throw new InvalidOperationException("Missing original corner object: " + path);
            paths.Add(path, value); return value;
        }
        P Param(A a, string name) => Array.Find(a.parameters, p => p.name == name);
        float Number(Running m, A a, string name, float fallback = 0)
        {
            var p = Param(a, name); if (p == null || p.none) return fallback;
            return string.IsNullOrEmpty(p.variable) ? p.number : m.floats.GetValueOrDefault(p.variable);
        }
        string Text(A a, string name) => Param(a, name)?.text ?? "";
        Transform Target(Running m, A a, string name)
        {
            var p = Param(a, name); if (p == null || p.none) return null;
            if (!string.IsNullOrEmpty(p.variable)) return m.objects.GetValueOrDefault(p.variable)?.transform;
            return Find(p.path == "$owner" ? m.data.path : p.path);
        }
        void Store(Running m, A a, string name, float value)
        {
            var p = Param(a, name); if (p != null && !string.IsNullOrEmpty(p.variable)) m.floats[p.variable] = value;
        }
        void Event(Running m, string name) { if (!string.IsNullOrEmpty(name) && m.pending == null) m.pending = name; }

        void OnEnable()
        {
            if (library == null || sourceRoot == null) return;
            Restore(); machines.Clear();
            foreach (var data in library.machines)
            {
                var m = new Running { data = data }; machines.Add(m); Enter(m, data.start); Drain(m);
            }
        }
        void Restore()
        {
            if (resetPoses != null) foreach (var p in resetPoses) if (p.target != null) { p.target.localPosition = p.position; p.target.localRotation = p.rotation; p.target.localScale = p.scale; }
            if (resetRenderers != null) foreach (var r in resetRenderers) if (r.target != null) r.target.enabled = r.enabled;
            if (resetColliders != null) foreach (var c in resetColliders) if (c.target != null) c.target.enabled = c.enabled;
        }
        void OnDisable()
        {
            foreach (var v in voices.Values) if (v != null) v.Stop();
            if (library != null) foreach (var machine in library.machines) foreach (var state in machine.states) foreach (var a in state.actions)
            {
                if (a.type is "PlayAnimation" or "AnimationPlaythroughAction")
                {
                    var target = Find(Param(a, a.type == "PlayAnimation" ? "gameObject" : "m_GameObject").path);
                    var anim = target.GetComponent<Animation>(); if (anim != null) anim.Stop();
                }
                if (a.type is "ParticlePlayAction" or "ParticleStopAction")
                {
                    var target = Find(Param(a, "m_GameObject").path);
                    var ps = target.GetComponent<ParticleSystem>(); if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            machines.Clear(); Restore();
        }
        void Enter(Running m, string state)
        {
            m.state = Array.Find(m.data.states, s => s.name == state); m.entered = Time.time; m.pending = null;
            m.done = new bool[m.state.actions.Length];
            for (int i = 0; i < m.done.Length; i++) m.done[i] = EnterAction(m, m.state.actions[i]);
        }
        void Drain(Running m)
        {
            for (int guard = 0; guard < 20; guard++)
            {
                if (m.pending == null && m.done.All(b => b)) m.pending = "FINISHED";
                if (m.pending == null) return;
                var next = Array.Find(m.state.transitions, t => t.eventName == m.pending); m.pending = null;
                if (next == null) return;
                Enter(m, next.toState);
            }
            throw new InvalidOperationException("Unbounded original corner transition: " + m.data.path);
        }
        void Update()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                pointer = mouse.position.ReadValue();
                bool blocked = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                HandlePointer(pointer, mouse.leftButton.wasPressedThisFrame && !blocked, mouse.leftButton.wasReleasedThisFrame && !blocked);
            }
            foreach (var m in machines)
            {
                for (int i = 0; i < m.done.Length && m.pending == null; i++) if (!m.done[i]) m.done[i] = Tick(m, m.state.actions[i]);
                Drain(m);
            }
        }
        void LateUpdate()
        {
            // Source audio remains attached to moving props (notably RockPickup).
            // Keep the elevated camera inside the same full-volume board radius.
            foreach (var voice in voices.Values)
                if (voice != null && voice.isPlaying) voice.minDistance = Vector3.Distance(boardCamera.transform.position, voice.transform.position) + .01f;
        }
        // Also used by editor checks, with actual screen-space collider hits.
        public bool HandlePointer(Vector2 screenPosition, bool down, bool up)
        {
            if (!isActiveAndEnabled || boardCamera == null) return false;
            pointer = screenPosition; bool consumed = false;
            foreach (var m in machines)
            {
                foreach (var a in m.state.actions)
                {
                    if (a.type != "MousePickRandomAction") continue;
                    var target = Target(m, a, "GameObject"); var collider = target.GetComponent<Collider>();
                    bool hit = collider != null && collider.enabled && collider.Raycast(boardCamera.ScreenPointToRay(pointer), out _, 100);
                    if (!hit) continue;
                    if (down && (Text(a, "mouseDownGateOpen").Length > 0 || Text(a, "mouseDownGateClosed").Length > 0))
                    {
                        var gate = m.gates[a]; gate.count++; m.clicks++;
                        bool open = gate.count >= gate.threshold;
                        if (open) { gate.opened = true; if (Number(m, a, "ResetOnOpen") != 0) gate.count = 0; }
                        Event(m, Text(a, open ? "mouseDownGateOpen" : "mouseDownGateClosed")); consumed = true;
                    }
                    if (up && Text(a, "mouseUp").Length > 0) { Event(m, Text(a, "mouseUp")); consumed = true; }
                }
                Drain(m);
            }
            return consumed;
        }
        bool EnterAction(Running m, A a)
        {
            switch (a.type)
            {
                case "MousePickRandomAction":
                    if (!m.gates.TryGetValue(a, out var gate)) { gate = new Gate(); m.gates.Add(a, gate); }
                    if (gate.opened) { gate.threshold = UnityEngine.Random.Range((int)Number(m, a, "RandomGateClicksMin"), (int)Number(m, a, "RandomGateClicksMax")); gate.opened = false; }
                    return false;
                case "PlayAnimation": case "AnimationPlaythroughAction":
                    bool playthrough = a.type == "AnimationPlaythroughAction";
                    var animation = Target(m, a, playthrough ? "m_GameObject" : "gameObject").GetComponent<Animation>();
                    string clip = Text(a, playthrough ? "m_AnimName" : "animName");
                    if (animation == null || animation[clip] == null) throw new InvalidOperationException("Original clip is missing: " + clip);
                    float blend = Number(m, a, playthrough ? "m_CrossFadeSec" : "blendTime");
                    var mode = (PlayMode)(int)Number(m, a, playthrough ? "m_PlayMode" : "playMode");
                    if (blend > 0) animation.CrossFade(clip, blend, mode); else animation.Play(clip, mode);
                    m.animations[a] = animation[clip]; return playthrough;
                case "ParticlePlayAction": case "ParticleStopAction":
                    var ps = Target(m, a, "m_GameObject").GetComponent<ParticleSystem>(); bool children = Number(m, a, "m_IncludeChildren") != 0;
                    if (a.type == "ParticlePlayAction") ps.Play(children); else ps.Stop(children, ParticleSystemStopBehavior.StopEmitting);
                    return true;
                case "AudioPlayRandomClipAction": case "AudioPlayClipAction": PlaySound(m, a); return true;
                case "SetVisibility": case "SetVisibilityRecursiveAction":
                    var target = Target(m, a, "gameObject");
                    var renderers = Number(m, a, "includeChildren") != 0 ? target.GetComponentsInChildren<Renderer>(true) : target.GetComponents<Renderer>();
                    foreach (var r in renderers) r.enabled = Number(m, a, "toggle") != 0 ? !r.enabled : Number(m, a, "visible") != 0;
                    return true;
                case "SetProperty":
                    Target(m, a, "collider").GetComponent<Collider>().enabled = Number(m, a, "enabled") != 0; return true;
                case "GetMainCamera":
                    m.objects[Param(a, "storeGameObject").variable] = boardCamera.gameObject; return true;
                case "iTweenRotateAdd": m.rotations[a] = 0; return false;
                case "Wait": return false;
                case "GetMouseX": case "GetMouseY": case "ScreenToWorldPoint": case "SetPosition": case "ScreenPick": case "GameObjectCompare":
                    Execute(m, a); return Number(m, a, "everyFrame") == 0;
                default: throw new InvalidOperationException("Unimplemented original corner action: " + a.type);
            }
        }
        bool Tick(Running m, A a)
        {
            if (a.type == "Wait")
            {
                if (Time.time - m.entered < Number(m, a, "time")) return false;
                Event(m, Text(a, "finishEvent")); return true;
            }
            if (a.type == "PlayAnimation")
            {
                var s = m.animations[a]; if (s.enabled && !(s.wrapMode == WrapMode.ClampForever && s.time > s.length)) return false;
                Event(m, Text(a, "finishEvent")); return true;
            }
            if (a.type == "iTweenRotateAdd")
            {
                float t = Mathf.Clamp01((Time.time - m.entered - Number(m, a, "delay")) / Number(m, a, "time"));
                float eased = Bounce(t); float old = m.rotations[a]; m.rotations[a] = eased;
                Target(m, a, "gameObject").Rotate(Param(a, "vector").vector * (eased - old), (Space)(int)Number(m, a, "space"));
                return t >= 1;
            }
            if (a.type != "MousePickRandomAction") Execute(m, a);
            return false;
        }
        // Source iTween easeType 24: easeOutBounce, RotateAdd uses incremental Self rotation.
        static float Bounce(float t)
        {
            if (t < 1 / 2.75f) return 7.5625f * t * t;
            if (t < 2 / 2.75f) { t -= 1.5f / 2.75f; return 7.5625f * t * t + .75f; }
            if (t < 2.5f / 2.75f) { t -= 2.25f / 2.75f; return 7.5625f * t * t + .9375f; }
            t -= 2.625f / 2.75f; return 7.5625f * t * t + .984375f;
        }
        void Execute(Running m, A a)
        {
            switch (a.type)
            {
                case "GetMouseX": case "GetMouseY":
                    bool x = a.type == "GetMouseX"; Store(m, a, "storeResult", (x ? pointer.x : pointer.y) / (Number(m, a, "normalize") != 0 ? (x ? Screen.width : Screen.height) : 1)); break;
                case "ScreenToWorldPoint":
                    var screen = new Vector3(Number(m, a, "screenX"), Number(m, a, "screenY"), Number(m, a, "screenZ"));
                    if (Number(m, a, "normalized") != 0) { screen.x *= Screen.width; screen.y *= Screen.height; }
                    var world = boardCamera.ScreenToWorldPoint(screen);
                    Store(m, a, "storeWorldX", world.x); Store(m, a, "storeWorldY", world.y); Store(m, a, "storeWorldZ", world.z); break;
                case "SetPosition":
                    var target = Target(m, a, "gameObject"); bool local = Number(m, a, "space") == 1;
                    var position = local ? target.localPosition : target.position;
                    position.x = Number(m, a, "x", position.x); position.y = Number(m, a, "y", position.y); position.z = Number(m, a, "z", position.z);
                    if (local) target.localPosition = position; else target.position = position; break;
                case "ScreenPick":
                    var ray = boardCamera.ScreenPointToRay(new Vector3(Number(m, a, "screenX"), Number(m, a, "screenY")));
                    GameObject picked = Physics.Raycast(ray, out var hit, Number(m, a, "rayDistance"), Physics.DefaultRaycastLayers) ? hit.collider.gameObject : null;
                    m.objects[Param(a, "storeGameObject").variable] = picked; break;
                case "GameObjectCompare":
                    bool equal = Target(m, a, "gameObjectVariable") == Target(m, a, "compareTo"); Event(m, Text(a, equal ? "equalEvent" : "notEqualEvent")); break;
            }
        }
        void PlaySound(Running m, A a)
        {
            if (a.sounds == null || a.sounds.Length == 0) return;
            if (!voices.TryGetValue(a, out var voice))
            {
                var target = Target(m, a, "m_ParentObject"); voice = target.gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voices.Add(a, voice);
            }
            bool single = a.type == "AudioPlayClipAction";
            voice.spatialBlend = Number(m, a, "m_SpatialBlend", 1); voice.minDistance = Vector3.Distance(boardCamera.transform.position, voice.transform.position) + .01f;
            voice.pitch = single ? Number(m, a, "m_Pitch", 1) : UnityEngine.Random.Range(Number(m, a, "m_MinPitch", 1), Number(m, a, "m_MaxPitch", 1));
            float volume = single ? Number(m, a, "m_Volume", 1) : UnityEngine.Random.Range(Number(m, a, "m_MinVolume", 1), Number(m, a, "m_MaxVolume", 1));
            int choice = 0; float total = a.weights == null ? 0 : a.weights.Sum();
            if (total > 0)
            {
                float ticket = UnityEngine.Random.value * total;
                for (; choice < a.sounds.Length - 1; choice++) { ticket -= a.weights[choice]; if (ticket < 0) break; }
            }
            else choice = UnityEngine.Random.Range(0, a.sounds.Length);
            voice.PlayOneShot(a.sounds[choice], volume);
        }
    }
}
