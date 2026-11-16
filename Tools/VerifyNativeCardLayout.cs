using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.UI;
using UnityEngine;

public static class VerifyNativeCardLayout
{
    public static async Task<string> Main()
    {
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        e.frontEnd.Play();e.confirmMulliganButton.onClick.Invoke();
        bool manual=e.manualControlMode;var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        try
        {
            e.manualControlMode=true;
            foreach(string field in new[]{"playerOneHand","playerTwoHand"})
            {
                var hand=(PlayerHand)typeof(EffectTester).GetField(field,flags).GetValue(e);hand.Hand.Clear();
                foreach(var card in e.cardPool.Take(10))hand.AddCardToHand(card);
            }
            typeof(EffectTester).GetMethod("RefreshAll",flags).Invoke(e,null);
            await Task.Delay(100);
            foreach(var display in new[]{e.handDisplay,e.opponentHandDisplay})
            {
                var fan=display.handPanel.GetComponent<HandFanLayout>();
                var views=display.handPanel.GetComponentsInChildren<CardView>().ToArray();
                foreach(int index in new[]{0,views.Length/2,views.Length-1})
                {
                    fan.SetHovered(views[index]);await Task.Delay(450);
                    var corners=new Vector3[4];((RectTransform)views[index].transform).GetWorldCorners(corners);
                    if(corners.Min(p=>p.y)<2||corners.Max(p=>p.y)>Screen.height-2)
                        throw new Exception("Hovered card crosses screen edge: "+display.name+" index="+index+" range="+corners.Min(p=>p.y)+".."+corners.Max(p=>p.y)+" screen="+Screen.height);
                }
            }
            var sample=e.handDisplay.handPanel.GetComponentsInChildren<CardView>().First(v=>v.Card.Data.cardType==CardType.Spell);
            var face=sample.nativeFace;
            var body=face.labels.First(l=>l.role==NativeCardRole.Description&&l.text.gameObject.activeInHierarchy).text.rectTransform;
            var school=face.labels.First(l=>l.role==NativeCardRole.School&&l.text.gameObject.activeInHierarchy).text.rectTransform;
            // Compare authored unrotated card coordinates, independently of current fan pose.
            if(body.anchoredPosition.y-body.rect.height*.5f <= school.anchoredPosition.y+school.rect.height*.5f)
                throw new Exception("Spell description overlaps its school label");
            return "Ten-card hands: first/middle/last hovered rectangles remain inside top/bottom screen edges for both players; spell description and school boxes are separated.";
        }
        finally{e.manualControlMode=manual;e.frontEnd.ShowMenu();}
    }
}
