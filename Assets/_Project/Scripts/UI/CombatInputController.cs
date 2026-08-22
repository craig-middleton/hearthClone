using System;
using UnityEngine;
using HearthstoneClone.Core;

namespace HearthstoneClone.UI
{
    // Owns click-to-attack input: minion/face click handling, attacker selection, and
    // resolving an attack through Combat.TryAttack. Constructed once in EffectTester.Start()
    // and lives for the whole match, unlike MulliganController (which finishes after one use).
    //
    // The own-side attack rejection path (Combat.TryAttack's "Cannot attack your own side of
    // the board" branch) remains unreachable from the UI here, same as before this class
    // existed - own-minion clicks route to selection (ownerIsActingPlayer) and own-face clicks
    // are blocked by the OnFaceClicked owner check below. This is a known, accepted gap
    // (see PROJECT_STATUS Verification Status) that extraction deliberately did not fix -
    // moving the code to a new class doesn't change what's reachable from the UI either way.
    public class CombatInputController
    {
        private readonly Board board;
        private readonly TurnManager turnManager;
        private readonly Player playerOne;
        private readonly Player playerTwo;
        private readonly Func<bool> isGameOver;
        private readonly Func<bool> isMulliganComplete;
        private readonly Func<bool> isManualControlMode;
        private readonly Action onSelectionChanged;
        private readonly Action onAfterAction;

        private Minion selectedAttacker;

        public Minion SelectedAttacker => selectedAttacker;

        public CombatInputController(Board board, TurnManager turnManager, Player playerOne, Player playerTwo, Func<bool> isGameOver, Func<bool> isMulliganComplete, Func<bool> isManualControlMode, Action onSelectionChanged, Action onAfterAction)
        {
            this.board = board;
            this.turnManager = turnManager;
            this.playerOne = playerOne;
            this.playerTwo = playerTwo;
            this.isGameOver = isGameOver;
            this.isMulliganComplete = isMulliganComplete;
            this.isManualControlMode = isManualControlMode;
            this.onSelectionChanged = onSelectionChanged;
            this.onAfterAction = onAfterAction;
        }

        public void OnMinionClicked(Minion minion, Player owner)
        {
            if (isGameOver()) return;
            if (!isMulliganComplete()) return;
            if (minion == null) return;
            if (!isManualControlMode() && turnManager.CurrentPlayer == playerTwo) return;

            bool ownerIsActingPlayer = owner == turnManager.CurrentPlayer && (owner == playerOne || isManualControlMode());

            if (ownerIsActingPlayer)
            {
                if (selectedAttacker == minion)
                {
                    selectedAttacker = null;
                }
                else if (minion.CanAttack)
                {
                    selectedAttacker = minion;
                }
                onSelectionChanged?.Invoke();
                return;
            }

            if (selectedAttacker == null) return;

            ResolveAttack(selectedAttacker, new Target(minion));
        }

        public void OnFaceClicked(Player owner)
        {
            if (isGameOver()) return;
            if (!isMulliganComplete()) return;
            if (!isManualControlMode() && turnManager.CurrentPlayer == playerTwo) return;

            if (selectedAttacker == null) return;
            if (owner == turnManager.CurrentPlayer) return;

            ResolveAttack(selectedAttacker, new Target(owner));
        }

        private void ResolveAttack(Minion attacker, Target target)
        {
            bool success = Combat.TryAttack(attacker, target, board, out string failReason);
            if (success)
            {
                Debug.Log($"{attacker.MinionName} attacked.");
                selectedAttacker = null;
            }
            else
            {
                Debug.LogWarning(failReason);
            }

            onAfterAction?.Invoke();
        }

        // Called from GameManager.AfterGameAction, after RemoveDeadMinions() and before
        // RefreshAll() - matches Constraint 6's ordering exactly, just delegating the
        // dead-check to whoever now owns selectedAttacker. A rejected attack keeps the
        // attacker selected so it can be retargeted, but a minion that just died must not
        // stay selected - it is no longer rendered, so there would be no way to deselect it.
        public void ClearSelectionIfDead()
        {
            if (selectedAttacker != null && selectedAttacker.IsDead)
            {
                selectedAttacker = null;
            }
        }

        // Called from GameManager.OnEndTurnClicked - an unconditional reset (not the
        // dead-check above), since attacker selection doesn't carry across a turn boundary.
        public void ClearSelection()
        {
            selectedAttacker = null;
        }
    }
}
