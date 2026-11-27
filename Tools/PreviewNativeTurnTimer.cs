using System;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEngine;
public static class PreviewNativeTurnTimer
{
 public static async Task<string> Main()
 {
  var game=UnityEngine.Object.FindAnyObjectByType<EffectTester>();game.frontEnd.ShowMenu();game.frontEnd.Play();game.confirmMulliganButton.onClick.Invoke();
  await Task.Delay(500);
  var clock=(TurnClock)typeof(EffectTester).GetField("turnClock",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
  clock.Advance(Mathf.Max(0,game.TurnTimeRemaining-18));
  await Task.Delay(1300);
  clock.Advance(game.TurnTimeRemaining*.45f);await Task.Delay(100);
  var rope=game.frontEnd.tableSurface.GetComponent<NativeTurnTimerPresentation>();
  return $"Disposable original-rope preview: visible={rope.RopeVisible}, remaining={game.TurnTimeRemaining}, burned={rope.BurnProgress}. Stop Play to discard.";
 }
 public static async Task<string> Expiry()
 {
  await Main();var game=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
  var clock=(TurnClock)typeof(EffectTester).GetField("turnClock",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
  clock.Advance(game.TurnTimeRemaining+1);await Task.Delay(150);
  return "Disposable original expiry smoke/button animation preview; stop Play to discard.";
 }
}
