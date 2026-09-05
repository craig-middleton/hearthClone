using System.Collections.Generic;
using HearthstoneClone.Core;

namespace HearthstoneClone.Cards
{
    public class PlayerHand
    {
        public Player CorePlayer;
        public List<CardInstance> Deck = new List<CardInstance>();
        public List<CardInstance> Hand = new List<CardInstance>();

        private const int MaxHandSize = 10;
        private const int MaxBoardSize = 7;

        // startingDeck may contain the same CardData reference multiple times (BuildDeck adds
        // a card copiesPerCard times) - wrapping each entry in its own CardInstance here is
        // the root fix's boundary: everything past this constructor deals in CardInstance,
        // so two "copies" of a duplicate card are two distinct objects from this point on.
        public PlayerHand(Player corePlayer, List<CardData> startingDeck)
        {
            CorePlayer = corePlayer;
            Deck = new List<CardInstance>();
            foreach (CardData card in startingDeck)
            {
                Deck.Add(new CardInstance(card));
            }
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

            CardInstance drawn = Deck[0];
            Deck.RemoveAt(0);

            if (Hand.Count >= MaxHandSize)
            {
                UnityEngine.Debug.Log($"{CorePlayer.PlayerName}'s hand is full — {drawn.Data.cardName} was burned.");
                return;
            }

            Hand.Add(drawn);
            UnityEngine.Debug.Log($"{CorePlayer.PlayerName} drew {drawn.Data.cardName}. Hand size: {Hand.Count}, Deck remaining: {Deck.Count}");
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

            Hand.Add(new CardInstance(card));
            UnityEngine.Debug.Log($"{CorePlayer.PlayerName} gained {card.cardName}. Hand size: {Hand.Count}");
        }

        public void MulliganCards(List<CardInstance> cards)
        {
            var setAside = new List<CardInstance>();

            foreach (CardInstance card in cards)
            {
                if (!Hand.Contains(card))
                {
                    UnityEngine.Debug.LogWarning($"{CorePlayer.PlayerName} tried to mulligan a card not in hand: {card.Data.cardName}");
                    continue;
                }

                Hand.Remove(card);
                setAside.Add(card);
            }

            for (int i = 0; i < setAside.Count; i++)
            {
                DrawCard();
            }

            Deck.AddRange(setAside);
            Shuffle();

            UnityEngine.Debug.Log($"{CorePlayer.PlayerName} mulliganed {setAside.Count} card(s).");
        }

        public bool PlayCard(CardInstance card, GameContext context, Target effectTarget = null)
        {
            if (!Hand.Contains(card))
            {
                UnityEngine.Debug.LogWarning($"{CorePlayer.PlayerName} tried to play a card not in hand: {card.Data.cardName}");
                return false;
            }

            if (CorePlayer.CurrentMana < card.Data.manaCost)
            {
                UnityEngine.Debug.Log($"{CorePlayer.PlayerName} cannot play {card.Data.cardName} - not enough mana ({CorePlayer.CurrentMana}/{card.Data.manaCost}).");
                return false;
            }

            if (card.Data.cardType == CardType.Minion && CorePlayer.BoardMinions.Count >= MaxBoardSize)
            {
                UnityEngine.Debug.Log($"{CorePlayer.PlayerName} cannot play {card.Data.cardName} - board is full ({MaxBoardSize}/{MaxBoardSize}).");
                return false;
            }

            CorePlayer.CurrentMana -= card.Data.manaCost;
            Hand.Remove(card);

            UnityEngine.Debug.Log($"{CorePlayer.PlayerName} played {card.Data.cardName}. Mana remaining: {CorePlayer.CurrentMana}");

            if (card.Data.cardType == CardType.Minion)
            {
                var minion = new Minion(card.Data.cardName, card.Data.attack, card.Data.health, card.Data.hasTaunt, card.Data.artwork);
                CorePlayer.BoardMinions.Add(minion);
                UnityEngine.Debug.Log($"{minion.MinionName} summoned to {CorePlayer.PlayerName}'s board.");
            }

            if (card.Data.onPlayEffect != null && effectTarget != null)
            {
                card.Data.onPlayEffect.Execute(context, effectTarget, CorePlayer);
            }
            else if (card.Data.onPlayEffect != null)
            {
                UnityEngine.Debug.LogWarning($"{card.Data.cardName} has an onPlayEffect but no target was provided — effect was skipped.");
            }

            return true;
        }
    }
}
