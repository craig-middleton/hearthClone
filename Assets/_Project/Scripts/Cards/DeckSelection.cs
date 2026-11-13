using System.Collections.Generic;
using System.Linq;

namespace HearthstoneClone.Cards
{
    // One validation boundary for the deck editor, saved decks and match initialization.
    public sealed class DeckSelection
    {
        public const int DeckSize = 30;
        readonly HashSet<CardData> allowed;
        readonly int copyLimit;
        readonly List<CardData> cards = new List<CardData>();
        public IReadOnlyList<CardData> Cards => cards.AsReadOnly();
        public int Count => cards.Count;
        public bool Complete => Count == DeckSize;
        public DeckSelection(IEnumerable<CardData> pool, int copies)
        { allowed = new HashSet<CardData>(pool.Where(c=>c!=null)); copyLimit = copies; }
        public int CountOf(CardData card) => cards.Count(c=>c==card);
        public bool CanAdd(CardData card) => card!=null && allowed.Contains(card) && Count<DeckSize && CountOf(card)<copyLimit;
        public bool Add(CardData card) { if(!CanAdd(card))return false;cards.Add(card);return true; }
        public bool Remove(CardData card) => cards.Remove(card);
        public void Clear() => cards.Clear();
        public List<CardData> Copy() => new List<CardData>(cards);
        public static bool IsValid(IReadOnlyList<CardData> deck, IEnumerable<CardData> pool, int copies)
        {
            if(deck==null || deck.Count!=DeckSize)return false;
            var allowed = new HashSet<CardData>(pool.Where(c=>c!=null));
            return deck.All(c=>c!=null&&allowed.Contains(c)) && deck.GroupBy(c=>c).All(g=>g.Count()<=copies);
        }
    }
}
