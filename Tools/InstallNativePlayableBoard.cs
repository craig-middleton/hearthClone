using System;
using System.Collections.Generic;
using System.Linq;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fits existing gameplay views to the recovered source geometry. No game rules change.
public static class InstallNativePlayableBoard
{
    const string GameScene = "Assets/Scenes/HearthstoneTable.unity";
    const string ProofScene = "Assets/Scenes/RecoveredOrgrimmarProof.unity";

    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode before authoring.");
        if (SceneManager.GetActiveScene().isDirty) throw new Exception("Save existing scene changes first.");
        var scene = EditorSceneManager.OpenScene(GameScene);
        var e = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        var surface = e.frontEnd.tableSurface.transform;
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Recovered original Orgrimmar" || root.name == "Main Camera")
                UnityEngine.Object.DestroyImmediate(root);

        // Copy the proven camera/light/render settings through the Editor, retaining
        // every source local transform and leaving the isolated proof on disk intact.
        var proof = EditorSceneManager.OpenScene(ProofScene, OpenSceneMode.Additive);
        var native = proof.GetRootGameObjects().Single(g => g.name == "Recovered original Orgrimmar");
        var camera = proof.GetRootGameObjects().Single(g => g.GetComponent<Camera>() != null).GetComponent<Camera>();
        SceneManager.MoveGameObjectToScene(native, scene);
        SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
        EditorSceneManager.CloseScene(proof, true);
        SceneManager.SetActiveScene(scene);
        camera.name = "Main Camera"; camera.tag = "MainCamera";
        if (camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();
        // Extend only the surrounding wooden tabletop for wide desktop windows.
        // Keep its mesh center fixed; the actual board/scenery retain source scale.
        var table = native.GetComponentsInChildren<MeshFilter>(true).Single(m => m.name == "TableTop");
        var tableCenter = table.transform.TransformPoint(table.sharedMesh.bounds.center);
        var tableScale = table.transform.localScale; tableScale.x *= 1.3f;
        table.transform.localScale = tableScale;
        table.transform.position += tableCenter - table.transform.TransformPoint(table.sharedMesh.bounds.center);

        var themes = e.boardThemes;
        foreach (Transform child in themes.transform.Cast<Transform>().ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        var proxy = new GameObject("Orgrimmar", typeof(RectTransform));
        proxy.transform.SetParent(themes.transform, false);
        var presentation = proxy.AddComponent<NativeBoardPresentation>();
        presentation.boardRoot = native; presentation.boardCamera = camera;
        themes.boards.Clear(); themes.boards.Add(proxy);
        if (e.boardBackgroundImage != null) e.boardBackgroundImage.enabled = false;
        e.boardBackgrounds.Clear();
        var oldDeck = surface.Find("DeckPile"); if (oldDeck != null) oldDeck.gameObject.SetActive(false);

        // Use the recovered camera's reference viewport to measure the actual seats.
        camera.aspect = NativeBoardPresentation.ReferenceAspect;
        camera.fieldOfView = NativeBoardPresentation.NativeVerticalFieldOfView;
        FitHero(e.faceView, native, camera, "HeroBackplateA", surface);
        FitHero(e.opponentFaceView, native, camera, "HeroBackplateB", surface);
        Place((RectTransform)e.heroPowerButton.transform, 183, -305, 139, 139);
        e.heroPowerButton.transform.localScale = Vector3.one * .90f;
        var otherPower = surface.Find("OpponentHeroPowerButton");
        Place((RectTransform)otherPower, 184, 322, 139, 139); otherPower.localScale = Vector3.one * .90f;
        Place((RectTransform)surface.Find("EndTurnButton"), 595, 44, 158, 69);
        FitPocket(surface.Find("DeckPockets/OpponentDeck"), 650, 195);
        FitPocket(surface.Find("DeckPockets/PlayerDeck"), 650, -102);

        Place((RectTransform)surface.Find("PlayerMana"), 302, -458, 78, 35);
        surface.Find("PlayerMana").GetComponent<TMP_Text>().fontSize = 21;
        Place((RectTransform)surface.Find("ManaCrystalRow"), 535, -458, 365, 40);
        var tray = (RectTransform)surface.Find("ManaTray");
        Place(tray, 530, -458, 51, 422); tray.localRotation = Quaternion.Euler(0, 0, -90);
        // The recovered frame already contains the blue opponent mana socket.
        surface.Find("OpponentManaBadge").gameObject.SetActive(false);
        Place((RectTransform)surface.Find("OpponentMana"), 265, 472, 77, 32);
        surface.Find("OpponentMana").GetComponent<TMP_Text>().fontSize = 19;
        var fan = e.handDisplay.handPanel.GetComponent<HandFanLayout>();
        fan.perCardStep = 100; fan.maxTotalWidth = 650;
        ((RectTransform)e.handDisplay.handPanel).anchoredPosition = new Vector2(-85, -505);

        presentation.FitCamera();
        e.frontEnd.tableSurface.SetActive(false);
        native.SetActive(false); // The saved scene starts at the menu.
        foreach (var component in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)))
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Selection.activeGameObject = native;
        return "Playable Orgrimmar installed: original 121-transform world hierarchy, native camera/shadows, fitted heroes/powers/mana and live recessed decks. Menu starts with board hidden; all new matches use this first board.";
    }

    static Vector2 Project(Camera camera, Vector3 point)
    {
        var p = camera.WorldToViewportPoint(point);
        return new Vector2((p.x - .5f) * 1920, (p.y - .5f) * 1080);
    }

    static void FitHero(FaceView face, GameObject native, Camera camera, string materialName, Transform surface)
    {
        var mesh = native.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.sharedMaterial != null && r.sharedMaterial.name == materialName).GetComponent<MeshFilter>();
        var points = mesh.sharedMesh.vertices.Select(v => Project(camera, mesh.transform.TransformPoint(v))).ToArray();
        var min = new Vector2(points.Min(p => p.x), points.Min(p => p.y));
        var max = new Vector2(points.Max(p => p.x), points.Max(p => p.y));
        var center = (min + max) * .5f; var size = max - min;
        Place((RectTransform)face.transform, center.x, center.y, 174, 212);
        var mask = (RectTransform)face.avatarImage.transform.parent;
        Place(mask, 0, 0, size.x * .985f, size.y * .985f);
        var shape = mask.GetComponent<PortraitSilhouette>();
        shape.outline = Hull(points).Select(p => new Vector2((p.x - min.x) / size.x, (p.y - min.y) / size.y)).ToArray();
        shape.raycastTarget = false; shape.SetVerticesDirty();
        var fit = face.avatarImage.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = face.avatarImage.sprite.rect.width / face.avatarImage.sprite.rect.height;
        face.avatarImage.enabled = true; face.avatarImage.raycastTarget = false;
        face.breathScaleAmount = face.swayAmount = 0;
        var rim = face.transform.Find("HeroArch"); if (rim != null) rim.gameObject.SetActive(false);
        face.GetComponent<Image>().color = Color.clear; face.GetComponent<Image>().raycastTarget = true;
        var so = new SerializedObject(face);
        var health = (Image)so.FindProperty("healthGemImage").objectReferenceValue;
        var number = (TMP_Text)so.FindProperty("healthNumberText").objectReferenceValue;
        var healthCenter = center + new Vector2(size.x * .5f - 3, -size.y * .5f + 8);
        Place((RectTransform)health.transform.parent, healthCenter.x, healthCenter.y, 38, 53);
        Place(number.rectTransform, healthCenter.x, healthCenter.y - 5, 60, 44); number.fontSize = 30;
        var armor = (GameObject)so.FindProperty("armorDisplay").objectReferenceValue;
        if (armor != null) Place((RectTransform)armor.transform, center.x + 74, center.y - 16, 38, 43);
        EditorUtility.SetDirty(face);
    }

    static List<Vector2> Hull(IEnumerable<Vector2> input)
    {
        var p = input.Distinct().OrderBy(v => v.x).ThenBy(v => v.y).ToArray();
        var hull = new List<Vector2>();
        foreach (var v in p)
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], v) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(v);
        }
        int lower = hull.Count;
        for (int i = p.Length - 2; i >= 0; i--)
        {
            while (hull.Count > lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p[i]) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(p[i]);
        }
        hull.RemoveAt(hull.Count - 1); return hull;
    }
    static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

    static void FitPocket(Transform pocket, float x, float y)
    {
        Place((RectTransform)pocket, x, y, 50, 180);
        foreach (string name in new[] { "StoneRim", "Recess", "InnerLip" }) pocket.Find(name).gameObject.SetActive(false);
        var clip = (RectTransform)pocket.Find("InsidePocket"); Place(clip, 0, 0, 38, 154);
        for (int i = 0; i < clip.childCount; i++)
        {
            var layer = (RectTransform)clip.GetChild(i); Place(layer, -6 + i * 2, -5 + i * 2, 67, 140);
            layer.localRotation = Quaternion.Euler(0, 72, 0);
        }
        var text = pocket.Find("Remaining").GetComponent<TMP_Text>();
        Place(text.rectTransform, 0, -96, 65, 29); text.fontSize = 20;
    }
    static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
    }
}
