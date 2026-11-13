using System;
using System.Linq;
using System.Reflection;
using HearthstoneClone.Cards;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;

public static class VerifyStartingDecks
{
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static PlayerHand Hand(EffectTester e,string name)=>(PlayerHand)typeof(EffectTester).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(e);
    static string Counts(EffectTester e)
    {
        var one=Hand(e,"playerOneHand");var two=Hand(e,"playerTwoHand");var a=one.Deck.Concat(one.Hand).ToArray();var b=two.Deck.Concat(two.Hand).ToArray();
        Check(a.Length==30&&b.Length==30,"Deck plus hand must be exactly 30 for both players, including Coin");
        Check(a.Concat(b).Distinct().Count()==60,"A CardInstance was shared between slots or players");
        Check(a.GroupBy(c=>c.Data).All(g=>g.Count()<=e.copiesPerCard)&&b.Where(c=>c.Data!=e.coinCard).GroupBy(c=>c.Data).All(g=>g.Count()<=e.copiesPerCard),"Deck exceeds the per-card copy limit");
        return $"P1={one.Deck.Count} deck + {one.Hand.Count} hand; P2={two.Deck.Count} deck + {two.Hand.Count} hand";
    }
    public static string[] Main()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Run this in a fresh Play-mode match.");
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        if(e.frontEnd!=null&&e.frontEnd.gameObject.activeSelf)e.frontEnd.Play();
        if(e.mulliganPanel.gameObject.activeSelf)e.confirmMulliganButton.onClick.Invoke();
        string turnOne=Counts(e);Check(e.faceView.Player.MaxMana==1,"Fixture must start on turn one");
        int previous=e.boardThemes.CurrentIndex;
        e.playAgainButton.onClick.Invoke();Check(e.mulliganPanel.gameObject.activeSelf,"Restart skipped mulligan");
        string opening=Counts(e);Check(e.boardThemes.CurrentIndex!=previous,"Restart did not choose a different board");
        e.confirmMulliganButton.onClick.Invoke();string restarted=Counts(e);
        return new[]{"Turn one: "+turnOne,"Restart opening: "+opening,"Restart turn one: "+restarted,"All 60 card instances distinct; copy limits preserved; restart changes board"};
    }
}
