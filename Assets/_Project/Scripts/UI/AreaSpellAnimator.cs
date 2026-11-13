using System.Collections;
using System.Collections.Generic;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    // One isolated offscreen stage per cast: simultaneous AI spells never move another
    // spell's render texture. Rules have already resolved; this component only presents them.
    public class AreaSpellAnimator : MonoBehaviour
    {
        public UIParticleBurstRenderer rendererTemplate;
        public TMP_FontAsset font;
        public Material numberMaterial;
        public Material compositeMaterial;
        public AudioClip fireSound;
        public AudioClip arcaneSound;
        public AudioClip frostSound;
        [Range(0, 1)] public float soundVolume = .65f;

        private Board board;
        private BoardDisplay playerBoard, opponentBoard;
        private FaceView playerFace, opponentFace;
        private AudioSource audioSource;
        private Material glowMaterial, shardMaterial;
        private Texture2D glowTexture, shardTexture;
        private int castSequence;
        private readonly List<CastStage> stages = new List<CastStage>();

        private sealed class Hit
        {
            public Minion minion;
            public BoardDisplay display;
            public MinionView heldView;
            public Vector3 position;
        }
        private sealed class CastStage
        {
            public GameObject root, overlay;
            public Camera camera;
            public RenderTexture texture;
            public Rect bounds;
            public Vector2 source;
            public readonly List<Hit> hits = new List<Hit>();
            public Vector3 World(Vector2 screen) => camera.transform.position + camera.transform.forward * 10f
                + camera.transform.right * ((screen.x - bounds.center.x) * .02f)
                + camera.transform.up * ((screen.y - bounds.center.y) * .02f);
        }

        public void Bind(Board model, BoardDisplay friendly, BoardDisplay enemy, FaceView friendlyFace, FaceView enemyFace)
        {
            StopAllCoroutines();
            foreach (var stage in stages) Release(stage);
            stages.Clear();
            board = model; playerBoard = friendly; opponentBoard = enemy;
            playerFace = friendlyFace; opponentFace = enemyFace;
        }

        public void PlayCast(CardData card, Player caster, IReadOnlyList<Minion> affected)
        {
            if (!(card.onPlayEffect is IAreaDamageEffect area) || affected == null || rendererTemplate == null) return;
            var targetBoard = caster == board.PlayerOne ? opponentBoard : playerBoard;
            // Cone of Cold can target either side. Use the actual affected minion's owner.
            if (affected.Count > 0)
                targetBoard = board.GetOwnerOf(affected[0]) == board.PlayerOne ? playerBoard : opponentBoard;
            if (targetBoard == null) return;
            var bounds = targetBoard.GetPanelScreenBounds();
            bounds = Rect.MinMaxRect(bounds.xMin - 110, bounds.yMin - 130, bounds.xMax + 110, bounds.yMax + 150);
            var stage = CreateStage(bounds);
            if (stage == null) return;
            var sourceFace = caster == board.PlayerOne ? playerFace : opponentFace;
            stage.source = sourceFace != null ? (Vector2)sourceFace.transform.position : bounds.center;
            foreach (var minion in affected)
            {
                var viewTransform = targetBoard.GetViewTransform(minion);
                if (viewTransform == null) continue;
                var hit = new Hit { minion = minion, display = targetBoard, position = viewTransform.position };
                var view = viewTransform.GetComponent<MinionView>();
                if (minion.IsDead && view != null)
                {
                    view.BeginHold(3f);
                    hit.heldView = view;
                }
                stage.hits.Add(hit);
            }
            stages.Add(stage);
            StartCoroutine(Animate(stage, card, area.DamageAmount));
        }

        private CastStage CreateStage(Rect bounds)
        {
            if (rendererTemplate.burstCamera == null || rendererTemplate.displayImage == null) return null;
            EnsureMaterials();
            var stage = new CastStage { bounds = bounds, root = new GameObject("AreaSpellStage") };
            stage.root.SetActive(false);
            var cameraObject = Instantiate(rendererTemplate.burstCamera.gameObject, stage.root.transform);
            stage.camera = cameraObject.GetComponent<Camera>();
            cameraObject.name = "SpellCaptureCamera"; cameraObject.tag = "Untagged";
            cameraObject.transform.position = new Vector3(1000 + (++castSequence) * 100, 0, -10);
            cameraObject.transform.rotation = Quaternion.identity;
            stage.camera.orthographic = true;
            stage.camera.orthographicSize = bounds.height * .01f;
            stage.camera.clearFlags = CameraClearFlags.SolidColor;
            stage.camera.backgroundColor = Color.clear;
            int height = 384;
            int width = Mathf.Clamp(Mathf.CeilToInt(height * bounds.width / bounds.height), 128, 2048);
            stage.texture = new RenderTexture(new RenderTextureDescriptor(width, height)
            {
                graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR),
                depthStencilFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil),
                msaaSamples = 1
            }) { name = "AreaSpellRT" };
            stage.camera.targetTexture = stage.texture;
            stage.camera.aspect = bounds.width / bounds.height;
            var canvas = rendererTemplate.displayImage.canvas.rootCanvas;
            stage.overlay = new GameObject("AreaSpellOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            stage.overlay.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)stage.overlay.transform;
            rect.sizeDelta = bounds.size / canvas.scaleFactor;
            rect.position = bounds.center;
            var image = stage.overlay.GetComponent<RawImage>();
            image.raycastTarget = false; image.texture = stage.texture; image.material = compositeMaterial;
            // Lethal targets use sorting order 1 while held for their death reaction.
            // This decorative canvas stays above them and has no input raycaster.
            var effectCanvas = stage.overlay.AddComponent<Canvas>();
            effectCanvas.overrideSorting = true; effectCanvas.sortingOrder = 10;
            stage.root.SetActive(true);
            return stage;
        }

        private IEnumerator Animate(CastStage stage, CardData card, int damage)
        {
            Color color = card.spellSchool == SpellSchool.Fire ? new Color(1, .45f, .06f) :
                card.spellSchool == SpellSchool.Arcane ? new Color(.7f, .25f, 1) : new Color(.4f, .85f, 1);
            var title = Label(stage, card.cardName.ToUpperInvariant(), stage.bounds.center + Vector2.up * 80, 28, color);
            if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.spatialBlend = 0; }
            AudioClip clip = card.spellSchool == SpellSchool.Fire ? fireSound : card.spellSchool == SpellSchool.Arcane ? arcaneSound : frostSound;
            if (clip != null) audioSource.PlayOneShot(clip, soundVolume);
            yield return new WaitForSeconds(.16f);

            if (card.spellSchool == SpellSchool.Arcane) Nova(stage);
            else if (card.spellSchool == SpellSchool.Frost) FrostFan(stage);
            else if (stage.hits.Count == 0)
            {
                for (int i = -2; i <= 2; i++) Burst(stage, stage.bounds.center + new Vector2(i * 130, 0), card.spellSchool);
            }
            foreach (var hit in stage.hits) Burst(stage, hit.position, card.spellSchool);
            yield return new WaitForSeconds(.16f);
            if (title != null) Destroy(title.gameObject);
            var labels = new List<TMP_Text>();
            foreach (var hit in stage.hits)
            {
                Transform current = hit.heldView != null ? hit.heldView.transform : hit.display.GetViewTransform(hit.minion);
                var view = current != null ? current.GetComponent<MinionView>() : null;
                if (view != null) view.PlayDamageReaction();
                labels.Add(Label(stage, "-" + damage, current != null ? current.position : hit.position, 46, Color.white));
            }
            float elapsed = 0;
            while (elapsed < .95f)
            {
                elapsed += Time.deltaTime;
                foreach (var label in labels)
                {
                    if (label == null) continue;
                    label.transform.position += Vector3.up * (Time.deltaTime * 65);
                    label.alpha = Mathf.Clamp01((1 - elapsed) * 2);
                }
                yield return null;
            }
            Release(stage); stages.Remove(stage);
        }

        private TMP_Text Label(CastStage stage, string text, Vector2 screen, float size, Color color)
        {
            var go = new GameObject("SpellLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(stage.overlay.transform, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(700, 90); rect.position = screen;
            var label = go.GetComponent<TextMeshProUGUI>(); label.font = font != null ? font : TMP_Settings.defaultFontAsset;
            if (numberMaterial != null) label.fontSharedMaterial = numberMaterial;
            label.text = text; label.fontSize = size; label.color = color; label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private ParticleSystem Emitter(CastStage stage, string name, Vector2 point, Material material)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.layer = rendererTemplate.BurstSpawnPoint.gameObject.layer;
            go.transform.SetParent(stage.root.transform, false); go.transform.position = stage.World(point);
            var ps = go.GetComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = .65f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.45f, .9f); main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(.12f, .32f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, 0));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            ps.Play(); return ps;
        }

        private void Burst(CastStage stage, Vector2 point, SpellSchool school)
        {
            bool fire = school == SpellSchool.Fire;
            bool frost = school == SpellSchool.Frost;
            var ps = Emitter(stage, fire ? "FirePillar" : frost ? "IceFan" : "ArcaneImpact", point, frost ? shardMaterial : glowMaterial);
            if (fire)
            {
                var core = Emitter(stage, "FireImpactFlash", point, glowMaterial);
                for (int i = 0; i < 5; i++)
                    core.Emit(new ParticleSystem.EmitParams { position = stage.World(point), velocity = Vector3.zero,
                        startSize = Random.Range(1.2f, 2f), startLifetime = .32f,
                        startColor = new Color(1, .55f, .12f, .65f) }, 1);
            }
            var colors = ps.colorOverLifetime; colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(Color.white, 0),
                new GradientColorKey(fire ? new Color(1,.55f,.02f) : frost ? new Color(.45f,.85f,1) : new Color(.65f,.2f,1), .25f),
                new GradientColorKey(fire ? new Color(.5f,.06f,.005f) : new Color(.15f,.2f,.75f), 1)
            }, new[] { new GradientAlphaKey(1,0), new GradientAlphaKey(.8f,.5f), new GradientAlphaKey(0,1) });
            colors.color = gradient;
            for (int i = 0; i < (fire ? 90 : 55); i++)
            {
                float angle = Random.Range(0, Mathf.PI * 2);
                var velocity = fire ? new Vector3(Random.Range(-.7f,.7f),Random.Range(1f,4f),0) :
                    new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0) * Random.Range(.5f,2f);
                var particle = new ParticleSystem.EmitParams {
                    position = stage.World(point + new Vector2(Random.Range(-25,25), fire ? -60 : 0)),
                    velocity = velocity * (fire ? 1.1f : 1),
                    startSize = Random.Range(fire ? .18f : .10f, fire ? .55f : .24f),
                    startLifetime = Random.Range(.5f,.95f),
                    startColor = Color.white,
                    rotation = frost ? 45 : Random.Range(0,360)
                };
                ps.Emit(particle, 1);
            }
        }

        private void Nova(CastStage stage)
        {
            var ps = Emitter(stage, "ArcaneShockwave", stage.bounds.center, glowMaterial);
            var colors = ps.colorOverLifetime; colors.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(new Color(.8f,.5f,1),0),new GradientColorKey(new Color(.3f,.05f,.9f),1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)}); colors.color = gradient;
            float speed = stage.bounds.width * .02f / 1.4f;
            for(int i=0;i<100;i++)
            {
                float a=i*Mathf.PI*2/100;
                ps.Emit(new ParticleSystem.EmitParams { position=stage.World(stage.bounds.center), velocity=new Vector3(Mathf.Cos(a)*speed,Mathf.Sin(a)*speed*.25f,0), startSize=.18f,startLifetime=.65f,startColor=Color.white },1);
            }
        }

        private void FrostFan(CastStage stage)
        {
            float direction = stage.bounds.center.y >= stage.source.y ? 1 : -1;
            Vector2 origin = stage.bounds.center + Vector2.down * (direction * stage.bounds.height * .34f);
            var ps = Emitter(stage, "ConeOfColdFan", origin, shardMaterial);
            for (int i = 0; i < 85; i++)
            {
                float angle = Random.Range(-.75f,.75f);
                ps.Emit(new ParticleSystem.EmitParams {
                    position = stage.World(origin),
                    velocity = new Vector3(Mathf.Sin(angle), direction * Mathf.Cos(angle), 0) * Random.Range(2.5f,5.5f),
                    startSize = Random.Range(.10f,.26f), startLifetime = Random.Range(.45f,.8f),
                    startColor = new Color(.6f,.9f,1), rotation = 45
                }, 1);
            }
        }

        private void EnsureMaterials()
        {
            if (glowMaterial != null) return;
            glowTexture = MakeTexture(false); shardTexture = MakeTexture(true);
            glowMaterial = MakeMaterial(glowTexture); shardMaterial = MakeMaterial(shardTexture);
        }
        private static Texture2D MakeTexture(bool shard)
        {
            var texture = new Texture2D(32,32,TextureFormat.RGBA32,false) { name = shard ? "IceShard" : "SpellGlow", wrapMode = TextureWrapMode.Clamp };
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                float dx=(x-15.5f)/15.5f,dy=(y-15.5f)/15.5f;
                float a=shard ? Mathf.Clamp01((1-Mathf.Abs(dx)-Mathf.Abs(dy)) * 5) : Mathf.Pow(Mathf.Clamp01(1-Mathf.Sqrt(dx*dx+dy*dy)),1.5f);
                texture.SetPixel(x,y,new Color(1,1,1,a));
            }
            texture.Apply(); return texture;
        }
        private static Material MakeMaterial(Texture texture)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap",texture); material.mainTexture=texture;
            material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.One);
            material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            return material;
        }
        private void Release(CastStage stage)
        {
            foreach(var hit in stage.hits)
            {
                if(hit.heldView==null)continue;
                hit.heldView.EndHold();
                if(hit.minion.IsDead && !hit.heldView.IsHeld)Destroy(hit.heldView.gameObject);
            }
            if(stage.camera!=null){stage.camera.enabled=false;stage.camera.targetTexture=null;}
            if(stage.overlay!=null)stage.overlay.SetActive(false);
            if(stage.texture!=null){stage.texture.Release();Destroy(stage.texture);}
            if(stage.overlay!=null)Destroy(stage.overlay);
            if(stage.root!=null)Destroy(stage.root);
        }
        private void OnDestroy()
        {
            foreach(var stage in stages)Release(stage);
            if(glowMaterial!=null)Destroy(glowMaterial);if(shardMaterial!=null)Destroy(shardMaterial);
            if(glowTexture!=null)Destroy(glowTexture);if(shardTexture!=null)Destroy(shardTexture);
        }
    }
}
