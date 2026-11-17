using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class VerifyNativeMulligan
{
 static void Check(bool b,string message){if(!b)throw new Exception(message);}
 static void Click(Button button)=>ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=button.transform.position},ExecuteEvents.pointerClickHandler);
 public static async Task<string> Main()
 {
  await Task.Delay(150);var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.Play();await Task.Delay(50);
  var views=e.mulliganPanel.GetComponentsInChildren<CardView>();Check(views.Length==3,"Starting hand count changed");
  var style=e.mulliganPanel.GetComponent<NativeMulliganPresentation>();Check(style!=null&&style.library.shapes.Length==5,"Native source art missing");
  foreach(var view in views){var marker=view.GetComponent<NativeMulliganCard>();Check(marker!=null&&marker.keep.activeSelf&&!marker.cross.activeSelf,"Keep state missing");Check(((RectTransform)view.transform).rect.width==300,"Mulligan cards not enlarged");}
  var selected=views[1];var instance=selected.Card;Click(selected.button);var mark=selected.GetComponent<NativeMulliganCard>();
  Check(mark.cross.activeSelf&&mark.replace.activeSelf&&!mark.keep.activeSelf,"Replace click did not show source X/badge");
  Check(views.Where(v=>v!=selected).All(v=>v.GetComponent<NativeMulliganCard>().keep.activeSelf),"Replace click changed another card");
  Check(new[]{mark.keep,mark.replace,mark.cross}.SelectMany(o=>o.GetComponentsInChildren<Graphic>(true)).All(g=>!g.raycastTarget),"Mulligan marks intercept input");
  var overlay=UnityEngine.Object.FindAnyObjectByType<MatchPresentation>();Check(overlay.mulliganShade.rect.width>=((RectTransform)overlay.mulliganShade.GetComponentInParent<Canvas>().rootCanvas.transform).rect.width-1,"Dimming leaves viewport edge uncovered");
  Click(e.confirmMulliganButton);await Task.Delay(50);Check(!e.mulliganPanel.gameObject.activeSelf&&!overlay.mulliganHeading.activeSelf,"Confirm did not dismiss native presentation");
  Check(!e.handDisplay.handPanel.GetComponentsInChildren<CardView>().Any(v=>ReferenceEquals(v.Card,instance)),"Chosen instance was not replaced");
  e.frontEnd.ShowMenu();e.frontEnd.Play();await Task.Delay(50);Check(e.mulliganPanel.GetComponentsInChildren<CardView>().Length==3,"Restart duplicates/misses cards");
  Click(e.confirmMulliganButton);await Task.Delay(50);e.frontEnd.ShowMenu();Check(!overlay.mulliganHeading.activeInHierarchy,"Menu left mulligan overlay active");
  return "Original banner/confirm/keep-replace-X art, larger cards, real replacement clicks/per-copy identity, full viewport shade, confirm cleanup, and restart/menu passed.";
 }
}
