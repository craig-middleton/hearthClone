using System;
using HearthstoneClone.Cards;
using UnityEngine;

namespace HearthstoneClone.UI
{
    // Opt-in source study: other cards retain their existing presentation.
    [CreateAssetMenu(menuName="HearthClone/Native card study")]
    public sealed class NativeCardStudy : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public CardData card;
            public NativeCardLibrary library;
            public string tribe;
            public Vector2 tribePosition, tribeSize;
        }
        public Material baselinePaint;
        public Entry[] entries = Array.Empty<Entry>();
        public Entry Find(CardData card) => Array.Find(entries, e => e.card == card);
    }
}
