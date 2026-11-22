using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEngine;

// Disposable Play-mode composition using the current card pool and normal view refresh.
public static class PreviewNativeCards
{
    public static async Task<string> Main()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        game.frontEnd.ShowMenu();
        game.frontEnd.Play();
        game.confirmMulliganButton.onClick.Invoke();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (game.ActionEffectsBusy || game.handDisplay.drawPresentation.IsBusy)
        {
            if (timer.ElapsedMilliseconds > 10000) throw new Exception("Opening presentation did not finish");
            await Task.Delay(20);
        }

        var player = game.faceView.Player;
        var opponent = game.opponentFaceView.Player;
        player.BoardMinions.Clear();
        opponent.BoardMinions.Clear();
        var cards = game.cardPool.Where(card => card.cardType == CardType.Minion && card.artwork != null).ToArray();
        var normal = cards.First(card => !card.hasTaunt && card.attack > 0);
        var shield = cards.First(card => card.hasTaunt);
        foreach (var card in new[] { normal, shield })
        {
            var minion = new Minion(card.cardName, card.attack, card.health, card.hasTaunt, card.artwork);
            minion.ResetForNewTurn();
            player.BoardMinions.Add(minion);
        }
        var enemyCard = cards.Last(card => card.attack > 0);
        opponent.BoardMinions.Add(new Minion(enemyCard.cardName, enemyCard.attack, enemyCard.health,
            enemyCard.hasTaunt, enemyCard.artwork));

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var hand = (PlayerHand)typeof(EffectTester).GetField("playerOneHand", flags).GetValue(game);
        hand.Hand.Clear();
        hand.AddCardToHand(normal);
        hand.AddCardToHand(shield);
        foreach (var name in new[] { "Fireball", "Frostbolt" })
            hand.AddCardToHand(game.cardPool.First(card => card.cardName == name));
        player.CurrentMana = 5;
        typeof(EffectTester).GetMethod("RefreshAll", flags).Invoke(game, null);
        await Task.Delay(180);
        return "Source card/minion preview with actual card-pool artwork, ready minions, taunt and affordable hand cards. Discard this Play fixture afterward.";
    }
}
