using System.Linq;
using UnityEngine;
using UnityEngine.Sprites;
namespace HearthstoneClone.UI
{
    // Source meshes remain world-space; existing UI owns combat input/live numbers.
    public class NativeMinionPresentation : MonoBehaviour
    {
        public GameObject actorPrefab,shadowPrefab,tauntPrefab;
        public Texture normalHighlight,tauntHighlight;
        public Vector3 sourceCenter;
        public float sourceWidth;
        public Transform stage,actor,taunt;
        public Camera boardCamera;
        public Renderer highlight;
        MinionView view;Material portrait;MaterialPropertyBlock highlightPaint;
        Texture currentHighlight;Sprite currentArtwork;
        Texture portraitFallback;
        readonly Vector3[] corners=new Vector3[4];
        void Awake(){view=GetComponent<MinionView>();}
        void Initialize()
        {
            if(stage!=null)return;
            var canvas=GetComponentInParent<Canvas>();if(canvas==null)return;
            // Lethal holds add a local sorting Canvas; use the actual root for composition.
            var board=canvas.rootCanvas.GetComponentInChildren<NativeBoardPresentation>(true);
            if(board==null||board.boardCamera==null)return;
            boardCamera=board.boardCamera;
            stage=new GameObject("Original minion "+name).transform;stage.SetParent(board.boardRoot.transform,false);
            actor=Instantiate(actorPrefab,stage).transform;actor.localPosition=Vector3.zero;
            Instantiate(shadowPrefab,stage);
            taunt=Instantiate(tauntPrefab,stage).transform;
            foreach(var renderer in taunt.GetComponentsInChildren<Renderer>(true))renderer.enabled=true;
            var mesh=actor.Find("RootObject/Card_InPlay_Ally/Mesh").GetComponent<MeshRenderer>();
            var materials=mesh.sharedMaterials;portrait=new Material(materials[0]);portraitFallback=portrait.GetTexture("_BaseMap");materials[0]=portrait;mesh.sharedMaterials=materials;
            highlight=actor.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Highlight_Card_Plane");highlightPaint=new MaterialPropertyBlock();
        }
        void LateUpdate()
        {
            if(view==null||view.Minion==null)return;
            Initialize();if(stage==null)return;
            // SetMinion rebinds this legacy artwork after every refresh. It must stay hidden.
            if(view.artworkImage!=null)view.artworkImage.enabled=false;
            if(view.tauntVisual!=null)view.tauntVisual.SetActive(false);
            var rect=(RectTransform)transform;rect.GetWorldCorners(corners);
            var centerScreen=rect.TransformPoint(rect.rect.center);var ray=boardCamera.ScreenPointToRay(centerScreen);
            // Original Card_Play_Ally root's source height, rather than an arbitrary overlay.
            var plane=new Plane(Vector3.up,new Vector3(0,1.3304632f,0));if(!plane.Raycast(ray,out float d))return;
            var center=ray.GetPoint(d);float pixels=Vector3.Distance(corners[0],corners[3]);
            float sourcePixels=Vector3.Distance(boardCamera.WorldToScreenPoint(center-Vector3.right*sourceWidth*.5f),boardCamera.WorldToScreenPoint(center+Vector3.right*sourceWidth*.5f));
            stage.localScale=Vector3.one*pixels/sourcePixels;stage.position=center-stage.TransformVector(sourceCenter);
            bool hasTaunt=view.Minion.HasTaunt;taunt.gameObject.SetActive(hasTaunt);
            var mask=hasTaunt?tauntHighlight:normalHighlight;
            if(mask!=currentHighlight){highlightPaint.SetTexture("_MainTex",mask);highlight.SetPropertyBlock(highlightPaint);currentHighlight=mask;}
            highlight.enabled=view.PresentationSelected||(view.PresentationShowsEligibility&&view.Minion.CanAttack);
            var art=view.Minion.Artwork;if(art!=currentArtwork)
            {
                var crop=art!=null?DataUtility.GetOuterUV(art):new Vector4(0,0,1,1);
                portrait.SetTexture("_BaseMap",art!=null?art.texture:portraitFallback);
                portrait.SetTextureScale("_BaseMap",new Vector2(crop.z-crop.x,crop.w-crop.y));
                portrait.SetTextureOffset("_BaseMap",new Vector2(crop.x,crop.y));currentArtwork=art;
            }
            SetLabel(view.attackText,actor.Find("RootObject/AttackObject/Gem_Attack"));
            SetLabel(view.healthText,actor.Find("RootObject/HealthObject/Gem_Health"));
            view.minionBackground.color=Color.clear;
        }
        void SetLabel(TMPro.TMP_Text label,Transform gem)
        {if(label!=null&&gem!=null)label.rectTransform.position=boardCamera.WorldToScreenPoint(gem.GetComponent<Renderer>().bounds.center);}
        void OnDisable(){if(stage!=null)stage.gameObject.SetActive(false);}
        void OnEnable(){if(stage!=null)stage.gameObject.SetActive(true);}
        void OnDestroy(){if(stage!=null)Destroy(stage.gameObject);if(portrait!=null)Destroy(portrait);}
    }
}
