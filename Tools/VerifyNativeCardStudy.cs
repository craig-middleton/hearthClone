using System;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class VerifyNativeCardStudy
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Neutral(Material source)
 {
  var material=new Material(source);var texture=new Texture2D(4,4,TextureFormat.RGBAFloat,false,true);
  var target=RenderTexture.GetTemporary(4,4,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);var before=RenderTexture.active;
  try
  {
   material.SetTexture("_SecondTex",Texture2D.whiteTexture);Graphics.Blit(Texture2D.whiteTexture,target,material);
   RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,4,4),0,0);texture.Apply();var pixel=texture.GetPixel(2,2);
   Check(Mathf.Abs(pixel.r-1)<.01f&&Mathf.Abs(pixel.g-1)<.01f&&Mathf.Abs(pixel.b-1)<.01f,
    "Source half-tint/mod2x darkens neutral white in the active color space");
  }
  finally{RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(material);}
 }
 public static async Task<string> Main()
 {
  var game=UnityEngine.Object.FindAnyObjectByType<EffectTester>();game.frontEnd.ShowMenu();
  var target=game.cardPool.Single(c=>c.cardName=="Ironfur Grizzly");
  var fire=game.cardPool.Single(c=>c.cardName=="Fireball");
  var control=game.cardPool.First(c=>c.cardType==CardType.Minion&&c.cardName!="Ironfur Grizzly");
  var probe=UnityEngine.Object.Instantiate(game.cardViewPrefab,game.frontEnd.mainMenu.transform).GetComponent<CardView>();
  try
  {
   var face=probe.nativeFace;Check(face.study!=null,"Study profile not installed");
   foreach(var card in new[]{target,fire,control,target,fire})
   {
    probe.SetCard(new CardInstance(card));await Task.Delay(50);
    bool selected=card==target||card==fire;
    var expected=selected?face.study.Find(card).library:face.library;
    var layers=probe.GetComponentsInChildren<NativeCardLayer>().Where(p=>p.enabled).ToArray();
    Check(layers.All(p=>p.library==expected),"Source study leaked or failed to restore after rebinding");
    Check(layers.All(p=>!p.raycastTarget&&p.maskable),"Prototype intercepted input or bypassed clipping");
    Check(face.studyTribe.gameObject.activeInHierarchy==(card==target),"Beast label leaked to another card");
    if(card==target)Check(face.studyTribe.text=="Beast"&&face.studyDecorations.All(p=>p.gameObject.activeInHierarchy),"Original Grizzly tribe plate absent");
    if(selected)
    {
     var portrait=layers.Single(p=>p.Definition.role==NativeCardRole.Artwork);
     Check(portrait.Definition.secondaryUV.Length==portrait.Definition.points.Length,"Original second UV channel missing");
     Check(portrait.material.shader.isSupported&&portrait.material.shader.name=="HearthClone/UI/Prototype Original Card Surface","Source material adapter absent");
     if(card==fire)Neutral(portrait.material);
    }
    else Check(layers.Where(p=>p.Definition.role!=NativeCardRole.Name).All(p=>p.material==face.study.baselinePaint),"Other cards changed material");
   }
   probe.SetCard(new CardInstance(target),faceDown:true);await Task.Delay(30);
   Check(!face.minionFace.activeInHierarchy&&!face.studyTribe.gameObject.activeInHierarchy,"Concealed study card exposes its tribe");
   probe.SetCard(new CardInstance(target),canDragPredicate:()=>true);await Task.Delay(50);
   var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=probe.transform.position};
   probe.OnBeginDrag(pointer);await Task.Delay(50);
   var ghosts=probe.GetComponentInParent<Canvas>().rootCanvas.GetComponentsInChildren<CardView>().Where(v=>v.name.EndsWith("(DragGhost)")).ToArray();
   Check(ghosts.Length==1&&ghosts[0].nativeFace.studyTribe.gameObject.activeInHierarchy,"Drag ghost lost source prototype binding");
   probe.OnEndDrag(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=Vector2.zero});
   return "Grizzly/Fireball-only profiles, original UV2/materials/tribe, non-prototype fallback and rebind, clipping/input isolation, concealment, drag ghost and GPU white-neutral source color-space check passed.";
  }
  finally{UnityEngine.Object.Destroy(probe.gameObject);game.frontEnd.ShowMenu();}
 }
}
