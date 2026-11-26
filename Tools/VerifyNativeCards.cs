using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Disposable live UI checks. Does not save scenes/assets, play probe cards or persist a deck.
public static class VerifyNativeCards
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static async Task Frame()
    {
        int before = Time.frameCount; var timer = System.Diagnostics.Stopwatch.StartNew();
        while (before == Time.frameCount)
        {
            if (timer.Elapsed.TotalSeconds > 10) throw new Exception("Game frames are not advancing");
            await Task.Delay(20);
        }
    }
    static List<RaycastResult> Hits(Vector2 position)
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
        return hits;
    }
    static void Click(Button button) => ExecuteEvents.Execute(button.gameObject,
        new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = button.transform.position }, ExecuteEvents.pointerClickHandler);
    static NativeCardLayer[] VisibleLayers(CardView view) => view.GetComponentsInChildren<NativeCardLayer>().Where(p => p.isActiveAndEnabled).ToArray();
    static void Face(CardView view, CardData data)
    {
        var face = view.GetComponent<NativeCardFace>();
        Check(face != null && face.library != null, "Card is missing its native presentation/library");
        bool minion = data.cardType == CardType.Minion;
        Check(face.minionFace.activeSelf == minion && face.spellFace.activeSelf != minion, "Native face variant does not follow card type");
        Check(view.nameText.text == data.cardName && view.costText.text == data.manaCost.ToString(), "Card name/cost no longer follow its data");
        Check(view.descriptionText.text == data.description, "Card description does not follow its data");
        if (minion) Check(view.attackText.text == data.attack.ToString() && view.healthText.text == data.health.ToString(), "Minion stats no longer follow card data");
        var layers = VisibleLayers(view);
        Check(layers.Length >= 4 && layers.All(p => p.mainTexture != null && p.GetComponent<CanvasRenderer>()!=null && !p.raycastTarget && p.maskable), "Native mesh layers are missing renderer/paint, intercept input or bypass clipping");
        Check(layers.Any(p => p.artwork == data.artwork && p.mainTexture == data.artwork.texture), "Native portrait did not receive this card's artwork");
        Check(layers.Any(p => p.nameTexture == (face.study!=null&&face.study.Find(data)!=null?face.study.Find(data).library:face.library).NameFor(data) && p.mainTexture == p.nameTexture), "Original curved-name geometry has the wrong native-font name texture");
        foreach(var label in face.labels.Where(l=>l.text.gameObject.activeInHierarchy))
        {
            if(label.role==NativeCardRole.Description)Check(label.text.text==data.description,"Visible native description is stale");
            if(label.role==NativeCardRole.Cost)Check(label.text.text==data.manaCost.ToString(),"Visible native cost is stale");
            if(label.role==NativeCardRole.Attack)Check(minion&&label.text.text==data.attack.ToString(),"Visible native attack is stale");
            if(label.role==NativeCardRole.Health)Check(minion&&label.text.text==data.health.ToString(),"Visible native health is stale");
        }
        Check(view.GetComponentsInChildren<Canvas>().Length == 0, "A nested Canvas would detach the card from fan input/draw order");
    }
    static void Tint(CardView view, Color expected)
    {
        Check(VisibleLayers(view).All(p => Vector4.Distance(p.color, expected) < .01f), "Mulligan tint did not reach every native mesh layer");
        Check(view.cardBackground.color.a < .01f, "Legacy frame became visible over the native card");
    }
    public static async Task<string[]> Main()
    {
        Check(EditorApplication.isPlaying, "Play mode only");
        var game = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        Check(game != null && game.frontEnd != null, "Open the playable game scene");
        game.frontEnd.ShowMenu();
        var minion = game.cardPool.First(c => c.cardType == CardType.Minion && c.artwork != null && !string.IsNullOrEmpty(c.description));
        var spell = game.cardPool.First(c => c.cardType == CardType.Spell && c.artwork != null && !string.IsNullOrEmpty(c.description));
        var canvas = game.frontEnd.GetComponentInParent<Canvas>().rootCanvas;
        GameObject stage = null;
        try
        {
            stage = new GameObject("Disposable native card interaction probe", typeof(RectTransform), typeof(HandFanLayout));
            stage.transform.SetParent(game.frontEnd.mainMenu.transform, false);
            var stageRect = (RectTransform)stage.transform; stageRect.anchoredPosition = new Vector2(-430, 0); stageRect.sizeDelta = new Vector2(500, 400);
            var probe = UnityEngine.Object.Instantiate(game.cardViewPrefab, stage.transform).GetComponent<CardView>();
            var fan = stage.GetComponent<HandFanLayout>();
            probe.SetCard(new CardInstance(minion)); fan.ApplyLayout(new List<CardView> { probe }); await Frame(); Face(probe, minion);
            probe.SetCard(new CardInstance(spell)); await Frame(); Face(probe, spell);
            probe.SetCard(new CardInstance(minion)); await Frame(); Face(probe, minion);
            probe.SetCard(new CardInstance(spell), faceDown: true); await Frame();
            var face = probe.GetComponent<NativeCardFace>();
            Check(!probe.faceUpDetails.activeSelf && !face.minionFace.activeInHierarchy && !face.spellFace.activeInHierarchy && VisibleLayers(probe).Length == 0, "Face-down card exposes native front geometry");
            Check(probe.nameText.text == "" && probe.costText.text == "" && !probe.artworkImage.enabled, "Face-down card exposes readable fields");

            int toggles = 0;
            var instance = new CardInstance(minion);
            probe.SetCardForMulligan(instance, selected => { Check(ReferenceEquals(selected, instance), "Mulligan lost per-copy identity"); toggles++; });
            await Frame(); Face(probe, minion);
            Check(Hits(probe.transform.position).FirstOrDefault().gameObject?.GetComponentInParent<CardView>() == probe, "Native decorations intercept the card's first raycast");
            Click(probe.button); Tint(probe, probe.selectedForMulliganColor);
            Click(probe.button); Tint(probe, probe.normalColor);
            Check(toggles == 2, "Mulligan clicks no longer toggle exactly once");

            bool began = false, ended = false;
            probe.SetCard(instance, (_, __, ___) => ended = true, (_, __) => began = true, () => true);
            fan.ApplyLayout(new List<CardView> { probe }); fan.SetHovered(probe);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (probe.transform.localScale.x < 1.03f && timer.Elapsed.TotalSeconds < 2) await Frame();
            Check(probe.transform.localScale.x > 1.02f, "Native card no longer scales with fan hover");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = probe.transform.position };
            ExecuteEvents.Execute(probe.gameObject, pointer, ExecuteEvents.beginDragHandler); await Frame();
            var ghost = (GameObject)typeof(CardView).GetField("dragGhost", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(probe);
            Check(began && ghost != null, "Eligible drag did not create the existing ghost");
            Check(ghost.transform.parent == canvas.transform && Quaternion.Angle(ghost.transform.localRotation, Quaternion.identity) < .01f && Vector3.Distance(ghost.transform.localScale, Vector3.one) < .01f, "Drag ghost is not upright at the root Canvas");
            var ghostLayers = ghost.GetComponentsInChildren<NativeCardLayer>().Where(p => p.isActiveAndEnabled).ToArray();
            var sourceLayers = VisibleLayers(probe);
            Check(ghostLayers.Length == sourceLayers.Length && ghostLayers.All(p => p.mainTexture != null), "Cloning the native card produced blank/missing ghost layers");
            Check(!ghost.GetComponent<CanvasGroup>().blocksRaycasts, "Ghost intercepts drop raycasts");
            pointer.position += new Vector2(250, 0); ExecuteEvents.Execute(probe.gameObject, pointer, ExecuteEvents.dragHandler); await Frame();
            Check(Hits(pointer.position).All(h => !h.gameObject.transform.IsChildOf(ghost.transform)), "A native ghost layer intercepts the drop target");
            ExecuteEvents.Execute(probe.gameObject, pointer, ExecuteEvents.endDragHandler); await Frame();
            Check(ended && ghost == null, "Drop callback or ghost cleanup changed");
            Face(probe, minion); Check(sourceLayers.All(p => p.mainTexture != null), "Destroying the ghost destroyed shared source paint");
        }
        finally
        {
            if (stage != null) UnityEngine.Object.Destroy(stage);
            await Frame();
        }

        var front = game.frontEnd;
        front.ChooseDeck(); front.clearButton.onClick.Invoke(); await Frame();
        var entry = front.collectionGrid.GetComponentsInChildren<DeckCollectionEntry>().First();
        Face(entry.view, entry.view.Card.Data);
        Check(Hits(entry.view.transform.position).FirstOrDefault().gameObject?.GetComponentInParent<CardView>() == entry.view, "Collection's native card is not the first input target");
        int before = front.Draft.Count; Click(entry.view.button); await Frame();
        Check(front.Draft.Count == before + 1, "Native collection card click no longer adds one copy");
        front.ShowMenu(); front.Play(); game.confirmMulliganButton.onClick.Invoke(); await Frame();
        var concealed = game.opponentHandDisplay.handPanel.GetComponentsInChildren<CardView>();
        Check(concealed.Length > 0 && concealed.All(v => !v.faceUpDetails.activeSelf && VisibleLayers(v).Length == 0 && v.nameText.text == ""), "AI hand leaks native card fronts");
        bool manual = game.manualControlMode;
        try
        {
            game.manualControlMode = true;
            typeof(EffectTester).GetMethod("RefreshAll", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null); await Frame();
            var revealed = game.opponentHandDisplay.handPanel.GetComponentsInChildren<CardView>();
            Check(revealed.Length > 0, "Manual opponent hand is empty"); foreach (var view in revealed) Face(view, view.Card.Data);
            game.manualControlMode = false;
            typeof(EffectTester).GetMethod("RefreshAll", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null); await Frame();
            Check(game.opponentHandDisplay.handPanel.GetComponentsInChildren<CardView>().All(v => VisibleLayers(v).Length == 0), "Returning to AI control leaves native faces visible");
        }
        finally { game.manualControlMode = manual; game.frontEnd.ShowMenu(); }
        return new[] { "Native minion/spell rebinding preserves names, descriptions, stats, artwork and curved names; face-down fronts are fully hidden", "Real mulligan clicks preserve CardInstance identity and tint native parts; first raycasts reach the card", "Fan hover and upright drag ghost preserve native geometry/paint; ghost never intercepts drops and cleanup leaves source intact", "Collection clicks still add one copy with maskable graphics; AI concealment/manual reveal/reconceal pass" };
    }
}
