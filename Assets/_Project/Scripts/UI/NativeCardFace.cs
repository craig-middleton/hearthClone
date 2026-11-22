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
            var name=library.NameFor(data);
            foreach(var part in GetComponentsInChildren<NativeCardLayer>(true))
            {
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
        }
    }
}
