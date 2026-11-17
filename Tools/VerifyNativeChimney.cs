using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.EventSystems;
public static class VerifyNativeChimney
{
 static void Check(bool b,string message){if(!b)throw new Exception(message);}
 public static async Task<string> Main()
 {
  await Task.Delay(200);var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();await Task.Delay(100);
  var c=UnityEngine.Object.FindAnyObjectByType<NativeChimneyInteraction>();Check(c!=null&&c.sounds.Length==3&&c.sounds.All(s=>s!=null),"Original chimney or audio missing");
  var listener=UnityEngine.Object.FindAnyObjectByType<AudioListener>();Check(c.voice.minDistance>=Vector3.Distance(listener.transform.position,c.voice.transform.position),"Chimney sound attenuates before reaching the board camera");
  var ember=c.puff.GetComponentsInChildren<ParticleSystem>().Single(p=>p.name=="FX_Embers");
  Check(c.puff.main.duration==2&&c.ambientSmoke.main.loop&&Mathf.Abs(c.ambientSmoke.main.duration-5)<.01f,"Source particle timing changed");
  foreach(var p in new[]{c.puff,ember,c.ambientSmoke}){var r=p.GetComponent<ParticleSystemRenderer>();Check(r.enabled&&r.sharedMaterial!=null&&r.sharedMaterial.shader.isSupported&&r.sharedMaterial.mainTexture!=null,"Missing particle paint/shader");}
  Vector2 point=c.boardCamera.WorldToScreenPoint(c.clickArea.bounds.center);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
  Check(hits.Count==0,"Chimney click is intercepted by UI: "+string.Join(",",hits.Select(h=>h.gameObject.name)));
  Check(c.TryClick(point)&&c.SmokeStopped,"First original chimney click failed");Check(!c.TryClick(point),"Source .2s wait gate ignored");await Task.Delay(350);
  Check(c.puff.particleCount>0&&ember.particleCount>0,"Original smoke/ember bursts not emitted");Check(c.voice.isPlaying,"Blacksmith sound did not start");
  Check(c.TryClick(point)&&!c.SmokeStopped&&c.ambientSmoke.isPlaying,"Second click did not resume stack smoke");
  Check(!c.TryClick(Vector2.zero),"Unrelated click triggered chimney");
  var hand=(RectTransform)e.handDisplay.handPanel;var portrait=e.faceView.avatarImage.rectTransform;var target=hand.parent.InverseTransformPoint(portrait.TransformPoint(portrait.rect.center));
  Check(Mathf.Abs(hand.anchoredPosition.x-target.x)<.1f,"Player hand is not centred under hero");
  e.frontEnd.ShowMenu();Check(!c.isActiveAndEnabled&&!c.voice.isPlaying&&!c.puff.IsAlive(true)&&!c.ambientSmoke.IsAlive(false),"Menu left chimney effects/audio active");
  e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();Check(!c.SmokeStopped&&c.ClickCount==0,"Restart did not reset source chimney state");
  return "Native chimney screen hit/UI clearance, source smoke+ember particles/timing, three sounds, alternating states/.2s gate, hand centring, menu/reset passed.";
 }
}
