using System;
using System.Collections.Generic;
using System.Linq;
using HearthstoneClone.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HearthstoneClone.UI
{
    public class FrontEndController : MonoBehaviour
    {
        const string SaveKey = "HearthClone.SelectedDeck.v1";
        const int PageSize = 8;
        public GameObject mainMenu, deckScreen, tableSurface;
        public Button playButton, chooseDeckButton, backButton, saveButton, clearButton, previousButton, nextButton, matchMenuButton;
        public Transform collectionGrid, selectedList;
        public DeckCollectionEntry collectionEntryPrefab;
        public DeckListEntry selectedEntryPrefab;
        public TMP_Text deckCount, pageLabel, deckHint, menuDeckLabel;
        public ScrollRect selectedScroll;
        EffectTester game;
        DeckSelection draft;
        List<CardData> savedDeck;
        List<CardData> collection;
        int page;
        bool initialized;
        public bool HasCustomDeck => savedDeck != null;
        public IReadOnlyList<CardData> SavedDeck => savedDeck?.AsReadOnly();
        public DeckSelection Draft => draft;
        [Serializable] class Saved { public string[] cards; }

        public void Initialize(EffectTester owner)
        {
            if(initialized)return;initialized=true;game=owner;
            collection=game.cardPool.Where(c=>c!=null).OrderBy(c=>c.manaCost).ThenBy(c=>c.cardName,StringComparer.Ordinal).ToList();
            draft=new DeckSelection(collection,game.copiesPerCard);
            LoadDeck();
            playButton.onClick.AddListener(Play);
            chooseDeckButton.onClick.AddListener(ChooseDeck);
            backButton.onClick.AddListener(ShowMenu);
            saveButton.onClick.AddListener(()=>SaveDeck());
            clearButton.onClick.AddListener(()=>{draft.Clear();Render();});
            previousButton.onClick.AddListener(()=>{page=Math.Max(0,page-1);RenderLibrary();});
            nextButton.onClick.AddListener(()=>{page=Math.Min((collection.Count-1)/PageSize,page+1);RenderLibrary();});
            matchMenuButton.onClick.AddListener(ShowMenu);
            ShowMenu();
        }
        public void ShowMenu()
        {
            tableSurface.SetActive(false); game.CancelPendingInput();game.ResetPresentation();
            gameObject.SetActive(true);mainMenu.SetActive(true);deckScreen.SetActive(false);
            menuDeckLabel.text=savedDeck==null?"Random deck - 30 cards":"Your chosen deck - 30 cards";
        }
        public void Play()
        {
            tableSurface.SetActive(true);
            if(game.StartNewGame(savedDeck)){gameObject.SetActive(false);}
            else{tableSurface.SetActive(false);ShowMenu();}
        }
        public void ChooseDeck()
        {
            mainMenu.SetActive(false);deckScreen.SetActive(true);page=0;draft.Clear();
            if(savedDeck!=null)foreach(var card in savedDeck)draft.Add(card);
            selectedScroll.verticalNormalizedPosition=1;Render();
        }
        public void AddCard(CardData card){if(draft.Add(card))Render();}
        public void RemoveCard(CardData card){if(draft.Remove(card))Render();}
        public bool SaveDeck()
        {
            if(!DeckSelection.IsValid(draft.Cards,collection,game.copiesPerCard))return false;
            savedDeck=draft.Copy();
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(new Saved{cards=savedDeck.Select(c=>c.name).ToArray()}));PlayerPrefs.Save();
            ShowMenu();return true;
        }
        void LoadDeck()
        {
            try
            {
                if(!PlayerPrefs.HasKey(SaveKey))return;
                var saved=JsonUtility.FromJson<Saved>(PlayerPrefs.GetString(SaveKey));
                if(saved?.cards==null)return;
                var byName=collection.ToDictionary(c=>c.name,StringComparer.Ordinal);
                var loaded=new List<CardData>();
                foreach(var id in saved.cards){if(id==null||!byName.TryGetValue(id,out var card))return;loaded.Add(card);}
                if(DeckSelection.IsValid(loaded,collection,game.copiesPerCard))savedDeck=loaded;
            }
            catch(ArgumentException){savedDeck=null;}
        }
        static void Clear(Transform parent)
        {foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        void Render()
        {
            RenderLibrary();Clear(selectedList);
            foreach(var group in draft.Cards.GroupBy(c=>c).OrderBy(g=>g.Key.manaCost).ThenBy(g=>g.Key.cardName,StringComparer.Ordinal))
            {
                var card=group.Key;var row=Instantiate(selectedEntryPrefab,selectedList);
                row.cardName.text=card.cardName;row.mana.text=card.manaCost.ToString();row.quantity.text="x"+group.Count();row.artwork.sprite=card.artwork;
                row.button.onClick.AddListener(()=>RemoveCard(card));
            }
            deckCount.text=draft.Count+" / 30";saveButton.interactable=draft.Complete;
            deckHint.text=draft.Complete?"Deck complete. Save it to play.":"Click a card to add it. Click a deck row to remove a copy.";
        }
        void RenderLibrary()
        {
            Clear(collectionGrid);
            foreach(var card in collection.Skip(page*PageSize).Take(PageSize))
            {
                var entry=Instantiate(collectionEntryPrefab,collectionGrid);
                entry.view.SetCard(new CardInstance(card),canDragPredicate:()=>false);
                entry.view.button.onClick.AddListener(()=>AddCard(card));
                entry.view.button.interactable=draft.CanAdd(card);
                entry.copies.text=draft.CountOf(card)+" / "+game.copiesPerCard;
            }
            pageLabel.text="Page "+(page+1)+" / "+((collection.Count+PageSize-1)/PageSize);
            previousButton.interactable=page>0;nextButton.interactable=(page+1)*PageSize<collection.Count;
        }
    }
}
