using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using HearthstoneClone.UI;
public static class PreviewNativeHeroDeath
{
 public static async Task<string> Main()
 {
  var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.ShowMenu();e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
  while(e.ActionEffectsBusy||e.handDisplay.drawPresentation.IsBusy)await Task.Delay(20);
  e.opponentFaceView.Player.TakeDamage(100);
  ((GameManager)typeof(EffectTester).GetField("gameManager",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(e)).AfterGameAction();
  var d=UnityEngine.Object.FindAnyObjectByType<NativeHeroDeathPresentation>();var timer=System.Diagnostics.Stopwatch.StartNew();
  while(d.ExplosionsStarted==0){if(timer.ElapsedMilliseconds>6000)throw new Exception("Original hero explosion did not start");await Task.Delay(10);}
  await Task.Delay(100);return "Original opponent hero explosion preview; disposable Play fixture.";
 }
}
