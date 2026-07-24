namespace HearthstoneClone.Core
{
    public class TurnManager
    {
        public Board Board;
        public Player CurrentPlayer;
        public int TurnNumber = 1;
        private const int MaxManaCap = 10;
        public TurnManager(Board board)
        {
            Board = board;
            CurrentPlayer = board.PlayerOne;
        }
        public void StartGame()
        {
            TurnNumber = 1;
            CurrentPlayer = Board.PlayerOne;
            RefillMana(CurrentPlayer);
        }
        public void EndTurn()
        {
            CurrentPlayer = Board.GetOpponent(CurrentPlayer);
            TurnNumber++;
            RefillMana(CurrentPlayer);
        }
        private void RefillMana(Player player)
        {
            if (player.MaxMana < MaxManaCap)
                player.MaxMana++;
            player.CurrentMana = player.MaxMana;
        }
    }
}
