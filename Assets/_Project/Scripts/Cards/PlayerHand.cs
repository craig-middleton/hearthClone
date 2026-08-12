using System.Collections.Generic;
using HearthstoneClone.Core;

namespace HearthstoneClone.Cards
{
    public class PlayerHand
    {
        public Player CorePlayer;
        public List<CardData> Deck = new List<CardData>();
        public List<CardData> Hand = new List<CardData>();

        private const int MaxHandSize = 10;
        private const int MaxBoardSize = 7;

        public PlayerHand(Player corePlayer, List<CardData> startingDeck)
        {
            CorePlayer = corePlayer;
            Deck = new List<CardData>(startingDeck);
        }

        public void DrawCard()
        {
            if (Deck.Count == 0)
            {
                CorePlayer.FatigueDamage++;
                CorePlayer.TakeDamage(CorePlayer.FatigueDamage);
                UnityEngine.Debug.Log($"{CorePlayer.PlayerName} is out of cards! Fatigue damage: {CorePlayer.FatigueDamage}. Health remaining: {CorePlayer.Health}");
                return;
            }

            CardData drawn = Deck[0];
            Deck.RemoveAt(0);

            if (Hand.Count >= MaxHandSize)
            {
                UnityEngine.Debug.Log($"{CorePlayer.PlayerName}'s hand is full — {drawn.cardName} was burned.");
                return;
            }

            Hand.Add(drawn);
            UnityEngine.Debug.Log($"{CorePlayer.PlayerName} drew {drawn.cardName}. Hand size: {Hand.Count}, Deck remaining: {Deck.Count}");
        }

        public void Shuffle()
        {
            for (int i = Deck.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (Deck[i], Deck[j]) = (Deck[j], Deck[i]);
            }
        }

        public void DrawOpeningHand(int count = 5)
        {
            for (int i = 0; i < count; i++)
            {
                DrawCard();
            }
        }

        public void AddCardToHand(CardData card)
        {
            if (Hand.Count >= MaxHandSize)
            {
                UnityEngine.Debug.Log($"{CorePlayer.PlayerName}'s hand is full — {card.cardName} could not be added.");
                return;
            }

            Hand.Add(card);
            UnityEngine.Debug.Log($"{CorePlayer.PlayerName} gained {card.cardName}. Hand size: {Hand.Count}");
        }

        public void MulliganCard(CardData card)
        {
            if (!Hand.Contains(card))
            {
                UnityEngine.Debug.LogWarning($"{CorePlayer.PlayerName} tried to mulligan a card not in hand: {card.cardName}");
                return;
            }

            Hand.Remove(card);
            Deck.Add(card);
            Shuffle();
            DrawCard();

            UnityEngine.Debug.Log($"{CorePlayer.PlayerName} mulliganed {card.cardName}.");
        }

        public bool PlayCard(CardData card, GameContext context, Target effectTarget = null)
        {
            if (!Hand.Contains(card))
            {
                UnityEngine.Debug.LogWarning($"{CorePlayer.PlayerName} tried to play a card not in hand: {card.cardName}");
                return false;
            }

            if (CorePlayer.CurrentMana < card.manaCost)
            {
                UnityEngine.Debug.Log($"{CorePlayer.PlayerName} cannot play {card.cardName} - not enough mana ({CorePlayer.CurrentMana}/{card.manaCost}).");
                return false;
            }

            if (card.cardType == CardType.Minion && CorePlayer.BoardMinions.Count >= MaxBoardSize)
            {
                UnityEngine.Debug.Log($"{CorePlayer.PlayerName} cannot play {card.cardName} - board is full ({MaxBoardSize}/{MaxBoardSize}).");
                return false;
            }

            CorePlayer.CurrentMana -= card.manaCost;
            Hand.Remove(card);

            UnityEngine.Debug.Log($"{CorePlayer.PlayerName} played {card.cardName}. Mana remaining: {CorePlayer.CurrentMana}");

            if (card.cardType == CardType.Minion)
            {
                var minion = new Minion(card.cardName, card.attack, card.health, card.hasTaunt);
                CorePlayer.BoardMinions.Add(minion);
                UnityEngine.Debug.Log($"{minion.MinionName} summoned to {CorePlayer.PlayerName}'s board.");
            }

            if (card.onPlayEffect != null && effectTarget != null)
            {
                card.onPlayEffect.Execute(context, effectTarget);
            }

            return true;
        }
    }
}