using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

// Disposable deadline injection into the actual clock: production duration stays 75 seconds.
public static class VerifyTurnTimer
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static T Field<T>(EffectTester e, string name) => (T)typeof(EffectTester).GetField(name, Private).GetValue(e);
    static void Refresh(EffectTester e) => typeof(EffectTester).GetMethod("RefreshAll", Private).Invoke(e, null);
    static void Tick(EffectTester e) => typeof(EffectTester).GetMethod("Update", Private).Invoke(e, null);
    static void Expire(EffectTester e) { Field<TurnClock>(e, "turnClock").Advance(e.TurnTimeRemaining + 1); Tick(e); }
    static async Task Until(Func<bool> condition, string message)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (!condition()) { if (timer.Elapsed.TotalSeconds > 15) throw new Exception(message); await Task.Delay(20); }
    }
    static void CoreClock()
    {
        var clock = new TurnClock();
        clock.Start(75); clock.Advance(54.5f);
        Check(clock.Active && !clock.Expired && Math.Abs(clock.RemainingSeconds - 20.5f) < .001f, "Clock countdown drift");
        clock.Advance(21); clock.Advance(10);
        Check(clock.Expired && clock.RemainingSeconds == 0, "Expired clock did not clamp at zero");
        clock.Stop(); clock.Advance(10); Check(!clock.Active && !clock.Expired, "Stopped clock expired");
        clock.Start(75); Check(!clock.Expired && clock.RemainingSeconds == 75, "Fresh turn retained old deadline");
    }
    public static async Task<string> Main()
    {
        Check(EditorApplication.isPlaying, "Play mode required"); CoreClock();
        var e = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        bool oldManual = e.manualControlMode; float oldDuration = e.turnDurationSeconds;
        int elapsedEvents = 0; Action elapsed = () => elapsedEvents++;
        e.TurnTimerElapsed += elapsed;
        try
        {
            e.turnDurationSeconds = 75; e.manualControlMode = true;
            e.frontEnd.ShowMenu(); Tick(e); Check(!e.TurnTimerActive, "Menu clock running");
            e.frontEnd.Play(); Tick(e); Check(!e.TurnTimerActive, "Mulligan clock running");
            e.confirmMulliganButton.onClick.Invoke();
            Check(e.TurnTimerActive && e.TurnTimeRemaining > 74 && !e.TurnTimeExpired, "First turn clock absent");
            await Until(() => !e.handDisplay.drawPresentation.IsBusy, "Opening draw stuck");

            // Pending hero aim is discarded; a deadline bypasses an in-progress button flip.
            e.faceView.Player.CurrentMana = 10; Refresh(e); e.heroPowerButton.onClick.Invoke();
            Check(e.targetingRibbon.Visible, "Fixture did not arm hero targeting");
            Expire(e);
            Check(e.TimedTurnNumber == 2 && !e.targetingRibbon.Visible && !Field<bool>(e, "powerPending"), "Expiry failed to clear aim or advance manual turn");
            Tick(e); Tick(e);
            Check(e.TimedTurnNumber == 2 && elapsedEvents == 1 && e.TurnTimeRemaining > 74, "Expiry advanced twice or reused deadline");

            // A held Player Two card is cancelled without spending it; late pointer release is inert.
            Refresh(e); await Task.Delay(50);
            var card = e.opponentHandDisplay.handPanel.GetComponentsInChildren<CardView>().First();
            var hand = Field<PlayerHand>(e, "playerTwoHand"); var held = card.Card; int heldCount = hand.Hand.Count;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = card.transform.position };
            card.OnBeginDrag(pointer);
            Check(Field<CardDragResolver>(e, "cardDragResolver").DragInProgress, "Fixture drag did not begin");
            Expire(e); card.OnEndDrag(pointer);
            Check(e.TimedTurnNumber == 3 && !Field<CardDragResolver>(e, "cardDragResolver").DragInProgress && hand.Hand.Contains(held) && hand.Hand.Count == heldCount,
                "Timeout resolved a held card, advanced twice, or retained drag guard");
            await Task.Delay(50);
            Check(!e.GetComponentsInChildren<CardView>(true).Any(v => v.name.EndsWith("(DragGhost)")) &&
                !e.frontEnd.GetComponentInParent<Canvas>().rootCanvas.GetComponentsInChildren<CardView>(true).Any(v => v.name.EndsWith("(DragGhost)")), "Timeout left a drag ghost");

            // Accept a real Fireball, then expire while its source animation queue is busy.
            var one = Field<PlayerHand>(e, "playerOneHand"); var fire = e.cardPool.First(c => c.cardName == "Fireball");
            one.Hand.Clear(); one.AddCardToHand(fire); e.faceView.Player.CurrentMana = 10;
            int health = e.opponentFaceView.Player.Health;
            Check(one.PlayCard(one.Hand[0], Field<GameContext>(e, "context"), new Target(e.opponentFaceView.Player)), "Fireball fixture rejected");
            Check(e.ActionEffectsBusy, "Real spell did not hold presentation");
            Expire(e);
            Check(e.TimedTurnNumber == 3 && e.TurnTimeExpired && !e.endTurnButton.interactable, "Busy expiry advanced early or left input enabled");
            Check(!Field<CardDragResolver>(e, "cardDragResolver").CanPlayerOneDrag() && !Field<GameManager>(e, "gameManager").CanUseHeroPower(e.faceView.Player), "Expired busy turn accepts card/power input");
            Tick(e); Check(elapsedEvents == 3 && e.TimedTurnNumber == 3, "Busy expiry event repeated");
            await Until(() => e.TimedTurnNumber == 4, "Expired busy turn never advanced after effect completion");
            Check(e.opponentFaceView.Player.Health == health - 3 && !e.TurnTimeExpired && e.TurnTimeRemaining > 70, "Expiry changed spell damage or poisoned next turn clock");
            Tick(e); Check(e.TimedTurnNumber == 4 && elapsedEvents == 3, "Effect completion caused double advance");

            // Normal AI turn is synchronous: one timeout returns control to a fresh Player One turn.
            e.frontEnd.ShowMenu(); Tick(e); Check(!e.TurnTimerActive && e.TurnTimeRemaining == 0, "Menu retained timer");
            e.manualControlMode = false; e.frontEnd.Play(); e.confirmMulliganButton.onClick.Invoke();
            Expire(e); Check(e.TimedTurnNumber == 3 && !e.IsOpponentTurn && !e.TurnTimeExpired && e.TurnTimeRemaining > 74, "Synchronous AI inherited or double-consumed deadline");
            await Until(() => !e.ActionEffectsBusy, "AI effects stuck");
            int turn = e.TimedTurnNumber; e.opponentFaceView.Player.Health = 0; Field<GameManager>(e, "gameManager").AfterGameAction(); Tick(e);
            Check(!e.TurnTimerActive && e.TimedTurnNumber == turn, "Game-over clock still runs");
            e.playAgainButton.onClick.Invoke(); Tick(e); Check(!e.TurnTimerActive && e.mulliganPanel.gameObject.activeSelf, "Restart skipped fresh inactive mulligan");
            e.confirmMulliganButton.onClick.Invoke(); Check(e.TimedTurnNumber == 1 && e.TurnTimeRemaining > 74, "Restart retained previous match deadline");
            return "TurnClock clamp/reset; no menu/mulligan/game-over timer; manual both-player expiry exactly once; pending aim/held card/ghost cancellation; real Fireball deadline input lock while accepted VFX completes; synchronous AI return and fresh match reset passed. Production 75-second duration unchanged.";
        }
        finally { e.TurnTimerElapsed -= elapsed; e.manualControlMode = oldManual; e.turnDurationSeconds = oldDuration; e.frontEnd.ShowMenu(); }
    }
}
