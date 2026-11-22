using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HearthstoneClone.UI;
public static class VerifyNativeHeroPowers
{
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 public static async Task<string> Main()
 {
  await Task.Delay(150);var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.ShowMenu();e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
  var opening=System.Diagnostics.Stopwatch.StartNew();
  while(e.ActionEffectsBusy||e.handDisplay.drawPresentation.IsBusy){if(opening.ElapsedMilliseconds>10000)throw new Exception("Opening presentation did not finish");await Task.Delay(20);}
  await Task.Delay(60);
  var a=e.heroPowerButton.GetComponent<NativeHeroPowerPresentation>();var b=e.opponentHeroPowerButton.GetComponent<NativeHeroPowerPresentation>();
  foreach(var c in new[]{a,b})
  {
   Check(c!=null&&c.usedCurves.Length>0&&c.restoreCurves.Length>0,"Source power curves missing");
   Check(c.stage.gameObject.activeInHierarchy,"Native power stage not active");
   var r=c.actor.GetComponentsInChildren<MeshRenderer>(true).Single(x=>x.name=="Mesh");
   Check(r.GetComponent<MeshFilter>().sharedMesh.subMeshCount==3&&r.sharedMaterials.All(m=>m.shader.isSupported&&m.mainTexture!=null),"Original three-part power model/paint absent");
   Check(c.actor.GetComponentsInChildren<Animation>(true).All(x=>!x.enabled),"Unsafe original animation player remained enabled");
   var shadow=c.actor.GetComponentsInChildren<MeshRenderer>().Single(x=>x.name=="shadow").sharedMaterial;Check(shadow.shader.name=="HearthClone/Compatibility/Source White Neutral Shadow","Original white-neutral shadow not multiplicative");
   var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=c.button.position},hits);
   Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==c.button.GetComponent<Button>(),"Power input covered");
   Vector2 screen=c.boardCamera.WorldToScreenPoint(r.bounds.center);Check(Vector2.Distance(screen,c.button.position)<8,"Original power face misaligned with input");
  }
  var owner=e.faceView.Player;owner.CurrentMana=10;typeof(EffectTester).GetMethod("RefreshFaceDisplay",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(e,null);await Task.Delay(50);Check(a.highlight.enabled,"Original ready highlight not visible");
  var initial=a.actor.localRotation;owner.HasUsedHeroPowerThisTurn=true;var motion=System.Diagnostics.Stopwatch.StartNew();
  while(Quaternion.Angle(initial,a.actor.localRotation)<=20){if(motion.ElapsedMilliseconds>1200)throw new Exception("Original used motion did not start");await Task.Delay(10);}
  await Task.Delay(500);Check(Quaternion.Angle(initial,a.actor.localRotation)>170&&!a.highlight.enabled,"Power flip did not complete to class back");
  owner.HasUsedHeroPowerThisTurn=false;await Task.Delay(650);Check(Quaternion.Angle(initial,a.actor.localRotation)<5,"Original restore motion failed");
  e.frontEnd.ShowMenu();Check(!a.stage.gameObject.activeInHierarchy&&!b.stage.gameObject.activeInHierarchy,"Menu left power models visible");
  e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();await Task.Delay(150);Check(Quaternion.Angle(initial,a.actor.localRotation)<5,"New game retained used power pose");
  return "Both source three-submesh models, original paint/shadow/curve bindings, screen/input alignment, ready highlight, full used/restore motion, menu/new-game reset passed.";
 }
}
