using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HearthstoneClone.UI
{
    // The original desktop StartingHandZone and DealStartingCards layout projected
    // through the native board camera. This adapter adds no replacement motion.
    public class NativeMulliganPresentation : MonoBehaviour
    {
        public NativeMulliganArt library;
        public TMP_FontAsset font;
        public Material outlinedText;
        public Camera boardCamera;
        public Vector2 cardSize = new Vector2(245, 367.5f);
        public Vector3 sourceZoneCenter = new Vector3(-7.740872f, .0023448467f, -4.451679f);
        public Vector2 sourceZoneSize = new Vector2(10, 5);
        public float sourceHandHeight = 2.87161303f;
        public float sourceCardScale = 1.1f;
        public float sourceBadgeWidth = 2.1f;
        public float sourceCrossWidth = 1.8f;

        readonly List<CardView> cards = new List<CardView>();
        RectTransform Panel => (RectTransform)transform;

        public Vector3 SourceCardPosition(int index, int count)
        {
            // Adapt the source spacing to the actual visible hand. This local
            // player receives three cards and no Coin: do not reserve a phantom slot.
            float spacing = sourceZoneSize.x / (count + 1);
            return new Vector3(sourceZoneCenter.x - sourceZoneSize.x * .5f + spacing * (1f + index),
                sourceHandHeight, sourceZoneCenter.z - .3f);
        }

        public Vector2 Project(Vector3 world)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Panel,
                boardCamera.WorldToScreenPoint(world), null, out var position);
            return position;
        }

        public Vector2 SourceCardSize(Vector3 world)
        {
            // NativeCardFace's shared 180x270 rectangle represents 2.17x3.255
            // source world units. Preserve the original 1.1 desktop X/Z scale.
            float width = 2.17f * sourceCardScale;
            float height = 3.255f * sourceCardScale;
            return new Vector2(Mathf.Abs(Project(world + Vector3.right * width * .5f).x -
                    Project(world - Vector3.right * width * .5f).x),
                Mathf.Abs(Project(world + Vector3.forward * height * .5f).y -
                    Project(world - Vector3.forward * height * .5f).y));
        }

        public void PrepareCard(RectTransform card)
        {
            if (boardCamera != null) cardSize = SourceCardSize(SourceCardPosition(0, 3));
            card.sizeDelta = cardSize;
        }

        void LateUpdate() => LayoutCards();

        public void LayoutCards()
        {
            if (boardCamera == null) return;
            cards.Clear();
            foreach (Transform child in transform)
            {
                var view = child.GetComponent<CardView>();
                if (view != null && child.gameObject.activeSelf) cards.Add(view);
            }
            for (int i = 0; i < cards.Count; i++)
            {
                var view = cards[i];
                var rect = (RectTransform)view.transform;
                var world = SourceCardPosition(i, cards.Count);
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
                rect.anchoredPosition = Project(world);
                rect.sizeDelta = SourceCardSize(world);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                cardSize = rect.sizeDelta;
                var marker = view.GetComponent<NativeMulliganCard>();
                if (marker == null) continue;
                // CreateNewUILabelAtCardPosition: source y + .1, z - depth/2.6.
                var badgeWorld = world + new Vector3(0, .1f, -sourceZoneSize.y / 2.6f);
                var badgeOffset = Project(badgeWorld) - Project(world);
                float badgeWidth = Mathf.Abs(Project(badgeWorld + Vector3.right * sourceBadgeWidth * .5f).x -
                    Project(badgeWorld - Vector3.right * sourceBadgeWidth * .5f).x);
                SizePart(marker.keep, badgeOffset, badgeWidth, "keep");
                SizePart(marker.replace, badgeOffset, badgeWidth, "replace");
                float crossWidth = Mathf.Abs(Project(world + Vector3.right * sourceCrossWidth * .5f).x -
                    Project(world - Vector3.right * sourceCrossWidth * .5f).x);
                SizePart(marker.cross, Vector2.zero, crossWidth, "cross");
            }
        }

        void SizePart(GameObject item, Vector2 position, float width, string key)
        {
            var rect = (RectTransform)item.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, width / library.Get(key).aspect);
        }

        public void Decorate(CardView view)
        {
            var marker = view.gameObject.AddComponent<NativeMulliganCard>();
            var badgePosition = new Vector2(0, -cardSize.y * .5f - 20);
            marker.keep = Part("Keep", view.transform, "keep", badgePosition, 180).gameObject;
            marker.replace = Part("Replace", view.transform, "replace", badgePosition, 180).gameObject;
            var label = new GameObject("Replace text", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(marker.replace.transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var text = label.GetComponent<TextMeshProUGUI>();
            text.text = "REPLACE"; text.font = font; text.fontSharedMaterial = outlinedText;
            text.fontSize = 25; text.enableAutoSizing = true; text.fontSizeMin = 14; text.fontSizeMax = 25;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            marker.cross = Part("Original red X", view.transform, "cross", Vector2.zero, cardSize.x * .75f).gameObject;
            // Reuse the original silhouette geometry/material, independently of
            // the hand-only eligibility component that correctly hides its own copy.
            var handStyle=view.GetComponent<NativeHandCardPresentation>();
            var source=view.Card.Data.cardType==HearthstoneClone.Cards.CardType.Minion
                ?handStyle?.minionOutline:handStyle?.spellOutline;
            if(source!=null)
            {
                marker.readyOutline=Instantiate(source,source.transform.parent);
                marker.readyOutline.name="Original mulligan selectable silhouette";
                marker.readyOutline.transform.SetAsFirstSibling();
                marker.readyOutline.raycastTarget=false;
            }
            marker.SetSelected(false);
        }

        NativeMulliganPart Part(string name, Transform parent, string key, Vector2 position, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(NativeMulliganPart));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.one * .5f; rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, width / library.Get(key).aspect);
            var part = go.GetComponent<NativeMulliganPart>();
            part.library = library; part.shape = key; part.raycastTarget = false;
            return part;
        }
    }
}
