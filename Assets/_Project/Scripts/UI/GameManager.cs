using System;
using UnityEngine;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.AI;

namespace HearthstoneClone.UI
{
    // Owns turn order, win condition, and Hero Power - the last piece of the EffectTester
    // decomposition (session 33). AfterGameAction is the shared "something happened, refresh
    // and check win" hook every other controller (CombatInputController, CardDragResolver)
    // calls back into after resolving their own action.
    public class GameManager
    {
        private const int HeroPowerCost = 2;

        private readonly Board board;
        private readonly TurnManager turnManager;
        private readonly Player playerOne;
        private readonly Player playerTwo;
        private readonly PlayerHand playerOneHand;
        private readonly PlayerHand playerTwoHand;
        private readonly AIController aiController;
        private readonly CombatInputController combatInputController;
        private readonly TMPro.TMP_Text gameOverText;
        private readonly UnityEngine.UI.Button playAgainButton;
        private readonly Func<bool> isMulliganComplete;
        private readonly Func<bool> isManualControlMode;
        private readonly Action onRefreshAll;

        private Player winner = null;

        public bool GameOver { get; private set; } = false;

        public GameManager(
            Board board, TurnManager turnManager, Player playerOne, Player playerTwo,
            PlayerHand playerOneHand, PlayerHand playerTwoHand,
            AIController aiController, CombatInputController combatInputController,
            TMPro.TMP_Text gameOverText, UnityEngine.UI.Button playAgainButton,
            Func<bool> isMulliganComplete, Func<bool> isManualControlMode,
            Action onRefreshAll)
        {
            this.board = board;
            this.turnManager = turnManager;
            this.playerOne = playerOne;
            this.playerTwo = playerTwo;
            this.playerOneHand = playerOneHand;
            this.playerTwoHand = playerTwoHand;
            this.aiController = aiController;
            this.combatInputController = combatInputController;
            this.gameOverText = gameOverText;
            this.playAgainButton = playAgainButton;
            this.isMulliganComplete = isMulliganComplete;
            this.isManualControlMode = isManualControlMode;
            this.onRefreshAll = onRefreshAll;
        }

        public void OnHeroPowerClicked()
        {
            if (GameOver) return;
            if (!isMulliganComplete()) return;
            if (!isManualControlMode() && turnManager.CurrentPlayer == playerTwo) return;

            // Everything below resolves through whoever is actually taking the turn,
            // so the single Hero Power button works for Player Two under manual
            // control mode instead of silently charging Player One.
            Player actingPlayer = turnManager.CurrentPlayer;
            Player opposingPlayer = board.GetOpponent(actingPlayer);

            if (actingPlayer.HasUsedHeroPowerThisTurn) return;
            if (actingPlayer.CurrentMana < HeroPowerCost) return;

            actingPlayer.CurrentMana -= HeroPowerCost;
            actingPlayer.HasUsedHeroPowerThisTurn = true;
            opposingPlayer.TakeDamage(1);

            Debug.Log($"{actingPlayer.PlayerName} used Hero Power. Dealt 1 damage to {opposingPlayer.PlayerName}.");

            AfterGameAction();
        }

        public void AfterGameAction()
        {
            board.RemoveDeadMinions();
            combatInputController.ClearSelectionIfDead();

            onRefreshAll?.Invoke();
            CheckWinCondition();
        }

        public void OnEndTurnClicked()
        {
            if (GameOver) return;
            if (!isMulliganComplete()) return;

            combatInputController.ClearSelection();
            turnManager.EndTurn();
            DrawForCurrentPlayer();
            Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");
            CheckWinCondition();

            if (!GameOver && !isManualControlMode() && turnManager.CurrentPlayer == playerTwo)
            {
                aiController.TakeTurn();
                CheckWinCondition();

                if (!GameOver)
                {
                    turnManager.EndTurn();
                    DrawForCurrentPlayer();
                    Debug.Log($"Turn {turnManager.TurnNumber}: {turnManager.CurrentPlayer.PlayerName}'s turn. Mana: {turnManager.CurrentPlayer.CurrentMana}/{turnManager.CurrentPlayer.MaxMana}");
                    CheckWinCondition();
                }
            }

            AfterGameAction();
        }

        private void DrawForCurrentPlayer()
        {
            if (turnManager.CurrentPlayer == playerOne)
            {
                playerOneHand.DrawCard();
            }
            else
            {
                playerTwoHand.DrawCard();
            }
        }

        private void CheckWinCondition()
        {
            if (GameOver) return;

            bool p1Dead = playerOne.Health <= 0;
            bool p2Dead = playerTwo.Health <= 0;

            // Both evaluated before branching, so a simultaneous double-KO (e.g. mutual lethal
            // combat damage resolved in the same AfterGameAction) is caught as a draw instead
            // of the old first-checked-wins bias always awarding Player Two.
            if (p1Dead && p2Dead)
            {
                GameOver = true;
                Debug.Log("*** Draw! ***");

                if (gameOverText != null)
                {
                    gameOverText.gameObject.SetActive(true);
                    gameOverText.text = "Draw!";
                }

                if (playAgainButton != null)
                {
                    playAgainButton.gameObject.SetActive(true);
                }
                return;
            }

            if (p1Dead)
            {
                winner = playerTwo;
            }
            else if (p2Dead)
            {
                winner = playerOne;
            }

            if (winner != null)
            {
                GameOver = true;
                Debug.Log($"*** {winner.PlayerName} wins! ***");

                if (gameOverText != null)
                {
                    gameOverText.gameObject.SetActive(true);
                    gameOverText.text = $"{winner.PlayerName} wins!";
                }

                if (playAgainButton != null)
                {
                    playAgainButton.gameObject.SetActive(true);
                }
            }
        }
    }
}
