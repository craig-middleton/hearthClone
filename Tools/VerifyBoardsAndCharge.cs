using System;
using System.Collections.Generic;
using System.Linq;
using HearthstoneClone.AI;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;

public static class VerifyBoardsAndCharge
{
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    public static string[] Main()
    {
        var results=new List<string>();
        var p1=new Player("Charge owner");var p2=new Player("Defender");var board=new Board(p1,p2);
        var ordinary=new Minion("Ordinary",3,5);var charge=new Minion("Charge",3,5,false,null,true);
        Check(!ordinary.CanAttack&&charge.CanAttack,"Charge/summoning sickness incorrect");p1.BoardMinions.Add(charge);
        var taunt=new Minion("Taunt",1,5,true);p2.BoardMinions.Add(taunt);
        Check(!Combat.TryAttack(charge,new Target(p2),board,out _),"Charge bypassed Taunt");
        Check(Combat.TryAttack(charge,new Target(taunt),board,out _)&&taunt.CurrentHealth==2&&charge.CurrentHealth==4,"Charge cannot attack Taunt immediately");
        Check(charge.HasAttackedThisTurn&&!charge.CanAttack,"Charge incorrectly grants repeated attacks");p2.BoardMinions.Clear();
        Check(!Combat.TryAttack(charge,new Target(p2),board,out _),"Charge attacked twice");
        var frozen=new Minion("Frozen Charge",3,5,false,null,true);frozen.Freeze();Check(!frozen.CanAttack,"Charge bypassed Freeze");
        results.Add("Charge: immediate attack, one attack per turn, with Taunt and Freeze still enforced");
        var wolf=AssetDatabase.LoadAssetAtPath<CardData>("Assets/_Project/ScriptableObjects/KeywordMinions/WolfRider.asset");
        p1=new Player("Hand owner");p2=new Player("Opponent");board=new Board(p1,p2);p1.CurrentMana=3;
        var hand=new PlayerHand(p1,new List<CardData>());hand.AddCardToHand(wolf);Check(hand.PlayCard(hand.Hand[0],new GameContext(board),new Target(p1)),"Charge card failed to summon");
        Check(p1.BoardMinions[0].HasCharge&&p1.BoardMinions[0].CanAttack&&p1.CurrentMana==0,"Card Charge flag did not reach runtime minion");
        results.Add("Card-to-model binding: Wolf Rider spends 3 mana and arrives ready to attack");
        p1=new Player("Human");p2=new Player("AI");board=new Board(p1,p2);p2.CurrentMana=3;
        var aiHand=new PlayerHand(p2,new List<CardData>());aiHand.AddCardToHand(wolf);new AIController(aiHand,new GameContext(board),board).TakeTurn();
        Check(p1.Health==27&&p2.BoardMinions[0].HasAttackedThisTurn,"AI did not use Charge on the summon turn");
        results.Add("AI: a newly summoned Charge minion attacks during that same turn");
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        Check(e.cardPool.Count==26&&e.cardPool.Count(c=>c.hasTaunt)>=3&&e.cardPool.Count(c=>c.hasCharge)>=3,"Keyword cards missing from pool");
        Check(e.cardPool.All(c=>c.artwork!=null),"Missing artwork in expanded card pool");
        var themes=e.boardThemes;int old=themes.CurrentIndex;var random=UnityEngine.Random.state;var seen=new HashSet<int>();int last=-1;
        for(int i=0;i<100;i++)
        {
            themes.ChooseNextBoard();Check(themes.CurrentIndex!=last,"Immediate board repeat");
            Check(themes.boards.Count(b=>b.activeSelf)==1,"More than one board visible");seen.Add(themes.CurrentIndex);last=themes.CurrentIndex;
        }
        themes.ShowBoard(old>=0?old:0);UnityEngine.Random.state=random;
        Check(seen.Count==3,"Random selection did not cover all board variants");
        results.Add("Boards: all 3 variants selected across 100 picks, one visible at a time, without immediate repeats");
        return results.ToArray();
    }
}
