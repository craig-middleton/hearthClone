using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HearthstoneClone.Cards;
using HearthstoneClone.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public static class VerifyFrontEnd
{
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static async Task Frame(){int n=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(n==Time.frameCount){if(timer.Elapsed.TotalSeconds>10)throw new Exception("No game frame");await Task.Delay(30);}}
    static void Click(UnityEngine.UI.Button button)=>ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    public static async Task<string[]> Main()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
        const string key="HearthClone.SelectedDeck.v1";bool had=PlayerPrefs.HasKey(key);string previous=PlayerPrefs.GetString(key,"");
        try
        {
            var e=UnityEngine.Object.FindAnyObjectByType<EffectTester>();var ui=e.frontEnd;
            Check(ui.mainMenu.activeInHierarchy&&!ui.tableSurface.activeSelf,"Project must start at its menu");
            Click(ui.chooseDeckButton);ui.clearButton.onClick.Invoke();await Frame();
            Check(ui.deckScreen.activeInHierarchy&&!ui.saveButton.interactable,"Empty deck can be saved");
            var card=ui.collectionGrid.GetComponentsInChildren<DeckCollectionEntry>().First().view.Card.Data;
            for(int n=0;n<2;n++){var view=ui.collectionGrid.GetComponentsInChildren<DeckCollectionEntry>().First(v=>v.view.Card.Data==card);Click(view.view.button);await Frame();}
            Check(ui.Draft.Count==2,"Library pointer clicks did not add copies");ui.AddCard(card);Check(ui.Draft.Count==2,"Third copy accepted");
            var firstRow=ui.selectedList.GetComponentsInChildren<DeckListEntry>().First();Click(firstRow.button);await Frame();Check(ui.Draft.Count==1,"Deck row does not remove one copy");
            ui.clearButton.onClick.Invoke();foreach(var c in e.cardPool.Take(15)){ui.AddCard(c);ui.AddCard(c);}await Frame();
            Check(ui.Draft.Count==30&&ui.saveButton.interactable,"Valid 30-card deck is not saveable");
            ui.AddCard(e.cardPool.Last());Check(ui.Draft.Count==30,"31st card accepted");
            var chosen=ui.Draft.Copy();Click(ui.saveButton);Check(ui.mainMenu.activeInHierarchy&&ui.HasCustomDeck,"Save did not return to menu");
            var privateFields=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(FrontEndController).GetField("savedDeck",privateFields).SetValue(ui,null);
            typeof(FrontEndController).GetMethod("LoadDeck",privateFields).Invoke(ui,null);
            Check(ui.SavedDeck!=null&&ui.SavedDeck.Count==30&&chosen.All(c=>ui.SavedDeck.Count(x=>x==c)==chosen.Count(x=>x==c)),"Saved deck did not reload with its exact composition");
            Check(!e.StartNewGame(chosen.Take(29).ToList()),"Match accepts 29-card custom deck");
            Click(ui.playButton);await Frame();
            Check(!ui.gameObject.activeSelf&&ui.tableSurface.activeSelf&&e.mulliganPanel.gameObject.activeInHierarchy,"Play does not open a fresh mulligan");
            e.confirmMulliganButton.onClick.Invoke();await Frame();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var hand=(PlayerHand)typeof(EffectTester).GetField("playerOneHand",flags).GetValue(e);
            var actual=hand.Deck.Concat(hand.Hand).Select(c=>c.Data).ToArray();
            Check(actual.Length==30&&chosen.All(c=>actual.Count(x=>x==c)==chosen.Count(x=>x==c)),"Chosen composition was not used in the real match");
            Check(hand.Hand.Count==4&&e.faceView.Player.Health==30,"New match did not start normally");
            ui.ShowMenu();Click(ui.playButton);await Frame();Check(e.faceView.Player.Health==30&&e.mulliganPanel.gameObject.activeSelf,"Play from menu reused a stale match");
            return new[]{"Menu opens first; empty/29-card decks rejected", "Real collection clicks add copies; third copies/31st card blocked; deck rows remove one", "Save persists 30 selected IDs; Play uses the exact selected composition and normal opening draws", "Returning to menu then Play creates a new game"};
        }
        finally{if(had)PlayerPrefs.SetString(key,previous);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
    }
}
