using System;
using System.Collections.Generic;
using HearthstoneClone.AI;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.Effects;
using HearthstoneClone.UI;
using UnityEngine;

// Isolated rule/view-boundary fixture; does not modify the active scene or match.
public static class VerifyGameManagerLifecycle
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static void Verify(bool simultaneous)
    {
        var one = new Player("Lifecycle One");
        var two = new Player("Lifecycle Two");
        var board = new Board(one, two);
        var context = new GameContext(board);
        var turns = new TurnManager(board);
        turns.StartGame();
        one.CurrentMana = 10;
        var handOne = new PlayerHand(one, new List<CardData>());
        var handTwo = new PlayerHand(two, new List<CardData>());
        var combat = new CombatInputController(board, turns, one, two,
            () => false, () => true, () => true, null, null);
        var attacker = new Minion("Selected lifecycle attacker", 2, 2);
        attacker.ResetForNewTurn();
        one.BoardMinions.Add(attacker);
        combat.OnMinionClicked(attacker, one);
        Check(combat.SelectedAttacker == attacker, "Fixture attacker was not selected");

        int refreshes = 0;
        GameManager manager = null;
        manager = new GameManager(board, turns, one, two, handOne, handTwo,
            new AIController(handTwo, context, board), combat, null, null,
            () => true, () => true,
            () =>
            {
                refreshes++;
                Check(manager.GameOver, "HUD refreshed before the lethal result was committed");
                Check(!manager.CanUseHeroPower(one), "Lethal result still exposes a ready power");
                Check(!one.BoardMinions.Contains(attacker), "HUD refreshed before dead minions were removed");
                Check(combat.SelectedAttacker == null, "HUD refreshed with a dead attacker selected");
            });

        // Player One remains alive/affordable in the win case: power eligibility must
        // become false because the match ended, rather than because its owner died.
        attacker.CurrentHealth = 0;
        two.Health = 0;
        if (simultaneous) one.Health = 0;
        manager.AfterGameAction();
        Check(manager.GameOver && refreshes == 1, "Lethal action did not refresh exactly once");
    }

    static void VerifyAIStopsAtLethal()
    {
        var damage = ScriptableObject.CreateInstance<DealDamageEffect>();
        var lethal = ScriptableObject.CreateInstance<CardData>();
        try
        {
            damage.damageAmount = 1;
            lethal.cardName = "Disposable lethal";
            lethal.cardType = CardType.Spell;
            lethal.targetRequirement = TargetRequirement.Any;
            lethal.onPlayEffect = damage;
            var one = new Player("Lifecycle victim", 1);
            var two = new Player("Lifecycle AI") { MaxMana = 3 };
            var board = new Board(one, two);
            var turns = new TurnManager(board);
            turns.StartGame();
            var handOne = new PlayerHand(one, new List<CardData>());
            var handTwo = new PlayerHand(two, new List<CardData> { lethal });
            var combat = new CombatInputController(board, turns, one, two,
                () => false, () => true, () => false, null, null);
            var manager = new GameManager(board, turns, one, two, handOne, handTwo,
                new AIController(handTwo, new GameContext(board), board), combat, null, null,
                () => true, () => false, null);
            int powers = 0;
            manager.HeroPowerUsed += (_, __, ___) => powers++;
            manager.OnEndTurnClicked();
            Check(manager.GameOver && one.Health <= 0, "AI lethal did not end the match");
            Check(powers == 0 && two.Armor == 0 && !two.HasUsedHeroPowerThisTurn && two.CurrentMana == 4,
                "AI used Armor Up or spent its mana after ending the match");
            Check(turns.CurrentPlayer == two && turns.TurnNumber == 2, "AI lethal started another turn");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lethal);
            UnityEngine.Object.DestroyImmediate(damage);
        }
    }

    public static string Main()
    {
        Verify(false);
        Verify(true);
        VerifyAIStopsAtLethal();
        return "Win/draw committed before HUD refresh; dead minions and selection cleared; powers disabled; AI lethal spends no subsequent Armor Up mana and starts no next turn. Active match untouched.";
    }
}
