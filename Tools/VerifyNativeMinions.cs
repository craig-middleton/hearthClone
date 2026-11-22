using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class VerifyNativeMinions
{
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 static void Refresh(EffectTester e)=>typeof(EffectTester).GetMethod("RefreshAll",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(e,null);
 static async Task Until(Func<bool> b,int timeout,string message){var t=System.Diagnostics.Stopwatch.StartNew();while(!b()){if(t.ElapsedMilliseconds>timeout)throw new Exception(message);await Task.Delay(20);}}
 public static async Task<string> Main()
 {
  await Task.Delay(150);var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.ShowMenu();e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
  await Until(()=>!e.ActionEffectsBusy&&!e.handDisplay.drawPresentation.IsBusy,10000,"Opening presentation busy");
  var owner=e.faceView.Player;var opponent=e.opponentFaceView.Player;owner.BoardMinions.Clear();opponent.BoardMinions.Clear();
  var artwork=e.cardPool.First(c=>c.cardType==CardType.Minion&&c.artwork!=null).artwork;
  var ready=new Minion("Ready source minion",3,5,false,artwork,true);var shield=new Minion("Source taunt",2,7,true,artwork,true);var asleep=new Minion("Summoning sickness",4,6,false,artwork);
  owner.BoardMinions.AddRange(new[]{ready,shield,asleep});Refresh(e);await Task.Delay(180);
  foreach(var m in owner.BoardMinions)
  {
   var view=e.boardDisplay.GetViewTransform(m).GetComponent<MinionView>();var native=view.GetComponent<NativeMinionPresentation>();
   Check(native!=null&&native.stage!=null&&native.actor!=null,"Source minion was not instantiated");
   var body=native.actor.Find("RootObject/Card_InPlay_Ally/Mesh").GetComponent<MeshRenderer>();
   Check(body.GetComponent<MeshFilter>().sharedMesh.vertexCount==532&&body.sharedMaterials.Length==2,"Original oval portrait/frame topology absent");
   Check(body.sharedMaterials.All(p=>p!=null&&p.shader.isSupported&&p.mainTexture!=null),"Source frame/portrait paint or compatibility shader missing");
   Check(body.sharedMaterials.All(p=>p.GetColor("_BaseColor").a>0.99f),"Missing-shader getters erased original portrait/frame opacity");
   Check(native.highlight.sharedMaterial.GetColor("_Color").g>.8f&&native.highlight.sharedMaterial.GetColor("_Color").r<.05f,"Playable actor state does not use original green material");
   Check(!view.artworkImage.enabled&&view.minionBackground.color.a==0,"Legacy atlas still overlays original model");
   Check(native.highlight.enabled==m.CanAttack,"Native eligibility ring does not match current minion rules");
   Check(native.taunt.gameObject.activeSelf==m.HasTaunt,"Native taunt model not following minion tag");
   Vector2 center=native.boardCamera.WorldToScreenPoint(body.bounds.center);Check(Vector2.Distance(center,view.transform.position)<8,"Original oval model does not align with existing input");
   var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=view.transform.position},hits);
   Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==view.button,"Source minion lost its first input hit");
   foreach(var pair in new[]{(view.attackText,"RootObject/AttackObject/Gem_Attack"),(view.healthText,"RootObject/HealthObject/Gem_Health")})
   {var gem=native.actor.Find(pair.Item2).GetComponent<MeshRenderer>();Check(Vector2.Distance(pair.Item1.rectTransform.position,native.boardCamera.WorldToScreenPoint(gem.bounds.center))<1,"Source stat label detached from gem");}
   Check(view.attackText.text==m.CurrentAttack.ToString()&&view.healthText.text==m.CurrentHealth.ToString(),"Source gems do not display live stats");
   if(m.HasTaunt){var renderer=native.taunt.GetComponentsInChildren<MeshRenderer>().Single();Check(Mathf.Abs(renderer.transform.localScale.x-.9775736f)<1e-6f,"Taunt stayed at birth scale");Check(renderer.bounds.size.x>body.bounds.size.x,"Original taunt shield is not surrounding the minion");}
  }
  // Refresh rebinds legacy art; prove the model adapter suppresses it again.
  var source=e.boardDisplay.GetViewTransform(ready).GetComponent<MinionView>();source.SetMinion(ready,null,true,true);await Task.Delay(60);Check(!source.artworkImage.enabled,"SetMinion restored legacy portrait over the model");
  // Held-before-first-render creates a local sorting Canvas; initialization must use rootCanvas.
  var heldMinion=new Minion("Held before first render",1,1,false,artwork);owner.BoardMinions.Add(heldMinion);Refresh(e);
  var held=e.boardDisplay.GetViewTransform(heldMinion).GetComponent<MinionView>();held.BeginHold(5);owner.BoardMinions.Remove(heldMinion);Refresh(e);await Task.Delay(120);
  var heldNative=held.GetComponent<NativeMinionPresentation>();Check(heldNative.stage!=null&&heldNative.stage.gameObject.activeInHierarchy,"Local hold Canvas broke source stage lookup");
  var heldStage=heldNative.stage;held.EndHold();Refresh(e);await Task.Delay(100);Check(heldStage==null,"Expired held view leaked its world model");
  var stages=e.boardDisplay.boardPanel.GetComponentsInChildren<NativeMinionPresentation>().Select(n=>n.stage).ToArray();e.frontEnd.ShowMenu();Check(stages.All(t=>t==null||!t.gameObject.activeInHierarchy),"Menu left source minion models visible");
  e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();await Task.Delay(100);
  return "Original 532-vertex oval portrait/frame, source gems/shadow/taunt settled scale, native eligibility rings, input/stat alignment, refreshed-art suppression, held-before-render rootCanvas, model disposal and menu/reset passed.";
 }
}
