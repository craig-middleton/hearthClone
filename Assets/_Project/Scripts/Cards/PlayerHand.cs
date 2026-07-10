using System.Collections.Generic;
using HearthstoneClone.Core;

namespace HearthstoneClone.Cards
{
    public class PlayerHand
    {
        public Player CorePlayer;
        public List<CardData> Deck = new List<CardData>();
        public List<CardData> Hand = new List<CardData>();

        public PlayerHand(Player corePlayer, List<CardData> startingDeck)
        {
            CorePlayer = corePlayer;
            Deck = new List<CardData>(startingDeck);
        }

        public void DrawCard()
        {
            if (Deck.Count == 0)
            {
                UnityEngine.Debug.Log($"{CorePlayer.PlayerName} tried to draw but deck is empty.");
                return;
            }

            CardData drawn = Deck[0];
            Deck.RemoveAt(0);
            Hand.Add(drawn);

            UnityEngine.Debug.Log($"{CorePlayer.PlayerName} drew {drawn.cardName}. Hand size: {Hand.Count}, Deck remaining: {Deck.Count}");
        }
    }
}