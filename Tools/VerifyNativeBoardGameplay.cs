using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Integration checks exercise actual menu transitions and the live camera/UI fit.
public static class VerifyNativeBoardGameplay
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Click(Button button) => ExecuteEvents.Execute(button.gameObject,
        new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    static async Task Frame()
    {
        int f = Time.frameCount; var watch = System.Diagnostics.Stopwatch.StartNew();
        while (f == Time.frameCount) { if (watch.Elapsed.TotalSeconds > 10) throw new Exception("Game is not ticking"); await Task.Delay(25); }
    }
    static GameObject FirstHit(Vector2 position)
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
        return hits.FirstOrDefault().gameObject;
    }
    public static async Task<string[]> Main()
    {
        Check(EditorApplication.isPlaying, "Play mode only");
        var e = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        var presentation = e.boardThemes.GetComponentInChildren<NativeBoardPresentation>(true);
        Check(presentation != null, "Native board bridge missing");
        var root = presentation.boardRoot; var camera = presentation.boardCamera;
        Check(root.transform.parent == null && root.GetComponent<RectTransform>() == null, "Native board is not in world space");
        Check(root.GetComponentsInChildren<Transform>(true).Length == 121, "Recovered hierarchy changed");
        Check(root.GetComponentsInChildren<MeshFilter>(true).Length == 35 && root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 2, "Recovered geometry missing");
        Check(root.GetComponentsInChildren<Canvas>(true).Length == 0, "A background Canvas entered the source geometry");
        Check(e.boardThemes.boards.Count == 1 && e.boardThemes.boards[0] == presentation.gameObject, "First-board selection is not native Orgrimmar only");
        Check(e.boardThemes.GetComponentsInChildren<Graphic>(true).Length == 0 && !e.boardBackgroundImage.enabled && e.boardBackgrounds.Count == 0, "A flat board plate can still render");
        Check(new SerializedObject(camera.GetUniversalAdditionalCameraData()).FindProperty("m_RendererIndex").intValue == 1, "Board camera lost its 3D renderer");
        Check(new SerializedObject(GraphicsSettings.currentRenderPipeline).FindProperty("m_DefaultRendererIndex").intValue == 0, "Default 2D effects renderer changed");
        Check(UnityEngine.Object.FindObjectsByType<AudioListener>().Count(l => l.enabled) == 1, "Expected exactly one active AudioListener");
        e.frontEnd.ShowMenu(); await Frame();
        Check(!root.activeInHierarchy && camera.isActiveAndEnabled, "Menu must hide geometry while retaining the camera/audio");
        Click(e.frontEnd.playButton); await Frame();
        Check(root.activeInHierarchy && e.mulliganPanel.gameObject.activeInHierarchy, "Menu Play did not open native board/mulligan");
        Click(e.confirmMulliganButton); await Frame(); Canvas.ForceUpdateCanvases();
        foreach (var pair in new[] { (e.faceView, "HeroBackplateA"), (e.opponentFaceView, "HeroBackplateB") })
        {
            var face = pair.Item1;
            var mesh = root.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.sharedMaterial != null && r.sharedMaterial.name == pair.Item2).GetComponent<MeshFilter>();
            // Imported source meshes discard their CPU vertex arrays in Play mode.
            // Project the retained local bounds instead of enabling Read/Write for a test.
            var bounds = mesh.sharedMesh.bounds;
            var corners = from x in new[] { -1f, 1f } from y in new[] { -1f, 1f } from z in new[] { -1f, 1f }
                select bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
            var points = corners.Select(v => camera.WorldToScreenPoint(mesh.transform.TransformPoint(v))).ToArray();
            var center = new Vector2((points.Min(p => p.x) + points.Max(p => p.x)) * .5f, (points.Min(p => p.y) + points.Max(p => p.y)) * .5f);
            Check(Vector2.Distance(center, face.transform.position) < 1.5f, "Hero UI is detached from native socket at the current window aspect");
            var health = (TMP_Text)new SerializedObject(face).FindProperty("healthNumberText").objectReferenceValue;
            Check(FirstHit(face.avatarImage.transform.position)?.GetComponentInParent<FaceView>() == face, "Another graphic intercepts the hero");
            Check(FirstHit(health.transform.position)?.GetComponentInParent<FaceView>() == face, "Another graphic intercepts hero health targeting");
        }
        Check(FirstHit(e.heroPowerButton.transform.position)?.GetComponentInParent<Button>() == e.heroPowerButton, "Power click is intercepted");
        Check(FirstHit(e.endTurnButton.transform.position)?.GetComponentInParent<Button>() == e.endTurnButton, "End Turn click is intercepted");
        Click(e.frontEnd.matchMenuButton); await Frame();
        Check(!root.activeInHierarchy, "Returning to menu left the world board active");
        Click(e.frontEnd.playButton); await Frame();
        Check(root.activeInHierarchy && e.mulliganPanel.gameObject.activeInHierarchy && e.boardThemes.CurrentIndex == 0, "A second new game lost the native board");
        return new[] { "Original 121-transform world board / 35 static meshes / 2 skinned renderers; no board image plate", "Native 3D camera renderer isolated from default 2D effects; one active audio listener", "Real Menu Play, mulligan confirmation, Menu return and second new match preserve native board visibility", "Both hero paintings fit their projected mesh sockets at the live window aspect; hero/health/power/End Turn are first raycast targets" };
    }
}
