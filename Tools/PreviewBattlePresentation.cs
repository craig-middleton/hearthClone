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
using UnityEngine.UI;

// Pauses a disposable Play-mode fixture at an actual arrow, projectile or impact frame.
public static class PreviewBattlePresentation
{
    static async Task Frame(){int f=Time.frameCount;while(f==Time.frameCount)await Task.Delay(25);}
    static void Click(Button button)=>ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    public static async Task<string> Main(string mode="Arrow")
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode only; discard this fixture afterward.");
        EditorApplication.isPaused=false;
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        if(e.frontEnd.gameObject.activeSelf)e.frontEnd.Play();
        e.CancelPendingInput();e.ResetPresentation();
        if(e.mulliganPanel.gameObject.activeSelf)e.confirmMulliganButton.onClick.Invoke();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var player=e.faceView.Player;var opponent=e.opponentFaceView.Player;
        player.CurrentMana=10;player.MaxMana=10;player.HasUsedHeroPowerThisTurn=false;
        var art=e.cardPool.First(c=>c.cardType==CardType.Minion&&c.artwork!=null).artwork;
        player.BoardMinions.Clear();opponent.BoardMinions.Clear();
        var attacker=new Minion("Preview attacker",4,6,false,art,true);var defender=new Minion("Preview defender",2,3,false,art);
        player.BoardMinions.Add(attacker);opponent.BoardMinions.Add(defender);
        var hand=(PlayerHand)typeof(EffectTester).GetField("playerOneHand",flags).GetValue(e);hand.Hand.Clear();
        var fireball=e.cardPool.First(c=>c.cardName=="Fireball");hand.AddCardToHand(fireball);
        typeof(EffectTester).GetMethod("RefreshAll",flags).Invoke(e,null);await Frame();
        string imageName;
        if(mode=="Arrow")
        {
            Click(e.heroPowerButton);e.targetingRibbon.PreviewPointer=e.opponentFaceView.transform.position;
            e.targetingRibbon.SendMessage("Update");Canvas.ForceUpdateCanvases();EditorApplication.isPaused=true;return "Paused at aimed Fireblast; no mana spent";
        }
        if(mode=="Attack")
        {
            Click(e.boardDisplay.GetViewTransform(attacker).GetComponent<MinionView>().button);await Frame();
            Click(e.opponentBoardDisplay.GetViewTransform(defender).GetComponent<MinionView>().button);imageName="NativeImpact";
        }
        else
        {
            var view=e.handDisplay.handPanel.GetComponentsInChildren<CardView>().First(v=>v.Card.Data==fireball);
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=view.transform.position};
            ExecuteEvents.Execute(view.gameObject,pointer,ExecuteEvents.beginDragHandler);pointer.position=e.opponentFaceView.transform.position;
            ExecuteEvents.Execute(view.gameObject,pointer,ExecuteEvents.dragHandler);ExecuteEvents.Execute(view.gameObject,pointer,ExecuteEvents.endDragHandler);
            if(player.CurrentMana!=6)throw new Exception("Fireball did not cast through drag input");imageName="NativeSpellProjectile";
        }
        var timer=System.Diagnostics.Stopwatch.StartNew();
        while(e.battleEffects.overlay.Find(imageName)==null){if(timer.Elapsed.TotalSeconds>20)throw new Exception("Expected native graphic not displayed");await Task.Delay(20);}
        EditorApplication.isPaused=true;return "Paused at "+imageName+" from a real "+mode+" action";
    }
}
