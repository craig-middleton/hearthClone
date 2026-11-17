using HearthstoneClone.Core;
using UnityEngine;
namespace HearthstoneClone.UI
{
    // Original resource meshes live directly in the recovered board frame.
    // The board camera renders them with the existing frame recess and source curves.
    public class NativeManaRack : MonoBehaviour
    {
        public Transform boardRoot;
        public Mesh sourceMesh;
        public Material crystalMaterial;
        public NativeManaLibrary library;
        static readonly Vector3 SourceOrigin = new Vector3(-3.674102f, .06766415f, -10.03144f);
        const float ResourceScale = .8152721f;
        static readonly Vector3 GemScale = Vector3.one * 1.2f;
        GameObject stage;
        Player owner;
        int previousMax, previousCurrent;
        Slot[] slots;
        sealed class Slot
        {
            public Transform resource, gem;
            public MeshRenderer renderer;
            public NativeManaClip clip;
            public float started;
            public MaterialPropertyBlock paint = new MaterialPropertyBlock();
        }
        void Initialize()
        {
            if(stage!=null)return;
            stage=new GameObject("Original mana crystals");stage.transform.SetParent(boardRoot,false);
            stage.layer=boardRoot.gameObject.layer;
            slots=new Slot[10];
            for(int i=0;i<slots.Length;i++)
            {
                var resource=new GameObject("Resource "+i);resource.layer=stage.layer;resource.transform.SetParent(stage.transform,false);
                resource.transform.localScale=Vector3.one*ResourceScale;
                var gem=new GameObject("Gem_Mana",typeof(MeshFilter),typeof(MeshRenderer));gem.layer=stage.layer;gem.transform.SetParent(resource.transform,false);
                gem.transform.localScale=GemScale;gem.GetComponent<MeshFilter>().sharedMesh=sourceMesh;
                var renderer=gem.GetComponent<MeshRenderer>();renderer.sharedMaterial=crystalMaterial;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                slots[i]=new Slot{resource=resource.transform,gem=gem.transform,renderer=renderer};resource.SetActive(false);
            }
        }
        public void SetMana(Player player)
        {
            if(player==null||!isActiveAndEnabled)return;
            Initialize();bool reset=owner!=player;owner=player;
            int max=Mathf.Clamp(player.MaxMana,0,10), current=Mathf.Clamp(player.CurrentMana,0,max);
            // Original layout advances by the source renderer's world width, with no
            // padding or independent UI fitting (ManaCrystalMgr.UpdateLayout).
            float step=sourceMesh.bounds.size.x*ResourceScale*GemScale.x;
            for(int i=0;i<slots.Length;i++)
            {
                var s=slots[i];s.resource.gameObject.SetActive(i<max);if(i>=max)continue;
                s.resource.localPosition=SourceOrigin+Vector3.right*(step*i);
                bool full=i<current;
                if(reset)StartState(s,full?library.restore:library.used,true);
                else if(i>=previousMax)StartState(s,full?library.birth:library.used,false);
                else if(full!=(i<previousCurrent))StartState(s,full?library.restore:library.used,false);
            }
            previousMax=max;previousCurrent=current;
        }
        void StartState(Slot s, NativeManaClip clip, bool instant)
        {
            s.clip=clip;s.started=Time.time-(instant?clip.duration:0);Sample(s,instant?clip.duration:0);
        }
        static Color Channel(Color c,string property,float value)
        {
            switch(property[property.Length-1]){case 'r':c.r=value;break;case 'g':c.g=value;break;case 'b':c.b=value;break;case 'a':c.a=value;break;}return c;
        }
        void Sample(Slot s,float time)
        {
            Color tint=crystalMaterial.GetColor("_tint"),fade=Color.white;
            Vector3 scale=GemScale,position=Vector3.zero;Quaternion rotation=Quaternion.identity;
            s.renderer.enabled=true;
            foreach(var binding in s.clip.curves)
            {
                string p=binding.property;float value=binding.curve.Evaluate(Mathf.Min(time,s.clip.duration));
                if(p=="m_Enabled")s.renderer.enabled=value>=.5f;
                else if(p.StartsWith("_tint."))tint=Channel(tint,p,value);
                else if(p.StartsWith("_Fade."))fade=Channel(fade,p,value);
                else if(p.StartsWith("m_LocalScale.")){if(p.EndsWith("x"))scale.x=value;else if(p.EndsWith("y"))scale.y=value;else scale.z=value;}
                else if(p.StartsWith("m_LocalPosition.")){if(p.EndsWith("x"))position.x=value;else if(p.EndsWith("y"))position.y=value;else position.z=value;}
                else if(p.StartsWith("m_LocalRotation.")){if(p.EndsWith("x"))rotation.x=value;else if(p.EndsWith("y"))rotation.y=value;else if(p.EndsWith("z"))rotation.z=value;else rotation.w=value;}
            }
            s.gem.localScale=scale;s.gem.localPosition=position;s.gem.localRotation=rotation.normalized;
            s.paint.SetColor("_tint",tint);s.paint.SetColor("_Fade",fade);s.renderer.SetPropertyBlock(s.paint);
        }
        void LateUpdate()
        {
            if(stage==null)return;
            foreach(var s in slots)if(s.resource.gameObject.activeSelf&&s.clip!=null)Sample(s,Time.time-s.started);
        }
        void OnDisable()
        {
            if(stage!=null){stage.SetActive(false);Destroy(stage);}
            stage=null;slots=null;owner=null;previousMax=previousCurrent=0;
        }
    }
}
