using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using TMPro;
using HearthstoneClone.Cards;
using HearthstoneClone.UI;
public static class VerifyDeckPockets
{
 public static string Main()
 {
  if(!EditorApplication.isPlaying)throw new Exception("Play mode only");
  var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
  var flags=BindingFlags.NonPublic|BindingFlags.Instance;var refresh=typeof(EffectTester).GetMethod("RefreshAll",flags);
  foreach(var pair in new[]{(e.faceView,"playerOneHand"),(e.opponentFaceView,"playerTwoHand")})
  {
   var hand=(PlayerHand)typeof(EffectTester).GetField(pair.Item2,flags).GetValue(e);
   var so=new SerializedObject(pair.Item1);var number=(TMP_Text)so.FindProperty("deckCountText").objectReferenceValue;
   if(number.text!=hand.Deck.Count.ToString())throw new Exception("Deck count mismatch");
   var pocket=number.transform.parent;if(((RectTransform)pocket).anchoredPosition.x<700)throw new Exception("Deck is not on right edge");
   var count=hand.Deck.Count;hand.DrawCard();refresh.Invoke(e,null);if(number.text!=(count-1).ToString())throw new Exception("Draw did not update pocket");
   var saved=hand.Deck.ToList();hand.Deck.Clear();refresh.Invoke(e,null);
   var array=so.FindProperty("deckPileLayers");for(int i=0;i<array.arraySize;i++)if(((GameObject)array.GetArrayElementAtIndex(i).objectReferenceValue).activeSelf)throw new Exception("Empty deck still shows cards");
   if(number.text!="0")throw new Exception("Empty deck count wrong");hand.Deck.AddRange(saved);refresh.Invoke(e,null);
  }
  return "Both right-edge pockets match independent model counts, update after draws, and hide every stack layer at zero";
 }
}
