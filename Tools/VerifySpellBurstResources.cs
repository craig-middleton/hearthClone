using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Disposable fixtures only. Run in Play with Editor auto-tick enabled; no scene save.
public static class VerifySpellBurstResources
{
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    static async Task Until(Func<bool> condition, string message)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (timer.Elapsed.TotalSeconds > 6) throw new Exception(message);
            await Task.Delay(20);
        }
    }

    public static async Task<string> Main()
    {
        Check(EditorApplication.isPlaying, "Play mode required");
        var root = new GameObject("Disposable burst resource verification");
        var materials = new List<Material>();
        var textures = new List<RenderTexture>();
        float oldScale = Time.timeScale;
        try
        {
            Time.timeScale = 1;
            Func<Transform, ParticleSystem>[] builders = {
                SpellBurstFactory.CreateFireBurst, SpellBurstFactory.CreateArcaneBurst,
                SpellBurstFactory.CreateFrostBurst, SpellBurstFactory.CreateNatureBurst,
                p => SpellBurstFactory.CreateFrostSweep(p, 2, .2f)
            };
            var problems = new List<string>();
            foreach (var build in builders)
            {
                var particles = build(root.transform);
                var material = particles.GetComponent<ParticleSystemRenderer>().sharedMaterial;
                materials.Add(material);
                Check(material != null && material.shader.isSupported, "Burst lacks supported material");
                // Force interruption for every builder; no dependency on natural particle stop.
                UnityEngine.Object.Destroy(particles.gameObject);
                await Until(() => particles == null, "Interrupted burst was not destroyed");
                await Task.Delay(50);
                if (material != null) problems.Add("Interrupted burst retained its material");
            }
            var natural = SpellBurstFactory.CreateFireBurst(root.transform);
            var naturalMaterial = natural.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            materials.Add(naturalMaterial);
            await Until(() => natural == null, "Natural stop did not destroy the burst");
            await Task.Delay(50);
            if (naturalMaterial != null) problems.Add("Natural stop retained its material");

            // Separate camera/image survive component destruction, exposing stale bindings.
            var canvas = new GameObject("Burst canvas", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(root.transform, false);
            var canvasRect = (RectTransform)canvas.transform;
            canvasRect.sizeDelta = new Vector2(800, 600);
            var image = new GameObject("Burst image", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(canvas.transform, false);
            image.rectTransform.sizeDelta = new Vector2(100, 100);
            var camera = new GameObject("Burst camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(root.transform, false);
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 1;
            var host = new GameObject("Burst renderer");
            host.transform.SetParent(root.transform, false);
            host.SetActive(false);
            var renderer = host.AddComponent<UIParticleBurstRenderer>();
            renderer.burstCamera = camera;
            renderer.burstSpawnPoint = host.transform;
            renderer.displayImage = image;
            renderer.textureSize = 32;
            host.SetActive(true);
            var point = camera.targetTexture;
            textures.Add(point);
            Check(point != null && image.texture == point && point.width == 32, "Point texture not bound");
            point.Create();
            renderer.ShowAt(Vector2.zero);
            Check(camera.targetTexture == point, "Repeated point burst reallocated its texture");
            renderer.ShowAtRegion(new Rect(0, 0, 400, 100));
            var region = camera.targetTexture;
            textures.Add(region);
            Check(region != point && region.width == 128 && region.height == 32 && image.texture == region,
                "Region texture aspect or bindings incorrect");
            if (point.IsCreated()) problems.Add("Resize retained the old GPU allocation");
            await Until(() => point == null, "Resize retained the old texture object");
            region.Create();
            renderer.ShowAt(Vector2.zero);
            var restored = camera.targetTexture;
            textures.Add(restored);
            Check(restored.width == 32 && restored.height == 32 && image.texture == restored &&
                image.rectTransform.sizeDelta == new Vector2(100, 100) && camera.orthographicSize == 1,
                "Point framing was not restored after a region burst");
            if (region.IsCreated()) problems.Add("Point restore retained region GPU allocation");
            await Until(() => region == null, "Point restore retained region texture object");
            restored.Create();
            UnityEngine.Object.Destroy(renderer);
            await Until(() => renderer == null, "Renderer component was not destroyed");
            if (camera.targetTexture != null || image.texture != null) problems.Add("Teardown retained owned texture bindings");
            await Task.Delay(50);
            if (restored != null) problems.Add("Teardown retained owned texture");

            // An external replacement binding must not be cleared or destroyed by the owner.
            host.SetActive(false);
            renderer = host.AddComponent<UIParticleBurstRenderer>();
            renderer.burstCamera = camera;
            renderer.burstSpawnPoint = host.transform;
            renderer.displayImage = image;
            renderer.textureSize = 32;
            host.SetActive(true);
            var owned = camera.targetTexture;
            textures.Add(owned);
            var external = new RenderTexture(16, 16, 24);
            textures.Add(external);
            camera.targetTexture = external;
            image.texture = external;
            UnityEngine.Object.Destroy(renderer);
            await Until(() => renderer == null, "Second renderer was not destroyed");
            await Task.Delay(50);
            Check(camera.targetTexture == external && image.texture == external && external != null,
                "Teardown changed an externally owned binding");
            if (owned != null) problems.Add("Rebound renderer retained owned texture");
            Check(problems.Count == 0, string.Join("; ", problems));
            return "All five burst builders clean materials after interruption; natural particle stop cleans material; point texture reuse, region resize/release, point framing restore, component teardown with surviving camera/image, and external binding preservation passed.";
        }
        finally
        {
            Time.timeScale = oldScale;
            UnityEngine.Object.Destroy(root);
            // Also cleans leaks when demonstrating the pre-fix failure.
            foreach (var material in materials) if (material != null) UnityEngine.Object.Destroy(material);
            foreach (var texture in textures) if (texture != null) { texture.Release(); UnityEngine.Object.Destroy(texture); }
        }
    }
}
