using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyNativeCombatSnapshots
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static async Task Until(Func<bool> condition,int timeout,string message)
    {
        var clock=System.Diagnostics.Stopwatch.StartNew();
        while(!condition()){if(clock.ElapsedMilliseconds>timeout)throw new Exception(message);await Task.Delay(20);}
    }
    static void Refresh(EffectTester match)=>typeof(EffectTester).GetMethod("RefreshAll",Flags).Invoke(match,null);
    static NativeMinionPresentation[] Snapshots(EffectTester match)=>match.battleEffects.overlay
        .GetComponentsInChildren<NativeMinionPresentation>(true).Where(n=>n.name=="CombatSnapshot").ToArray();
    static MeshRenderer Body(NativeMinionPresentation native)=>native.actor.Find("RootObject/Card_InPlay_Ally/Mesh").GetComponent<MeshRenderer>();
    static void VisibleSnapshot(NativeMinionPresentation copy,NativeMinionPresentation source)
    {
        Check(copy.stage!=null&&copy.stage!=source.stage&&copy.actor!=source.actor,"Combat snapshot shares its source world stage");
        var body=Body(copy);var original=Body(source);
        Check(body.gameObject.activeInHierarchy&&body.enabled&&body.GetComponent<MeshFilter>().sharedMesh.vertexCount==532,"Combat snapshot lacks the visible original oval model");
        Check(body.sharedMaterials.All(m=>m!=null&&m.shader.isSupported&&m.mainTexture!=null),"Combat snapshot lost source paint");
        Check(body.sharedMaterials[0]!=original.sharedMaterials[0]&&body.sharedMaterials[0].mainTexture==original.sharedMaterials[0].mainTexture,"Combat portrait lifetime still belongs to the disposable source view");
        Check((copy.boardCamera.cullingMask&(1<<body.gameObject.layer))!=0&&GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(copy.boardCamera),body.bounds),"Combat model is outside its visible board camera");
        Check(!copy.highlight.enabled&&copy.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget)&&copy.GetComponentsInChildren<Button>(true).All(b=>!b.enabled),"Combat snapshot retains readiness or input");
        Check(copy.stage.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"Combat world snapshot intercepts board clicks");
    }
    public static async Task<string> Main()
    {
        var match=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        float previousScale=Time.timeScale;
        try
        {
            match.frontEnd.ShowMenu();match.frontEnd.Play();match.confirmMulliganButton.onClick.Invoke();
            await Until(()=>!match.ActionEffectsBusy&&!match.handDisplay.drawPresentation.IsBusy,15000,"Opening presentation did not finish");
            // Slow only this disposable fixture so brief travel phases remain observable
            // while the background Editor yields. Production durations are unchanged.
            Time.timeScale=.1f;
            var owner=match.faceView.Player;var enemy=match.opponentFaceView.Player;
            owner.BoardMinions.Clear();enemy.BoardMinions.Clear();
            var art=match.cardPool.First(c=>c.cardType==CardType.Minion&&c.artwork!=null).artwork;
            var attacker=new Minion("Snapshot attacker",3,20,false,art,true);
            var defender=new Minion("Snapshot defender",1,20,false,art,true);
            owner.BoardMinions.Add(attacker);enemy.BoardMinions.Add(defender);Refresh(match);
            await Until(()=>match.boardDisplay.GetViewTransform(attacker).GetComponent<NativeMinionPresentation>().stage!=null,3000,"Original source model was not initialized");
            var source=match.boardDisplay.GetViewTransform(attacker).GetComponent<NativeMinionPresentation>();
            var sourceStage=source.stage;var sourcePosition=sourceStage.position;
            var ghost=(GameObject)typeof(BattleEffects).GetMethod("Ghost",Flags).Invoke(match.battleEffects,new object[]{source.transform});
            var copy=ghost.GetComponent<NativeMinionPresentation>();VisibleSnapshot(copy,source);
            ghost.SetActive(false);Check(sourceStage.gameObject.activeInHierarchy,"Disabling combat snapshot hid its source actor");
            ghost.SetActive(true);ghost.transform.position+=new Vector3(100,35,0);
            await Until(()=>Vector2.Distance(copy.boardCamera.WorldToScreenPoint(Body(copy).bounds.center),ghost.transform.position)<8,3000,"Native snapshot model did not follow its moved UI root");
            Check(Vector3.Distance(sourceStage.position,sourcePosition)<.001f,"Moving snapshot moved the source actor");
            var copiedStage=copy.stage;
            typeof(BattleEffects).GetMethod("Remove",Flags).Invoke(match.battleEffects,new object[]{ghost});
            await Until(()=>copiedStage==null,3000,"Combat snapshot leaked its owned model");
            Check(sourceStage!=null&&sourceStage.gameObject.activeInHierarchy,"Removing combat snapshot destroyed its live source actor");

            // Use real combat input, then refresh while its accepted animation is running.
            source.GetComponent<MinionView>().button.onClick.Invoke();
            match.opponentBoardDisplay.GetViewTransform(defender).GetComponent<MinionView>().button.onClick.Invoke();
            await Until(()=>Snapshots(match).Any(n=>n.gameObject.activeInHierarchy&&n.stage!=null&&n.stage.gameObject.activeInHierarchy),3000,"Actual attack did not show its native combat snapshot");
            copy=Snapshots(match).Single(n=>n.gameObject.activeInHierarchy);var attackRoot=copy.transform;var from=attackRoot.position;
            await Until(()=>Vector3.Distance(attackRoot.position,from)>4,3000,"Actual native attack snapshot never moved");
            Check(Vector2.Distance(copy.boardCamera.WorldToScreenPoint(Body(copy).bounds.center),attackRoot.position)<8,"Attack model detached from its moving snapshot");
            Refresh(match);
            await Until(()=>
            {
                var live=match.boardDisplay.GetViewTransform(attacker).GetComponent<NativeMinionPresentation>();
                return live.stage!=null&&!live.stage.gameObject.activeInHierarchy&&live.GetComponent<CanvasGroup>()!=null&&live.GetComponent<CanvasGroup>().alpha==0;
            },3000,"Board refresh restored a duplicate stationary attacker during combat");
            await Until(()=>!match.ActionEffectsBusy,12000,"Actual attack did not finish");
            await Until(()=>match.boardDisplay.GetViewTransform(attacker).GetComponent<NativeMinionPresentation>().stage!=null,3000,"Post-attack source actor was not restored");
            source=match.boardDisplay.GetViewTransform(attacker).GetComponent<NativeMinionPresentation>();
            Check(source.stage.gameObject.activeInHierarchy&&(source.GetComponent<CanvasGroup>()==null||source.GetComponent<CanvasGroup>().alpha==1),"Attack left the surviving source model hidden");
            Check(Snapshots(match).Length==0,"Completed attack retained its snapshot");

            // A real lethal original spell must retain the source model through travel,
            // even after the regular board view and its portrait material are disposed.
            enemy.BoardMinions.Clear();var doomed=new Minion("Snapshot lethal target",1,1,false,art);
            enemy.BoardMinions.Add(doomed);Refresh(match);
            await Until(()=>match.opponentBoardDisplay.GetViewTransform(doomed).GetComponent<NativeMinionPresentation>().stage!=null,3000,"Lethal fixture model was not initialized");
            var victim=match.opponentBoardDisplay.GetViewTransform(doomed).GetComponent<NativeMinionPresentation>();var victimStage=victim.stage;
            var hand=(PlayerHand)typeof(EffectTester).GetField("playerOneHand",Flags).GetValue(match);
            hand.Hand.Clear();hand.AddCardToHand(match.cardPool.First(c=>c.cardName=="Frostbolt"));owner.CurrentMana=10;
            Check(hand.PlayCard(hand.Hand[0],new GameContext(new Board(owner,enemy)),new Target(doomed)),"Actual lethal Frostbolt was rejected");
            copy=Snapshots(match).Single();VisibleSnapshot(copy,victim);var lethalStage=copy.stage;
            Refresh(match);
            await Until(()=>victimStage==null,3000,"Refresh did not dispose the original lethal victim");
            Check(doomed.IsDead&&lethalStage!=null&&lethalStage.gameObject.activeInHierarchy&&Body(copy).sharedMaterials[0]!=null,"Lethal snapshot vanished with the original model/material before impact");
            await Until(()=>lethalStage==null,7000,"Lethal native snapshot survived beyond spell impact");
            await Until(()=>!match.ActionEffectsBusy,10000,"Original lethal spell did not release its queue");

            source=match.boardDisplay.GetViewTransform(attacker).GetComponent<NativeMinionPresentation>();
            ghost=(GameObject)typeof(BattleEffects).GetMethod("Ghost",Flags).Invoke(match.battleEffects,new object[]{source.transform});
            copiedStage=ghost.GetComponent<NativeMinionPresentation>().stage;
            match.battleEffects.ResetEffects();
            await Until(()=>copiedStage==null,3000,"Reset leaked the native snapshot stage");
            Check(source.stage!=null&&source.stage.gameObject.activeInHierarchy&&Snapshots(match).Length==0,"Reset removed the live source actor or retained ghost UI");
            return "Independent native snapshot model/portrait ownership, moved-model projection, suppressed input/readiness, live actor survival, actual attack refresh/hide/restore, lethal original-spell persistence to impact and reset cleanup passed.";
        }
        finally
        {
            Time.timeScale=previousScale;
            if(match!=null&&match.frontEnd!=null)match.frontEnd.ShowMenu();
        }
    }
}
