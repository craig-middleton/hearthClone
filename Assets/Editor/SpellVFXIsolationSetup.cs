using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using HearthstoneClone.UI;

namespace HearthstoneClone.EditorTools
{
    // One-shot setup for the spell-VFX plan's step 1 (isolated ParticleSystem render test).
    // Builds the scene rig via the Editor API rather than hand-authored scene YAML, since a
    // Camera + RenderTexture + UI wiring is easy to get subtly wrong by hand and easy to get
    // right by just asking Unity to serialize what it built. Run once via
    // Tools > Spell VFX > Setup Isolation Test (Step 1) with TestScene open, then delete this
    // file once step 1 is confirmed and folded into the real integration.
    public static class SpellVFXIsolationSetup
    {
        [MenuItem("Tools/Spell VFX/Setup Isolation Test (Step 1)")]
        public static void Run()
        {
            GameObject canvasGO = GameObject.Find("GameCanvas");
            if (canvasGO == null)
            {
                Debug.LogError("SpellVFXIsolationSetup: GameCanvas not found in the active scene - open Assets/Scenes/TestScene.unity first.");
                return;
            }

            if (GameObject.Find("SpellVFXController") != null)
            {
                Debug.LogWarning("SpellVFXIsolationSetup: rig already exists in this scene (SpellVFXController found) - not creating a duplicate.");
                return;
            }

            int uiParticlesLayer = LayerMask.NameToLayer("UIParticles");
            if (uiParticlesLayer < 0)
            {
                Debug.LogError("SpellVFXIsolationSetup: 'UIParticles' layer not found - check ProjectSettings/TagManager.asset, and that the Editor picked up the change (it may need reopening this project).");
                return;
            }

            // Offscreen camera: parked far from any real content, culled to just the
            // UIParticles layer, transparent clear so only the particles paint the RT.
            GameObject cameraGO = new GameObject("SpellBurstCamera", typeof(Camera));
            cameraGO.transform.position = new Vector3(10000f, 10000f, -10f);
            Camera burstCamera = cameraGO.GetComponent<Camera>();
            burstCamera.orthographic = true;
            burstCamera.orthographicSize = 1f;
            burstCamera.nearClipPlane = 0.3f;
            burstCamera.farClipPlane = 20f;
            burstCamera.clearFlags = CameraClearFlags.SolidColor;
            burstCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            burstCamera.cullingMask = 1 << uiParticlesLayer;
            burstCamera.depth = -10;
            burstCamera.tag = "Untagged";

            GameObject spawnPointGO = new GameObject("BurstSpawnPoint");
            spawnPointGO.layer = uiParticlesLayer;
            spawnPointGO.transform.SetParent(cameraGO.transform, worldPositionStays: false);
            spawnPointGO.transform.localPosition = new Vector3(0f, 0f, 10f);

            // Display surface: a plain RawImage inside GameCanvas, last sibling so it draws
            // above BoardBackground/BoardPanel/cards - ordinary Overlay-canvas sibling order,
            // no special sorting layer needed since it's just another UI graphic now.
            GameObject displayGO = new GameObject("SpellBurstDisplay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            displayGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            RectTransform displayRect = (RectTransform)displayGO.transform;
            displayRect.anchorMin = new Vector2(0.5f, 0.5f);
            displayRect.anchorMax = new Vector2(0.5f, 0.5f);
            displayRect.pivot = new Vector2(0.5f, 0.5f);
            displayRect.sizeDelta = new Vector2(200f, 200f);
            displayRect.anchoredPosition = Vector2.zero;
            RawImage displayImage = displayGO.GetComponent<RawImage>();
            displayImage.raycastTarget = false;
            displayRect.SetAsLastSibling();

            GameObject controllerGO = new GameObject("SpellVFXController");
            UIParticleBurstRenderer burstRenderer = controllerGO.AddComponent<UIParticleBurstRenderer>();
            burstRenderer.burstCamera = burstCamera;
            burstRenderer.burstSpawnPoint = spawnPointGO.transform;
            burstRenderer.displayImage = displayImage;

            ParticleRenderIsolationTest isolationTest = controllerGO.AddComponent<ParticleRenderIsolationTest>();
            isolationTest.burstRenderer = burstRenderer;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("SpellVFXIsolationSetup: rig created (SpellBurstCamera, SpellVFXController, SpellBurstDisplay under GameCanvas). Save the scene, then press Play to confirm a white particle burst renders centered on screen, above the board and cards.");
            Selection.activeGameObject = controllerGO;
        }
    }
}
