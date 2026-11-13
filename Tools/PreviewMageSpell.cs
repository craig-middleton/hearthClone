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

// Disposable Play-mode fixture for viewing VFX; never saves a scene or card asset.
public static class PreviewMageSpell
{
    public static async Task<string> Main(string spellName="Flamestrike",bool opponentCast=false)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play mode first; this fixture must not edit the saved scene.");
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        e.CancelPendingInput();e.ResetPresentation();
        if(e.frontEnd!=null&&e.frontEnd.gameObject.activeSelf)e.frontEnd.Play();
        if(e.mulliganPanel.gameObject.activeSelf)e.confirmMulliganButton.onClick.Invoke();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var p1=e.faceView.Player;var p2=e.opponentFaceView.Player;
        p1.BoardMinions.Clear();p2.BoardMinions.Clear();
        var targetPlayer=opponentCast?p1:p2;
        var caster=opponentCast?p2:p1;
        var art=e.cardPool.First(c=>c.cardType==CardType.Minion&&c.artwork!=null).artwork;
        for(int i=0;i<4;i++)targetPlayer.BoardMinions.Add(new Minion("Preview target "+i,2,2+i*2,false,art));
        caster.BoardMinions.Add(new Minion("Friendly control",3,6,false,art));
        var hand=(PlayerHand)typeof(EffectTester).GetField(opponentCast?"playerTwoHand":"playerOneHand",flags).GetValue(e);
        hand.Hand.Clear();var card=e.cardPool.First(c=>c.cardName==spellName);hand.AddCardToHand(card);caster.CurrentMana=10;caster.MaxMana=10;
        typeof(EffectTester).GetMethod("RefreshAll",flags).Invoke(e,null);
        var display=opponentCast?e.boardDisplay:e.opponentBoardDisplay;
        Canvas.ForceUpdateCanvases();UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)display.boardPanel);
        // Fresh UI graphics are not raycastable until the canvas has rendered a game frame.
        int frame=Time.frameCount;var wait=System.Diagnostics.Stopwatch.StartNew();
        while(Time.frameCount==frame){if(wait.Elapsed.TotalSeconds>10)throw new InvalidOperationException("Game frames are not advancing");await Task.Delay(30);}
        if(opponentCast)
        {
            Target target=card.targetRequirement==TargetRequirement.AnyMinion?new Target(targetPlayer.BoardMinions[1]):new Target(caster);
            if(!hand.PlayCard(hand.Hand[0],new GameContext(new Board(p1,p2)),target))throw new InvalidOperationException("Fixture cast failed");
            new Board(p1,p2).RemoveDeadMinions();typeof(EffectTester).GetMethod("RefreshAll",flags).Invoke(e,null);
        }
        else
        {
            var view=e.handDisplay.handPanel.GetComponentsInChildren<CardView>().Last(v=>v.Card!=null && v.Card.Data==card);
            var pointer=new PointerEventData(EventSystem.current){position=view.transform.position,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(view.gameObject,pointer,ExecuteEvents.beginDragHandler);
            pointer.position=card.targetRequirement==TargetRequirement.AnyMinion?display.GetViewTransform(targetPlayer.BoardMinions[1]).position:card.targetRequirement==TargetRequirement.Any?e.opponentFaceView.transform.position:display.boardPanel.position;
            ExecuteEvents.Execute(view.gameObject,pointer,ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(view.gameObject,pointer,ExecuteEvents.endDragHandler);
        }
        if(caster.CurrentMana!=10-card.manaCost)throw new InvalidOperationException("Preview did not cast the card through its intended input path");
        return spellName+": remaining mana="+caster.CurrentMana+", living targets="+targetPlayer.BoardMinions.Count+", friendly health="+caster.BoardMinions[0].CurrentHealth;
    }
}
