using System.Collections.Generic;

namespace HearthstoneClone.Core
{
    public class Board
    {
        public Player PlayerOne;
        public Player PlayerTwo;

        public Board(Player playerOne, Player playerTwo)
        {
            PlayerOne = playerOne;
            PlayerTwo = playerTwo;
        }

        public Player GetOpponent(Player player)
        {
            return player == PlayerOne ? PlayerTwo : PlayerOne;
        }

        /// <summary>
        /// Returns the player whose board holds the given minion.
        /// RETURNS NULL for a minion that is on neither board - a corpse already
        /// swept by RemoveDeadMinions(), a minion still in hand, or a stale
        /// reference held past its lifetime. Callers MUST handle the null case
        /// rather than dereferencing the result; Combat.TryAttack derives the
        /// defending player this way and rejects the attack outright when the
        /// lookup comes back null.
        /// Matching is by reference identity, which is safe here: Minion overrides
        /// neither Equals nor GetHashCode, and every board minion is constructed
        /// exactly once, so a reference identifies one specific minion for its
        /// whole lifetime.
        /// </summary>
        public Player GetOwnerOf(Minion minion)
        {
            if (PlayerOne.BoardMinions.Contains(minion)) return PlayerOne;
            if (PlayerTwo.BoardMinions.Contains(minion)) return PlayerTwo;
            return null;
        }

        public void RemoveDeadMinions()
        {
            PlayerOne.BoardMinions.RemoveAll(m => m.IsDead);
            PlayerTwo.BoardMinions.RemoveAll(m => m.IsDead);
        }

        /// <summary>
        /// Returns the given player's Taunt minions, or an empty list if they have none.
        /// MUST NEVER RETURN NULL. Combat.TryAttack calls .Count on the result directly,
        /// so adding a null-returning guard clause here would silently reintroduce an NRE
        /// at that call site. List.FindAll already returns an empty list when nothing
        /// matches, which is the correct "this player has no Taunt minions" answer.
        /// No separate IsDead filter is needed - corpses are stripped by RemoveDeadMinions().
        /// </summary>
        public List<Minion> GetTauntMinions(Player player)
        {
            return player.BoardMinions.FindAll(m => m.HasTaunt);
        }
    }
}