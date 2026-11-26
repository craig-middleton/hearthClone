using System;
using HearthstoneClone.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    [Serializable] public class NativeCardText
    {
        public TMP_Text text;
        public NativeCardRole role;
        public Color ink;
    }
    public class NativeCardFace : MonoBehaviour
    {
        public NativeCardLibrary library;
        public GameObject minionFace,spellFace;
        public Graphic[] legacyGraphics;
        public NativeCardText[] labels;
        public TMP_Text fallbackName;
        public NativeCardStudy study;
        public NativeCardLayer[] studyDecorations;
        public TMP_Text studyTribe;
        void OnRectTransformDimensionsChange()
        {
            if(minionFace==null||spellFace==null)return;
            var rect=((RectTransform)transform).rect;
            float scale=Mathf.Min(rect.width/180,rect.height/270);
            minionFace.transform.localScale=spellFace.transform.localScale=Vector3.one*scale;
        }
        // Binding is explicit. Ghost clones retain serialized mesh/text/texture payloads.
        public void Bind(CardData data,bool faceDown,Color tint)
        {
            minionFace.SetActive(!faceDown&&data.cardType==CardType.Minion);
            spellFace.SetActive(!faceDown&&data.cardType==CardType.Spell);
            if(faceDown)return;
            foreach(var graphic in legacyGraphics)if(graphic!=null)graphic.enabled=false;
            var owner=GetComponent<CardView>();
            owner.cardBackground.enabled=true;owner.cardBackground.color=Color.clear;
            var entry=study!=null?study.Find(data):null;
            var activeLibrary=entry!=null?entry.library:library;
            var name=activeLibrary.NameFor(data);
            if(entry!=null)
            {
                for(var parent=transform;parent!=null;parent=parent.parent)
                    if(parent.TryGetComponent<Canvas>(out var canvas))
                        canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.Normal;
            }
            if(studyTribe!=null)
            {
                studyTribe.gameObject.SetActive(entry!=null&&!string.IsNullOrEmpty(entry.tribe));
                if(entry!=null){studyTribe.text=entry.tribe;studyTribe.rectTransform.anchoredPosition=entry.tribePosition;studyTribe.rectTransform.sizeDelta=entry.tribeSize;}
            }
            foreach(var part in GetComponentsInChildren<NativeCardLayer>(true))
            {
                bool decoration=Array.IndexOf(studyDecorations??Array.Empty<NativeCardLayer>(),part)>=0;
                if(decoration)
                {
                    bool show=entry!=null&&!string.IsNullOrEmpty(entry.tribe)&&!part.spell;
                    part.gameObject.SetActive(show);if(!show)continue;
                }
                part.library=activeLibrary;
                if(study!=null)part.material=part.Definition.sourceMaterial!=null?part.Definition.sourceMaterial:
                    part.Definition.role==NativeCardRole.Name?null:study.baselinePaint;
                part.artwork=data.artwork;part.nameTexture=name;
                part.enabled=part.Definition.role!=NativeCardRole.Name||name!=null;
                part.SetVerticesDirty();
                part.SetMaterialDirty(); // Artwork/name textures changed as well as geometry.
            }
            foreach(var label in labels)
                label.text.text=Value(label.role,data);
            fallbackName.gameObject.SetActive(name==null);fallbackName.text=data.cardName;
            SetTint(tint);
        }
        static string Value(NativeCardRole role,CardData data)
        {
            switch(role)
            {
                case NativeCardRole.Cost:return data.manaCost.ToString();
                case NativeCardRole.Attack:return data.attack.ToString();
                case NativeCardRole.Health:return data.health.ToString();
                case NativeCardRole.Description:return data.description;
                case NativeCardRole.School:return data.spellSchool==SpellSchool.None?"":data.spellSchool.ToString();
                default:return data.cardName;
            }
        }
        public void SetTint(Color tint)
        {
            foreach(var part in GetComponentsInChildren<NativeCardLayer>(true))part.color=tint;
            foreach(var label in labels)label.text.color=label.ink*tint;
            fallbackName.color=tint;
            if(studyTribe!=null)studyTribe.color=tint;
        }
    }
}
