using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEngine;

public static class VerifyGrizzlyTauntPrototype
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Refresh(EffectTester game) => typeof(EffectTester).GetMethod("RefreshAll", Private).Invoke(game, null);
    static async Task Until(Func<bool> condition, int limit, string message)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (!condition()) { if (timer.ElapsedMilliseconds > limit) throw new Exception(message); await Task.Delay(10); }
    }
    public static async Task<string> Main()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        float previousTimeScale = Time.timeScale;
        try
        {
            game.frontEnd.ShowMenu(); game.frontEnd.Play(); game.confirmMulliganButton.onClick.Invoke();
            await Until(() => !game.ActionEffectsBusy && !game.handDisplay.drawPresentation.IsBusy, 10000, "Opening draw did not finish");
            // Slow only this disposable fixture so low-rate background Editor frames
            // cannot skip the source spring's brief overshoot. Game timing stays unchanged.
            Time.timeScale = .25f;
            var data = game.cardPool.Single(c => c.cardName == "Ironfur Grizzly");
            var owner = game.faceView.Player;
            owner.BoardMinions.Clear();
            var grizzly = new Minion(data.cardName, data.attack, data.health, true, data.artwork);
            var sameArtControl = new Minion("Bear", 3, 6, true, data.artwork);
            owner.BoardMinions.Add(grizzly); owner.BoardMinions.Add(sameArtControl); Refresh(game);
            await Task.Delay(100);
            var native = game.boardDisplay.GetViewTransform(grizzly).GetComponent<NativeMinionPresentation>();
            var style = native.GetComponent<GrizzlyTauntPrototype>();
            Check(style != null && style.IsPrototypeActive, "Grizzly-specific original taunt presenter not active");
            Check(style.library.spawnChannels.Length == 7 && Mathf.Abs(style.library.spawnDuration - .48333338f) < 1e-5f,
                "Original summon channels/duration absent");
            Check(native.actor.localPosition.y > .1f, "Original summon did not begin above source landing position");
            var control = game.boardDisplay.GetViewTransform(sameArtControl).GetComponent<GrizzlyTauntPrototype>();
            Check(control != null && !control.IsPrototypeActive, "Prototype leaked to Bear with shared artwork");
            var shield = native.taunt.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Taunt_Shield_mesh");
            var view = native.GetComponent<MinionView>();
            view.SetMinion(sameArtControl); await Task.Delay(80);
            Check(!style.IsPrototypeActive && native.actor.localPosition.sqrMagnitude < 1e-8f &&
                shield.GetComponent<Renderer>().enabled,
                "Mid-birth rebind left the non-prototype card in its old pose");
            view.SetMinion(grizzly); await Task.Delay(80);
            Check(style.IsPrototypeActive && !shield.GetComponent<Renderer>().enabled, "Original .6s taunt wait was skipped");
            var director = native.stage.GetComponentInParent<GrizzlyTauntPrototypeDirector>();
            Check(director != null && director.PlayedTauntSounds == 0, "Sound started before source state6");
            bool overshot = false;
            await Until(() =>
            {
                overshot |= shield.localScale.x > style.library.settledScale + .015f;
                return style.SourceAge >= style.library.idleDelay + style.library.scaleDuration + .05f;
            }, 30000, "Original taunt spring did not finish");
            Check(native.actor.localPosition.sqrMagnitude < 1e-8f, "Original summon failed to land at source origin");
            Check(overshot && Mathf.Abs(shield.localScale.x - style.library.settledScale) < 1e-6f,
                "Original spring overshoot or final target snap absent");
            Check(shield.GetComponent<Renderer>().enabled && director.PlayedTauntSounds == 1,
                "Source shield visibility/exact sound did not enter once");
            Check(style.library.tauntUpSound != null && style.library.tauntUpSound.name == "taunt_shield_up",
                "Wrong original taunt sound binding");
            float settled = style.library.settledScale;
            Refresh(game); await Task.Delay(100);
            var rebuilt = game.boardDisplay.GetViewTransform(grizzly).GetComponent<NativeMinionPresentation>();
            var rebuiltShield = rebuilt.taunt.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Taunt_Shield_mesh");
            Check(director.PlayedTauntSounds == 1 && Mathf.Abs(rebuiltShield.localScale.x - settled) < 1e-6f,
                "View refresh replayed original birth or sound");
            game.frontEnd.ShowMenu();
            Check(director.ActiveBirths == 0 && director.PlayedTauntSounds == 0, "Menu retained Grizzly instance state");
            return "Grizzly-only original .4833s summon channels, source .6s delay/.4s Spring overshoot and exact final scale, source shield-up sound, shared-art isolation, refresh continuity and menu cleanup passed. Source root fade/gleam/particles/death/VO remain unported.";
        }
        finally { Time.timeScale = previousTimeScale; game.frontEnd.ShowMenu(); }
    }
}
