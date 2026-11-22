using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using HearthstoneClone.Core;
using HearthstoneClone.Cards;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyNativeSpells
{
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static async Task Until(Func<bool> condition,int limit,string message)
 {var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){if(timer.ElapsedMilliseconds>limit)throw new Exception(message);await Task.Delay(20);}}
 static bool HasPaint(RenderTexture source)
 {
  var target=RenderTexture.GetTemporary(32,32,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
  var cpu=new Texture2D(32,32,TextureFormat.RGBA32,false);
  try{Graphics.Blit(source,target);RenderTexture.active=target;cpu.ReadPixels(new Rect(0,0,32,32),0,0);cpu.Apply();return cpu.GetPixels().Any(c=>c.r+c.g+c.b>.001f);}
  finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(cpu);}
 }
 static async Task Cast(EffectTester e,string name,Target target)
 {
  var player=e.faceView.Player;var hand=(PlayerHand)typeof(EffectTester).GetField("playerOneHand",Flags).GetValue(e);var card=e.cardPool.First(c=>c.cardName==name);
  hand.Hand.Clear();hand.AddCardToHand(card);player.CurrentMana=10;
  int count=e.battleEffects.originalSpells.PlayedCount;
  Check(hand.PlayCard(hand.Hand[0],new GameContext(new Board(e.faceView.Player,e.opponentFaceView.Player)),target),"Actual "+name+" cast rejected");
  Check(player.CurrentMana==10-card.manaCost,"Original effect path changed mana spending");
  typeof(EffectTester).GetMethod("RefreshAll",Flags).Invoke(e,null);
  await Until(()=>e.battleEffects.originalSpells.PlayedCount==count+1,1000,"Resolved "+name+" did not enter original source sequence");
  await Task.Delay(150);
  var cameras=UnityEngine.Object.FindObjectsByType<Camera>().Where(c=>c.name=="Original spell source viewport camera").ToArray();
  Check(cameras.Length>0&&cameras.All(c=>c.cullingMask==(1<<31)&&c.GetComponent<AudioListener>()==null),"Spell capture layer/listener isolation failed");
  Check(UnityEngine.Object.FindObjectsByType<AudioSource>().Any(a=>a.name.StartsWith("Original ")&&a.name.EndsWith(" sound")&&a.isPlaying),"Original source spell sound did not start");
  Check(cameras.Any(c=>HasPaint(c.targetTexture)),"Original source spell geometry/particles did not render any paint");
  Check(e.battleEffects.overlay.GetComponentsInChildren<RawImage>().All(r=>!r.raycastTarget),"Original spell presentation intercepts input");
  await Until(()=>!e.battleEffects.IsBusy,2500,"Original spell finish delay did not release battle queue");
  Check(e.battleEffects.originalSpells.ActiveCount>0,"Source impact/trail tails vanished when gameplay queue finished");
 }
 public static async Task<string> Main()
 {
  await Task.Delay(150);var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.ShowMenu();e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
  await Until(()=>!e.ActionEffectsBusy&&!e.handDisplay.drawPresentation.IsBusy,10000,"Opening draw did not finish");
  var native=e.battleEffects.originalSpells;Check(native!=null&&native.library.entries.Length==2,"Original spell library not installed");
  foreach(var entry in native.library.entries)
  {
   Check(Mathf.Abs(entry.pathDuration-.4f)<.00001f&&entry.easeType==0,"Original .4s easeInQuad source path changed");
   Check(entry.missile.sound!=null&&entry.impact.sound!=null,"Verified original spell clips missing");
   string portrait=entry.cardName=="Fireball"?"W15_A087_D":"WoW_CHI_006_D";
   var card=e.cardPool.First(c=>c.cardName==entry.cardName);
   Check(card.artwork!=null&&card.artwork.texture.name==portrait,"Playable "+entry.cardName+" does not use its CardDef-proven original artwork");
  }
  var fireball=native.library.entries.First(entry=>entry.cardName=="Fireball");
  var core=fireball.missile.prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials)
   .First(m=>m!=null&&m.name=="GenFX_FireballCore_Compatibility");
  var tint=core.GetColor("_TintColor");
  Check(core.shader.isSupported&&Mathf.Abs(tint.r-1)<.00001f&&Mathf.Abs(tint.g-.08208954f)<.00001f&&Mathf.Abs(tint.a-.45882353f)<.00001f,
   "Original Fireball tint was lost while adapting its missing source shader");
  int health=e.opponentFaceView.Player.Health;await Cast(e,"Fireball",new Target(e.opponentFaceView.Player));Check(e.opponentFaceView.Player.Health<health,"Actual Fireball did not damage hero");
  var enemy=e.opponentFaceView.Player;var art=e.cardPool.First(c=>c.cardType==CardType.Minion&&c.artwork!=null).artwork;var minion=new Minion("Original frost lethal fixture",2,1,false,art);enemy.BoardMinions.Add(minion);typeof(EffectTester).GetMethod("RefreshAll",Flags).Invoke(e,null);await Task.Delay(50);
  await Cast(e,"Frostbolt",new Target(minion));Check(minion.IsDead,"Actual Frostbolt did not resolve lethal damage");
  await Until(()=>native.ActiveCount==0,7000,"Source spell tails/cameras did not release after original lifetimes");
  Check(!e.battleEffects.overlay.GetComponentsInChildren<Transform>().Any(t=>t.name=="CombatSnapshot"),"Lethal original spell left a combat snapshot");
  await Cast(e,"Fireball",new Target(enemy));e.frontEnd.ShowMenu();Check(native.ActiveCount==0,"Menu retained original spell stages");
  Check(!UnityEngine.Object.FindObjectsByType<Camera>().Any(c=>c.name=="Original spell source viewport camera"&&c.enabled),"Menu left capture camera active");
  e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();Check(native.PlayedCount==0,"New match retained source spell history");
  return "Actual Fireball hero damage and Frostbolt lethal-minion casts, source .4s/ease0 paths, real source geometry/particle paint, exact audio bindings, capture isolation/input, queue release and independent original tails, lethal snapshot/menu/new-game cleanup passed.";
 }
}
