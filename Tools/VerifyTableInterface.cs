using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HearthstoneClone.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Input regression checks for the shaped live UI. Run after PreviewTableInterface in Play mode.
public static class VerifyTableInterface
{
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static List<RaycastResult> Hits(Vector2 point)
    {
        var results=new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},results);
        return results;
    }
    static async Task Frame()
    {
        int before=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();
        while(Time.frameCount==before){if(timer.Elapsed.TotalSeconds>10)throw new InvalidOperationException("No Game frame advanced");await Task.Delay(25);}
    }
    public static async Task<string[]> Main()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play mode only; this check spends mana through the real power button.");
        var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();
        await Frame();
        foreach(var face in new[]{e.faceView,e.opponentFaceView})
        {
            Check(Hits(face.avatarImage.transform.position).Any(h=>h.gameObject.GetComponentInParent<FaceView>()==face),"Hero painting no longer targets its FaceView");
            var serialized=new SerializedObject(face);
            var health=(TMP_Text)serialized.FindProperty("healthNumberText").objectReferenceValue;
            Check(Hits(health.transform.position).Any(h=>h.gameObject.GetComponentInParent<FaceView>()==face),"Health number no longer targets the hero behind it");
            Check(health.text==face.Player.Health.ToString(),"Health number no longer reflects the model");
        }
        var backs=e.opponentHandDisplay.handPanel.GetComponentsInChildren<CardView>();
        Check(backs.Length>0&&backs.All(v=>!v.faceUpDetails.activeSelf&&v.nameText.text==""),"Opponent hand leaks card faces");
        Check(Hits(e.heroPowerButton.transform.position).Any(h=>h.gameObject.GetComponent<Button>()==e.heroPowerButton),"New power artwork blocks its button");
        var player=e.faceView.Player;var opponent=e.opponentFaceView.Player;
        Check(player.CurrentMana>=2&&!player.HasUsedHeroPowerThisTurn,"Use the disposable preview with a fresh available power");
        int mana=player.CurrentMana,healthBefore=opponent.Health;
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=e.heroPowerButton.transform.position};
        ExecuteEvents.Execute(e.heroPowerButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        if(e.targetingRibbon!=null)ExecuteEvents.Execute(e.opponentFaceView.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        Check(player.CurrentMana==mana-2&&opponent.Health==healthBefore-1,"Power button changed the existing cost/damage behavior");
        ExecuteEvents.Execute(e.heroPowerButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        Check(player.CurrentMana==mana-2&&opponent.Health==healthBefore-1,"Power could be used twice this turn");
        await Frame();
        var power=e.heroPowerButton.GetComponent<HeroPowerPresentation>();
        Check(power.usedBack.enabled&&!power.artwork.enabled&&!power.readyGlow.activeSelf,"Power does not show its spent back");
        var row=(Transform)new SerializedObject(e.faceView).FindProperty("manaCrystalRow").objectReferenceValue;
        var gems=row.GetComponentsInChildren<Image>();
        Check(gems.Length==player.MaxMana,"Mana strip count differs from MaxMana");
        Check(gems.Count(g=>g.material.name=="LitMana")==player.CurrentMana,"Lit mana count differs from CurrentMana");
        Check(gems.All(g=>g.sprite==gems[0].sprite),"Full and spent mana use different silhouettes");
        return new[]{"Both hero paintings and health numbers remain targetable; health values follow the model", "Opponent cards conceal names/artwork", "Real power pointer click spends 2 mana and deals 1 damage, once only; spent back/halo update", "Ten consistent mana silhouettes correctly show the remaining lit and spent counts"};
    }
}
