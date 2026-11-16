using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public static class VerifyNativeMatchControls
{
    static void Check(bool b,string m){if(!b)throw new Exception(m);}
    public static async Task<string> Main()
    {
        await Task.Delay(150);var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
        var initial=System.Diagnostics.Stopwatch.StartNew();while(e.ActionEffectsBusy||e.handDisplay.drawPresentation.IsBusy){if(initial.Elapsed.TotalSeconds>10)throw new Exception("Opening draw stuck");await Task.Delay(30);}
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        e.faceView.Player.CurrentMana=10;typeof(EffectTester).GetMethod("RefreshAll",flags).Invoke(e,null);
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        ExecuteEvents.Execute(e.heroPowerButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        e.targetingRibbon.PreviewPointer=e.opponentFaceView.transform.position;e.targetingRibbon.SendMessage("Update");
        var native=e.targetingRibbon.nativePresentation;
        Check(native!=null&&native.head.gameObject.activeSelf&&native.ring.gameObject.activeSelf&&native.dashes.Any(d=>d.gameObject.activeSelf),"Native aiming shapes absent");
        Check(native.GetComponentsInChildren<NativeTargetPart>(true).All(p=>!p.raycastTarget&&p.triangles.Length>0&&p.paint!=null),"Native parts intercept input or lack mesh/paint");
        e.CancelPendingInput();Check(!native.head.gameObject.activeSelf&&!native.ring.gameObject.activeSelf&&native.dashes.All(d=>!d.gameObject.activeSelf),"Cancel left native aiming visible");
        await Task.Delay(50);pointer.position=e.endTurnButton.transform.position;var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==e.endTurnButton,"End Turn is covered by another input target");
        var button=e.endTurnButton.GetComponent<NativeTurnButtonPresentation>();Check(button.model.activeInHierarchy,"Native button not visible");
        ExecuteEvents.Execute(e.endTurnButton.gameObject,pointer,ExecuteEvents.pointerDownHandler);await Task.Delay(100);
        Check(button.animator.transform.localPosition.y<-.1f,"Native press did not depress mesh");
        ExecuteEvents.Execute(e.endTurnButton.gameObject,pointer,ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(e.endTurnButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);await Task.Delay(550);
        Check(Quaternion.Angle(button.animator.transform.localRotation,Quaternion.identity)>140,"Actual End Turn click did not flip native mesh");
        var timer=System.Diagnostics.Stopwatch.StartNew();while(e.ActionEffectsBusy){if(timer.Elapsed.TotalSeconds>25)throw new Exception("Action effects did not finish");await Task.Delay(40);}
        var returning=System.Diagnostics.Stopwatch.StartNew();while(button.IsTransitioning){button.OnPointerEnter(pointer);button.OnPointerExit(pointer);if(returning.Elapsed.TotalSeconds>5)throw new Exception("Return flip stuck under hover");await Task.Delay(25);}
        await Task.Delay(100);Check(Quaternion.Angle(button.animator.transform.localRotation,Quaternion.identity)<5,"Button did not return to ready face");
        e.frontEnd.ShowMenu();Check(!button.model.activeInHierarchy,"Menu left native button visible");
        e.frontEnd.Play();Check(!button.model.activeInHierarchy,"Mulligan should hide the turn button");e.confirmMulliganButton.onClick.Invoke();Check(button.model.activeInHierarchy,"Confirmed new game did not restore native button");
        return "Live aiming topology/paint/input/cancel pass; actual press depresses, End Turn click flips and returns after AI effects; menu/restart visibility pass.";
    }
}
