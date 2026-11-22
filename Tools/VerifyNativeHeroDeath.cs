using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyNativeHeroDeath
{
 static bool Visible(FaceView face){var group=face.GetComponent<CanvasGroup>();return group==null||group.alpha!=0;}
 static void Check(bool b,string message){if(!b)throw new Exception(message);}
 static async Task Until(Func<bool> done,int ms,string message)
 {
  var watch=System.Diagnostics.Stopwatch.StartNew();while(!done()){if(watch.ElapsedMilliseconds>ms)throw new Exception(message);await Task.Delay(20);}
 }
 static async Task<(EffectTester,NativeHeroDeathPresentation)> Start()
 {
  await Task.Delay(150);var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.ShowMenu();await Task.Delay(50);e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
  await Until(()=>!e.ActionEffectsBusy&&!e.handDisplay.drawPresentation.IsBusy,10000,"Opening presentation did not finish");
  var death=UnityEngine.Object.FindAnyObjectByType<NativeHeroDeathPresentation>();Check(death!=null&&death.library!=null,"Original death presenter not installed");
  Check(death.library.debris.Select(c=>c.path).Distinct().Count()==17,"Original 17-piece motion missing");Check(death.library.crackSound!=null&&death.library.explodeSound!=null,"Source hero crack/explosion sounds absent");return(e,death);
 }
 static void Resolve(EffectTester e)
 {
  var manager=(GameManager)typeof(EffectTester).GetField("gameManager",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(e);manager.AfterGameAction();
 }
 public static async Task<string> Main()=>await One(false);
 public static async Task<string> Player()=>await One(true);
 static async Task<string> One(bool player)
 {
  var(e,d)=await Start();var face=player?e.faceView:e.opponentFaceView;var labels=player?d.playerLabels:d.opponentLabels;var power=player?e.heroPowerButton:e.opponentHeroPowerButton;
  face.Player.TakeDamage(face.Player.Health+face.Player.Armor);Resolve(e);
  await Until(()=>d.ActiveEffects.Length==1,1000,"Lethal hero did not start source crack sequence");Check(d.IsBusy&&!e.gameOverText.gameObject.activeSelf&&!e.playAgainButton.gameObject.activeSelf,"Result UI interrupted hero death");
  var crack=face.avatarImage.GetComponentsInChildren<NativeTargetPart>(true).SingleOrDefault(p=>p.name=="Original hero crack geometry");Check(crack!=null&&crack.points.Length>0&&crack.paint==d.library.crackPaint&&!crack.raycastTarget,"Source crack UI topology/paint missing");
  Check(d.ActiveEffects[0].GetComponentInChildren<AudioSource>().isPlaying,"Original crack sound did not play");
  await Until(()=>d.ExplosionsStarted==1,4000,"Source hero crack phase never exploded");
  var stage=d.ActiveEffects.Single();var debris=stage.transform.Find("Original hero death/HeroBombDebris");
  if(debris==null)debris=stage.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="HeroBombDebris");
  var renderers=debris.GetComponentsInChildren<MeshRenderer>(true);Check(renderers.Length==17&&renderers.All(r=>r.enabled),"Original hero fragments not shown");
  Check(face.GetComponent<CanvasGroup>().alpha==0&&labels.All(g=>g.alpha==0)&&!power.gameObject.activeSelf,"Hero/health/armor/power remained floating after explosion");
  var positions=renderers.Select(r=>r.transform.localPosition).ToArray();await Task.Delay(250);
  Check(renderers.Where((r,i)=>(r.transform.localPosition-positions[i]).sqrMagnitude>.00001f).Any(),"Original hero fragment curves did not move geometry");
  Check(stage.GetComponentsInChildren<ParticleSystem>(true).Any(p=>p.isPlaying||p.particleCount>0),"Original explosion particles did not play");
  await Until(()=>d.CompletedCount==1&&!d.IsBusy,10000,"Source death sequence never finished");Check(e.gameOverText.gameObject.activeSelf&&e.playAgainButton.gameObject.activeSelf,"Source death finished without releasing result UI");
  e.playAgainButton.onClick.Invoke();await Task.Delay(150);Check(d.ActiveEffects.Length==0&&d.ExplosionsStarted==0&&d.CompletedCount==0&&labels.All(g=>g.alpha>0),"Play Again retained death state/hidden labels");
  e.frontEnd.ShowMenu();await Task.Delay(50);Check(d.ActiveEffects.Length==0&&labels.All(g=>g.alpha>0),"Menu left death stages/labels hidden");
  e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();await Task.Delay(100);Check(!d.IsBusy&&d.ExplosionsStarted==0&&Visible(e.faceView)&&Visible(e.opponentFaceView),"New match retained death state");
  return (player?"Player":"Opponent")+" source crack topology+audio,17-piece explosion motion/paint, native particles, hidden portrait/labels/power, delayed result and menu/new-game cleanup passed.";
 }
 public static async Task<string> Both()
 {
  var(e,d)=await Start();e.faceView.Player.TakeDamage(100);e.opponentFaceView.Player.TakeDamage(100);Resolve(e);
  await Until(()=>d.ExplosionsStarted==2,4000,"Simultaneous lethal did not explode both original heroes");
  Check(!e.gameOverText.gameObject.activeSelf,"Draw label appeared over active death effects");
  await Until(()=>d.CompletedCount==2&&!d.IsBusy,10000,"Double hero death did not finish");Check(e.gameOverText.gameObject.activeSelf&&e.gameOverText.text=="Draw!","Death presentation changed simultaneous-draw result");
  e.frontEnd.ShowMenu();await Task.Delay(50);Check(d.ActiveEffects.Length==0,"Double hero effects leaked into menu");return "Simultaneous source hero deaths preserve Draw, delayed results and cleanup.";
 }
}
