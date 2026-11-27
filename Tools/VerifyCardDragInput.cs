using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Uses the real shared prefab in a disposable Canvas; leaves match state alone.
public static class VerifyCardDragInput
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    static T Field<T>(object owner, string name)
        => (T)owner.GetType().GetField(name, Private).GetValue(owner);
    static bool SourceVisible(CardView view)
    {
        var group = view.GetComponent<CanvasGroup>();
        return view.gameObject.activeInHierarchy && (group == null || group.alpha > 0) &&
            view.nativeFace != null &&
            (view.nativeFace.minionFace.activeInHierarchy || view.nativeFace.spellFace.activeInHierarchy);
    }

    static void VerifyCancelKeys(Transform parent, EffectTester live, CardView view,
        HandFanLayout fan, CardDragResolver resolver, PointerEventData left)
    {
        // InputSystem.Update also notifies existing action listeners. Require an idle
        // real match so these virtual cancellation inputs cannot discard its aim/drag.
        var liveResolver = Field<CardDragResolver>(live, "cardDragResolver");
        Check(!Field<bool>(live, "powerPending") && (liveResolver == null || !liveResolver.DragInProgress),
            "Run virtual cancellation checks with no live card drag or pending hero target");
        var fixtureRoot = new GameObject("Disposable cancellation poll fixture");
        fixtureRoot.SetActive(false);
        fixtureRoot.transform.SetParent(parent, false);
        var fixture = fixtureRoot.AddComponent<EffectTester>();
        fixture.enabled = false; // Never runs Start or constructs another match.
        typeof(EffectTester).GetField("cardDragResolver", Private).SetValue(fixture, resolver);
        var tick = typeof(EffectTester).GetMethod("Update", Private);
        var oldKeyboard = Keyboard.current;
        var oldMouse = Mouse.current;
        var settings = InputSystem.settings;
        var oldEditorBehavior = settings.editorInputBehaviorInPlayMode;
        Vector2 position = oldMouse != null ? oldMouse.position.ReadValue() : left.position;
        Keyboard keyboard = null;
        Mouse mouse = null;
        Action afterUpdate = null;
        bool pending = false, escapeInput = false, handled = false;
        try
        {
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("HearthClone cancellation keyboard");
            mouse = InputSystem.AddDevice<Mouse>("HearthClone cancellation mouse");
            afterUpdate = () =>
            {
                // API-script continuations can read the separate Editor buffer.
                // Run the production poll in the same input buffer as gameplay.
                if (!pending || InputState.currentUpdateType == InputUpdateType.Editor) return;
                keyboard.MakeCurrent(); mouse.MakeCurrent();
                if (!(escapeInput ? keyboard.escapeKey.wasPressedThisFrame : mouse.rightButton.wasPressedThisFrame)) return;
                tick.Invoke(fixture, null);
                handled = true;
            };
            InputSystem.onAfterUpdate += afterUpdate;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            InputSystem.Update();

            foreach (bool escape in new[] { true, false })
            {
                ExecuteEvents.Execute(view.gameObject, left, ExecuteEvents.beginDragHandler);
                var ghost = Field<GameObject>(view, "dragGhost");
                Check(ghost != null && resolver.DragInProgress && Field<bool>(fan, "dragActive"),
                    "Virtual cancellation did not start an accepted drag");
                escapeInput = escape; handled = false; pending = true;
                if (escape) InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                else InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Right));
                InputSystem.Update();
                pending = false;
                Check(handled, "Virtual cancellation input was not observed by the gameplay input update");
                Check(!resolver.DragInProgress && !Field<bool>(view, "dragActive") && !Field<bool>(fan, "dragActive") &&
                    Field<GameObject>(view, "dragGhost") == null && !ghost.activeSelf && SourceVisible(view),
                    (escape ? "Escape" : "Right click") + " did not cancel through EffectTester.Update or hid the source card");
                ExecuteEvents.Execute(view.gameObject, left, ExecuteEvents.endDragHandler);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                InputSystem.Update();
            }
        }
        finally
        {
            if (afterUpdate != null) InputSystem.onAfterUpdate -= afterUpdate;
            try
            {
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            }
            finally
            {
                if (settings != null) settings.editorInputBehaviorInPlayMode = oldEditorBehavior;
                if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
                if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
                UnityEngine.Object.Destroy(fixtureRoot);
            }
        }
    }

    public static async Task<string> Main()
    {
        Check(EditorApplication.isPlaying, "Play mode required");
        var root = new GameObject("Disposable card input fixture", typeof(RectTransform), typeof(Canvas));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        try
        {
            var panel = new GameObject("Disposable input hand", typeof(RectTransform), typeof(HandFanLayout));
            panel.transform.SetParent(root.transform, false);
            var fan = panel.GetComponent<HandFanLayout>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/HearthstoneTable/TableCard.prefab");
            var view = UnityEngine.Object.Instantiate(prefab, panel.transform).GetComponent<CardView>();
            var source = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
            var card = new CardInstance(source.cardPool.Find(c => c != null));
            var one = new Player("Input One");
            var two = new Player("Input Two");
            var board = new Board(one, two);
            var turns = new TurnManager(board);
            var resolver = new CardDragResolver(one, two, board, new GameContext(board),
                new PlayerHand(one, new List<CardData>()), new PlayerHand(two, new List<CardData>()),
                turns, null, null, null, null, null, () => false, () => true, () => true, null);
            int began = 0, ended = 0;
            view.SetCard(card, (_, __, ___) => ended++,
                (c, v) => { began++; resolver.OnCardDragBegan(c, v); }, () => true);
            fan.ApplyLayout(new List<CardView> { view });
            var left = new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left, pointerId = -1, position = new Vector2(300, 300) };
            var right = new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Right, pointerId = -2, position = new Vector2(500, 500) };
            var middle = new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Middle, pointerId = -3, position = new Vector2(500, 500) };
            var other = new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left, pointerId = 42, position = new Vector2(600, 600) };
            var sameDeviceRight = new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Right, pointerId = -1, position = new Vector2(650, 650) };

            foreach (var rejected in new[] { right, middle })
            {
                ExecuteEvents.Execute(view.gameObject, rejected, ExecuteEvents.beginDragHandler);
                ExecuteEvents.Execute(view.gameObject, rejected, ExecuteEvents.endDragHandler);
            }
            Check(began == 0 && ended == 0 && Field<GameObject>(view, "dragGhost") == null,
                "Right/middle drag started or resolved a card");
            Check(!Field<bool>(fan, "dragActive"), "Rejected mouse button blocked fan interaction");

            ExecuteEvents.Execute(view.gameObject, left, ExecuteEvents.beginDragHandler);
            var ghost = Field<GameObject>(view, "dragGhost");
            Check(began == 1 && ghost != null && resolver.DragInProgress && Field<bool>(fan, "dragActive"),
                "Left drag failed to begin or did not lock the fan");
            Vector3 before = ghost.transform.position;
            ExecuteEvents.Execute(view.gameObject, other, ExecuteEvents.beginDragHandler);
            foreach (var foreign in new[] { other, right, sameDeviceRight })
            {
                ExecuteEvents.Execute(view.gameObject, foreign, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(view.gameObject, foreign, ExecuteEvents.endDragHandler);
            }
            Check(began == 1 && ended == 0 && Field<GameObject>(view, "dragGhost") == ghost &&
                ghost.transform.position == before && Field<bool>(fan, "dragActive"),
                "Foreign pointer replaced/moved/released the ghost or unlocked its fan");
            ExecuteEvents.Execute(view.gameObject, left, ExecuteEvents.endDragHandler);
            Check(ended == 1 && Field<GameObject>(view, "dragGhost") == null && !Field<bool>(fan, "dragActive"),
                "Accepted left release did not resolve once and restore its fan");
            resolver.CancelDrag();

            ExecuteEvents.Execute(view.gameObject, left, ExecuteEvents.beginDragHandler);
            var cancelledGhost = Field<GameObject>(view, "dragGhost");
            Check(cancelledGhost != null && cancelledGhost.activeInHierarchy && Field<bool>(fan, "dragActive"),
                "Cancellation fixture has no visible accepted ghost or fan lock");
            resolver.CancelDrag();
            ExecuteEvents.Execute(view.gameObject, left, ExecuteEvents.endDragHandler);
            Check(!resolver.DragInProgress && !Field<bool>(view, "dragActive") && !Field<bool>(fan, "dragActive") &&
                Field<GameObject>(view, "dragGhost") == null && !cancelledGhost.activeSelf && ended == 1,
                "Cancellation retained a visible ghost/input lock or late release resolved the cancelled card");
            Check(view.Card == card && SourceVisible(view),
                "Cancellation hid, replaced or removed the accepted source card");
            VerifyCancelKeys(root.transform, source, view, fan, resolver, left);
            Check(ended == 1 && view.Card == card, "Cancellation key allowed a late card drop or changed the source card");
            await Task.Delay(30);
            Check(root.GetComponentsInChildren<CardView>(true).Length == 1, "Input fixture leaked a drag ghost");
            return "Right/middle drags rejected; accepted pointer owns ghost/fan/drop; foreign pointers inert; public, Escape and right-click cancellation clear resolver/card/fan and preserve source visibility; late releases inert; temporary input devices restored. Active match untouched.";
        }
        finally
        {
            root.SetActive(false);
            UnityEngine.Object.Destroy(root);
        }
    }
}
