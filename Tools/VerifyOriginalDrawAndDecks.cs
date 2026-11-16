using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyOriginalDrawAndDecks
{
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 public static async Task<string> Main()
 {
  await Task.Delay(150);var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
  var m=e.handDisplay.drawPresentation;Check(m.ActiveCount==1,"Turn-one deck draw missing");
  var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  var flights=(IDictionary)typeof(NativeCardDrawPresentation).GetField("flights",flags).GetValue(m);
  var f=flights.Values.Cast<object>().Single();var ft=f.GetType();var root=(GameObject)ft.GetField("root").GetValue(f);
  Check(root.GetComponent<Canvas>().renderMode==RenderMode.WorldSpace,"Draw lost original camera projection");
  Check(root.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"Draw decorations intercept input");
  typeof(EffectTester).GetMethod("RefreshAll",flags).Invoke(e,null);
  Check(ReferenceEquals(f,flights.Values.Cast<object>().Single()),"Refresh restarted draw");
  var target=(CardView)ft.GetField("target").GetValue(f);var group=target.GetComponent<CanvasGroup>();
  Check(group.alpha==0&&!group.blocksRaycasts,"Hidden live card receives input");
  // Compare all four corners at handoff, independently of source path evaluation.
  typeof(NativeCardDrawPresentation).GetMethod("Sample",flags).Invoke(m,new[]{f,(object)2f});
  var front=(GameObject)ft.GetField("front").GetValue(f);var a=new Vector3[4];var b=new Vector3[4];
  ((RectTransform)front.transform).GetWorldCorners(a);((RectTransform)target.transform).GetWorldCorners(b);
  for(int i=0;i<4;i++)Check(Vector2.Distance(m.boardCamera.WorldToScreenPoint(a[i]),b[i])<1.5f,"Draw-to-hand corner mismatch");
  await Task.Delay(2200);Check(!m.IsBusy&&e.endTurnButton.interactable,"Draw did not restore End Turn");
  Check(e.handDisplay.handPanel.GetSiblingIndex()>e.faceView.transform.GetSiblingIndex(),"Hero labels paint over hand");
  foreach(var face in new[]{e.faceView,e.opponentFaceView})
  {
   var pile=face.nativeDeck;Check(pile!=null&&pile.states.Length==5,"Original deck states missing");
   foreach(int n in new[]{30,26,19,12,5,1,0}){pile.SetRemaining(n);Check(pile.states.Count(s=>s.activeSelf)==(n==0?0:1),"Deck state overlap/empty deck error");}
  }
  e.frontEnd.ShowMenu();Check(!m.IsBusy&&(root==null||!root.activeInHierarchy),"Menu left draw active");
  e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();Check(m.ActiveCount==1,"Restart lost draw");Check(e.endTurnButton.interactable,"Draw globally disabled End Turn");
  var hand=(HearthstoneClone.Cards.PlayerHand)typeof(EffectTester).GetField("playerOneHand",flags).GetValue(e);int remaining=hand.Deck.Count;e.endTurnButton.onClick.Invoke();Check(hand.Deck.Count==remaining-1,"End Turn ignored during draw");
  e.frontEnd.ShowMenu();Check(!m.IsBusy,"Restart cancellation stuck");
  return "Turn-one draw, refresh continuity, nonblocking snapshot, hidden-card input, four-corner handoff, input restoration, hover draw order, both five-stage decks/empty state, menu and restart passed.";
 }
}
