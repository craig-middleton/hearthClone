using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using UnityEngine;
public static class VerifyNativeManaRack
{
 static void Check(bool b,string message){if(!b)throw new Exception(message);}
 static MeshRenderer[] Models(){var root=GameObject.Find("Original mana crystals");return root==null?new MeshRenderer[0]:root.GetComponentsInChildren<MeshRenderer>(true);}
 public static async Task<string> Main()
 {
  await Task.Delay(200);var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
  var r=e.faceView.nativeManaRack;var flags=BindingFlags.NonPublic|BindingFlags.Instance;var refresh=typeof(EffectTester).GetMethod("RefreshAll",flags);
  var player=e.faceView.Player;player.MaxMana=10;player.CurrentMana=7;refresh.Invoke(e,null);await Task.Delay(2250);
  var models=Models();Check(models.Length==10&&models.All(m=>m.enabled),"Expected ten visible native resource models");
  Check(models.All(m=>m.GetComponent<MeshFilter>().sharedMesh==r.sourceMesh),"Crystals do not share original geometry");
  Check(r.crystalMaterial.shader.isSupported&&r.crystalMaterial.GetTexture("_ScrollingTex")!=null,"Compatibility shader or original reflection map missing");
  int full=0,spent=0;foreach(var model in models){var block=new MaterialPropertyBlock();model.GetPropertyBlock(block);var tint=block.GetColor("_tint");if(tint.a>.99f)full++;else if(tint.a<.01f)spent++;}
  Check(full==7&&spent==3,"Source mana states do not match 7/10");
  Check(!e.frontEnd.tableSurface.transform.Find("ManaTray").gameObject.activeSelf,"UI tray overlay returned");
  Check(!e.frontEnd.tableSurface.transform.Find("ManaCrystalRow").gameObject.activeSelf,"Flat crystal row returned");
  var stage=GameObject.Find("Original mana crystals");Check(stage.transform.parent==r.boardRoot,"Crystals are not part of the native board");
  Check(stage.GetComponentsInChildren<Camera>(true).Length==0&&stage.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Length==0,"Mana uses an overlay camera or UI view");
  Check(stage.GetComponentsInChildren<Collider>(true).Length==0,"Mana models intercept board input");
  refresh.Invoke(e,null);Check(Models().All(m=>models.Contains(m)),"Refresh duplicates mana models");
  player.CurrentMana=3;refresh.Invoke(e,null);await Task.Delay(650);
  full=0;foreach(var model in models){var block=new MaterialPropertyBlock();model.GetPropertyBlock(block);if(block.GetColor("_tint").a>.99f)full++;}Check(full==3,"Spending failed to reach source used state");
  player.CurrentMana=10;refresh.Invoke(e,null);await Task.Delay(650);
  foreach(var model in models){var block=new MaterialPropertyBlock();model.GetPropertyBlock(block);Check(block.GetColor("_tint").a>.99f,"Refill did not restore source ready state");}
  player.MaxMana=0;player.CurrentMana=0;refresh.Invoke(e,null);Check(models.All(m=>!m.gameObject.activeInHierarchy),"Zero mana leaves visible crystals");
  e.frontEnd.ShowMenu();await Task.Delay(100);Check(Models().Length==0,"Menu leaked crystal stage/texture");
  e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();Check(Models().Count(m=>m.gameObject.activeInHierarchy)==1,"New game retained stale mana");
  return "Original mesh/paint/reflection, seven full/three spent, original board recess/no overlays, stable resources, spend/refill endpoints, zero mana and menu/restart cleanup passed.";
 }
}
