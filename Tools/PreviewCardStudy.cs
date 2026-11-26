using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEngine;

// Disposable real hand views, enlarged for a material/typography review. Stop Play to discard.
public static class PreviewCardStudy
{
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 public static async Task<string> Main()
 {
  var game=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
  game.frontEnd.ShowMenu();game.frontEnd.Play();game.confirmMulliganButton.onClick.Invoke();
  var timer=System.Diagnostics.Stopwatch.StartNew();
  while(game.ActionEffectsBusy||game.handDisplay.drawPresentation.IsBusy)
  {if(timer.ElapsedMilliseconds>10000)throw new Exception("Opening draw busy");await Task.Delay(20);}
  var hand=(PlayerHand)typeof(EffectTester).GetField("playerOneHand",Private).GetValue(game);
  hand.Hand.Clear();foreach(string name in new[]{"Ironfur Grizzly","Fireball"})hand.AddCardToHand(game.cardPool.Single(c=>c.cardName==name));
  game.faceView.Player.MaxMana=10;game.faceView.Player.CurrentMana=10;
  typeof(EffectTester).GetMethod("RefreshAll",Private).Invoke(game,null);await Task.Delay(100);
  var views=game.handDisplay.handPanel.GetComponentsInChildren<CardView>();
  game.handDisplay.handPanel.GetComponent<HandFanLayout>().enabled=false;
  var canvas=(RectTransform)game.handDisplay.handPanel.GetComponentInParent<Canvas>().rootCanvas.transform;
  for(int i=0;i<views.Length;i++)
  {var rect=(RectTransform)views[i].transform;rect.position=canvas.TransformPoint(new Vector3(i==0?-235:235,10,0));rect.localRotation=Quaternion.identity;rect.localScale=Vector3.one*2.2f;}
  await Task.Delay(150);return "Real Grizzly and Fireball hand views enlarged; source material study only, discard Play fixture.";
 }
}
