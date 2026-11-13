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

public static class VerifyPowersAndBattle
{
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static void Click(Button button)=>ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    static async Task Frame(){int f=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(f==Time.frameCount){if(timer.Elapsed.TotalSeconds>10)throw new Exception("Game not ticking");await Task.Delay(25);}}
    static async Task Idle(BattleEffects effects)
    {var timer=System.Diagnostics.Stopwatch.StartNew();while(effects.IsBusy){if(timer.Elapsed.TotalSeconds>25)throw new Exception("Combat effects did not complete");await Task.Delay(40);}await Frame();Check(effects.overlay.GetComponentsInChildren<Graphic>().All(g=>g.name!="NativeSpellProjectile"&&g.name!="NativeImpact"&&g.name!="ImpactDamage"),"Temporary graphics leaked");}
    public static async Task<string[]> Main()
    {
        var a=new Player("Mage"){CurrentMana=2};var b=new Player("Warrior"){CurrentMana=2};var model=new Board(a,b);
        var missing=new Minion("Off board",1,1);
        Check(!HeroPower.TryUse(a,HeroPowerKind.Fireblast,new Target(missing),model)&&a.CurrentMana==2,"Off-board target spent mana");
        Check(HeroPower.TryUse(b,HeroPowerKind.ArmorUp,new Target(b),model)&&b.Armor==2&&b.CurrentMana==0,"Armor Up failed");
        Check(HeroPower.TryUse(a,HeroPowerKind.Fireblast,new Target(b),model)&&b.Armor==1&&b.Health==30,"Armor did not absorb Fireblast");
        Check(!HeroPower.TryUse(a,HeroPowerKind.Fireblast,new Target(b),model),"Second use accepted");
        b.TakeDamage(3);Check(b.Armor==0&&b.Health==28,"Armor overflow damage wrong");
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required for live UI checks");
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        e.CancelPendingInput();e.ResetPresentation();
        if(e.frontEnd.gameObject.activeSelf)e.frontEnd.Play();
        if(e.mulliganPanel.gameObject.activeSelf)e.confirmMulliganButton.onClick.Invoke();
        e.faceView.Player.CurrentMana=10;e.faceView.Player.MaxMana=10;e.faceView.Player.HasUsedHeroPowerThisTurn=false;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(EffectTester).GetMethod("RefreshAll",flags).Invoke(e,null);await Frame();
        Check(e.opponentHeroPowerButton.gameObject.activeInHierarchy&&!e.opponentHeroPowerButton.interactable,"Opponent power missing or clickable out of turn");
        Click(e.heroPowerButton);Check(e.targetingRibbon.Visible&&e.faceView.Player.CurrentMana==10,"Arming power spent mana or missed arrow");
        Check(e.targetingRibbon.GetComponent<CanvasRenderer>()!=null,"Target arrow has no renderer");
        e.CancelPendingInput();Check(!e.targetingRibbon.Visible&&e.faceView.Player.CurrentMana==10,"Cancel spent mana");
        int hp=e.opponentFaceView.Player.Health;Click(e.heroPowerButton);Click(e.opponentFaceView.button);
        Check(e.faceView.Player.CurrentMana==8&&e.opponentFaceView.Player.Health==hp-1&&!e.targetingRibbon.Visible,"Aimed face hit failed");
        Check(e.battleEffects.IsBusy,"Power has no native projectile presentation");await Idle(e.battleEffects);
        // A fresh target with one health exercises dead-target snapshots and actual minion click routing.
        var target=new Minion("Power target",1,1);e.opponentFaceView.Player.BoardMinions.Add(target);
        e.faceView.Player.HasUsedHeroPowerThisTurn=false;e.faceView.Player.CurrentMana=10;
        typeof(EffectTester).GetMethod("RefreshAll",flags).Invoke(e,null);await Frame();
        Click(e.heroPowerButton);Click(e.opponentBoardDisplay.GetViewTransform(target).GetComponent<MinionView>().button);
        Check(target.IsDead&&!e.opponentFaceView.Player.BoardMinions.Contains(target),"Aimed minion kill failed");await Idle(e.battleEffects);
        // Real attack button route, with lethal retaliation, must preserve snapshots then clean up.
        var attacker=new Minion("Attacker",3,1,false,e.cardPool.First(c=>c.artwork!=null).artwork,true);
        var defender=new Minion("Defender",2,2,false,attacker.Artwork);
        e.faceView.Player.BoardMinions.Clear();e.opponentFaceView.Player.BoardMinions.Clear();e.faceView.Player.BoardMinions.Add(attacker);e.opponentFaceView.Player.BoardMinions.Add(defender);
        typeof(EffectTester).GetMethod("RefreshAll",flags).Invoke(e,null);await Frame();
        Click(e.boardDisplay.GetViewTransform(attacker).GetComponent<MinionView>().button);await Frame();
        Click(e.opponentBoardDisplay.GetViewTransform(defender).GetComponent<MinionView>().button);
        Check(attacker.IsDead&&defender.IsDead&&e.battleEffects.IsBusy,"Attack rule/presentation path failed");await Idle(e.battleEffects);
        Check(e.battleEffects.overlay.Find("CombatSnapshot")==null,"Attack ghost leaked");
        // With an empty AI hand/deck it has two spare mana on turn entry, and should armor up.
        var aiHand=(PlayerHand)typeof(EffectTester).GetField("playerTwoHand",flags).GetValue(e);aiHand.Hand.Clear();aiHand.Deck.Clear();e.opponentFaceView.Player.MaxMana=1;e.opponentFaceView.Player.Armor=0;
        Click(e.endTurnButton);await Idle(e.battleEffects);
        Check(e.opponentFaceView.Player.Armor==2&&e.opponentFaceView.Player.HasUsedHeroPowerThisTurn,"AI did not use its visible Armor Up power");
        e.frontEnd.ShowMenu();Check(!e.battleEffects.IsBusy&&!e.targetingRibbon.Visible,"Menu did not cancel presentation/input");
        return new[]{"Power rules: target validation, 2-mana/once-per-turn checks, armor absorption/overflow pass", "Aiming and cancel preserve mana until target; face/minion clicks and lethal Fireblast work", "Native power projectile and lethal attack/retaliation snapshots clean up", "Opponent power visible; AI uses Armor Up with spare mana; Menu cancels effects and aiming"};
    }
}
