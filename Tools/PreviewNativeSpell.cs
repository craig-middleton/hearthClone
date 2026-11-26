using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEngine;

// Disposable actual-cast fixture. Stop Play to discard; Resume also clears the pause.
public static class PreviewNativeSpell
{
    public static Task<string> Fireball() => Cast("Fireball", 520);
    public static Task<string> Frostbolt() => Cast("Frostbolt", 220);
    public static string Resume() { Time.timeScale = 1; return "Preview resumed."; }
    static async Task<string> Cast(string name, int pauseAfterMilliseconds)
    {
        Time.timeScale = 1;
        var game = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        game.frontEnd.ShowMenu(); game.frontEnd.Play(); game.confirmMulliganButton.onClick.Invoke();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (game.ActionEffectsBusy || game.handDisplay.drawPresentation.IsBusy)
        {
            if (timer.ElapsedMilliseconds > 10000) throw new Exception("Opening draw did not finish");
            await Task.Delay(20);
        }
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var hand = (PlayerHand)typeof(EffectTester).GetField("playerOneHand", flags).GetValue(game);
        var card = game.cardPool.First(c => c.cardName == name);
        hand.Hand.Clear(); hand.AddCardToHand(card); game.faceView.Player.CurrentMana = 10;
        if (!hand.PlayCard(hand.Hand[0], new GameContext(new Board(game.faceView.Player,
            game.opponentFaceView.Player)), new Target(game.opponentFaceView.Player)))
            throw new Exception("Actual source spell cast rejected");
        typeof(EffectTester).GetMethod("RefreshAll", flags).Invoke(game, null);
        if(name=="Fireball")
        {
            var wait=System.Diagnostics.Stopwatch.StartNew();
            while(game.battleEffects.originalSpells.ActiveCount<2)
            {if(wait.ElapsedMilliseconds>15000)throw new Exception("Fireball impact did not start");await Task.Delay(10);}
            float impact=Time.time;
            while(Time.time-impact<.12f)
            {if(wait.ElapsedMilliseconds>15000)throw new Exception("Impact frames stopped");await Task.Delay(10);}
        }
        else await Task.Delay(pauseAfterMilliseconds);
        Time.timeScale = 0;
        return "Paused actual " + name + " for capture. Run Resume or stop Play afterward.";
    }
}
