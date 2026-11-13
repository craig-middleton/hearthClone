using System;
using System.Collections.Generic;
using System.Linq;
using HearthstoneClone.AI;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.Effects;
using UnityEditor;

public static class VerifyMageSpells
{
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static CardData Card(string name)=>AssetDatabase.LoadAssetAtPath<CardData>("Assets/_Project/ScriptableObjects/MageSpells/"+name+".asset");
    static Minion M(string name,int health)=>new Minion(name,2,health);
    public static string[] Main()
    {
        var results=new List<string>();
        var flame=Card("Flamestrike");var arcane=Card("ArcaneExplosion");var cone=Card("ConeOfCold");
        Check(flame!=null&&arcane!=null&&cone!=null,"Missing spell asset");
        var p1=new Player("Test player");var p2=new Player("Test opponent");var board=new Board(p1,p2);var context=new GameContext(board);
        var friendly=M("Friendly",4);var enemy=M("Enemy",5);var lethal=M("Lethal",4);
        p1.BoardMinions.Add(friendly);p2.BoardMinions.AddRange(new[]{enemy,lethal});p1.CurrentMana=10;
        var hand=new PlayerHand(p1,new List<CardData>());hand.AddCardToHand(flame);
        int events=0;IReadOnlyList<Minion> captured=null;
        hand.CardPlayed+=(card,affected)=>{events++;captured=affected;Check(lethal.IsDead,"Event fired before rules resolved");};
        Check(hand.PlayCard(hand.Hand[0],context,new Target(p1)),"Flamestrike cast rejected");
        Check(enemy.CurrentHealth==1&&lethal.IsDead&&friendly.CurrentHealth==4,"Flamestrike ownership/damage incorrect");
        Check(p1.Health==30&&p2.Health==30&&p1.CurrentMana==3&&hand.Hand.Count==0,"Flamestrike hero damage or wrong cost");
        Check(events==1&&captured.Count==2&&captured.Contains(lethal),"Cast snapshot lost a lethal target");
        results.Add("Flamestrike: 4 enemy-only damage, 7 mana, successful-cast event retains lethal targets");

        p1.BoardMinions.Clear();p2.BoardMinions.Clear();friendly=M("P1",4);enemy=M("P2",4);
        p1.BoardMinions.Add(friendly);p2.BoardMinions.Add(enemy);
        arcane.onPlayEffect.Execute(context,new Target(enemy),p2);
        Check(friendly.CurrentHealth==3&&enemy.CurrentHealth==4,"Player Two AoE hit wrong board");
        results.Add("Arcane Explosion: Player Two caster hits Player One, even with an unrelated supplied target");

        p1.BoardMinions.Clear();p2.BoardMinions.Clear();var line=new[]{M("A",3),M("B",3),M("C",1),M("D",3),M("E",3)};p2.BoardMinions.AddRange(line);
        cone.onPlayEffect.Execute(context,new Target(line[2]),p1);
        Check(line[0].CurrentHealth==3&&line[4].CurrentHealth==3&&!line[0].IsFrozen&&!line[4].IsFrozen,"Cone hit non-neighbours");
        Check(line[1].CurrentHealth==2&&line[2].IsDead&&line[3].CurrentHealth==2&&line[1].IsFrozen&&line[2].IsFrozen&&line[3].IsFrozen,"Cone adjacency/damage/freeze incorrect");
        results.Add("Cone of Cold: center plus both neighbours, including lethal center, with no spill beyond three targets");

        p2.BoardMinions.Clear();line=new[]{M("A",3),M("Dead",0),M("B",3),M("C",3)};p2.BoardMinions.AddRange(line);
        cone.onPlayEffect.Execute(context,new Target(line[0]),p2);
        Check(line[0].CurrentHealth==2&&line[2].CurrentHealth==2&&line[3].CurrentHealth==3,"Cone edge/dead-slot adjacency incorrect");
        Check(((IAreaDamageEffect)cone.onPlayEffect).GetAffectedMinions(context,new Target(p1),p1).Count==0,"Cone accepted a hero target");
        results.Add("Cone edge/friendly targeting: living adjacency skips corpses; heroes are excluded");

        p1.BoardMinions.Clear();p2.BoardMinions.Clear();friendly=M("AI ally",5);enemy=M("AI enemy",1);p2.BoardMinions.Add(friendly);p1.BoardMinions.Add(enemy);p2.CurrentMana=4;
        var aiHand=new PlayerHand(p2,new List<CardData>());aiHand.AddCardToHand(cone);new AIController(aiHand,context,board).TakeTurn();
        Check(enemy.IsDead&&!friendly.IsFrozen&&friendly.CurrentHealth==5&&aiHand.Hand.Count==0&&p2.CurrentMana==0,"AI aimed offensive AnyMinion at its own board");
        results.Add("AI: Cone of Cold targets an enemy rather than using the friendly-buff branch");

        p1.BoardMinions.Clear();p2.BoardMinions.Clear();p2.CurrentMana=7;aiHand=new PlayerHand(p2,new List<CardData>());aiHand.AddCardToHand(flame);
        new AIController(aiHand,context,board).TakeTurn();Check(aiHand.Hand.Count==1&&p2.CurrentMana==7,"AI wasted AoE on an empty board");
        p1.CurrentMana=1;hand=new PlayerHand(p1,new List<CardData>());hand.AddCardToHand(flame);events=0;hand.CardPlayed+=(c,a)=>events++;
        Check(!hand.PlayCard(hand.Hand[0],context,new Target(p1))&&hand.Hand.Count==1&&p1.CurrentMana==1&&events==0,"Failed cast changed state or fired VFX event");
        results.Add("Invalid/unhelpful casts: insufficient mana spends nothing; AI retains empty-board AoE");
        return results.ToArray();
    }
}
