using System;
using System.Linq;
using System.Reflection;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

// Disposable mid-game visual fixture. Never save a scene while this is running.
public static class PreviewTableInterface
{
    public static string Main(int theme = 0)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("This preview is Play mode only.");
        var e = UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        if (e.mulliganPanel.gameObject.activeSelf) e.confirmMulliganButton.onClick.Invoke();
        e.boardThemes.ShowBoard(theme);
        var player = e.faceView.Player; var opponent = e.opponentFaceView.Player;
        player.BoardMinions.Clear(); opponent.BoardMinions.Clear();
        Add(e,player,"Wolf Rider"); Add(e,player,"Ironfur Grizzly"); Add(e,player,"Bear"); Add(e,player,"Charging Rhino");
        Add(e,opponent,"Shielded Guardian"); Add(e,opponent,"Goblin"); Add(e,opponent,"Boulderfist"); Add(e,opponent,"Murloc");
        player.BoardMinions[0].CurrentHealth = Math.Max(1,player.BoardMinions[0].MaxHealth-1);
        player.BoardMinions[1].HasAttackedThisTurn = true;
        opponent.BoardMinions[2].Freeze();
        player.MaxMana = 10; player.CurrentMana = 7; player.Health = 24; opponent.Health = 18;
        player.HasUsedHeroPowerThisTurn = false;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var hand = (PlayerHand)typeof(EffectTester).GetField("playerOneHand",flags).GetValue(e);
        hand.Hand.Clear();
        foreach (var name in new[]{"Shieldbearer","Fireball","Arcane Explosion","Wolf Rider","Cone of Cold","Flamestrike"})
            hand.AddCardToHand(e.cardPool.First(c=>c.cardName==name));
        typeof(EffectTester).GetMethod("RefreshAll",flags).Invoke(e,null);
        // Clear the old hover so a capture shows the resting fan consistently.
        var fan = e.handDisplay.handPanel.GetComponent<HandFanLayout>();
        typeof(HandFanLayout).GetField("hoveredView",flags).SetValue(fan,null);
        return "Disposable preview: 4 minions per side, 6 existing hand cards, 7/10 mana, existing power available; theme="+theme;
    }
    static void Add(EffectTester e,Player player,string name)
    {
        var card=e.cardPool.First(c=>c.cardName==name);
        var minion=new Minion(card.cardName,card.attack,card.health,card.hasTaunt,card.artwork,card.hasCharge);
        minion.HasSummoningSickness=false;player.BoardMinions.Add(minion);
    }
}
