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

        public void RemoveDeadMinions()
        {
            PlayerOne.BoardMinions.RemoveAll(m => m.IsDead);
            PlayerTwo.BoardMinions.RemoveAll(m => m.IsDead);
        }
    }
}