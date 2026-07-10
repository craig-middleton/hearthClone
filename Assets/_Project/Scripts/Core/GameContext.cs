namespace HearthstoneClone.Core
{
    public class GameContext
    {
        public Board Board;

        public GameContext(Board board)
        {
            Board = board;
        }
    }
}