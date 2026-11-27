using System;
using System.Collections.Generic;
using HearthstoneClone.AI;
using HearthstoneClone.Cards;
using HearthstoneClone.Core;
using HearthstoneClone.Effects;
using UnityEngine;

// Isolated models only: does not replace the live match or mutate scene assets.
// Freeze primary reference: Blizzard's 1.0.0.4217 patch notes specify the next
// attack phase, rather than a fixed number of start-of-turn resets.
// https://hearthstone.blizzard.com/en-us/news/11868609
public static class VerifyCoreRules
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    sealed class Match
    {
        public readonly Player One = new Player("Rule test One");
        public readonly Player Two = new Player("Rule test Two");
        public readonly Board Board;
        public readonly TurnManager Turns;
        public Match()
        {
            Board = new Board(One, Two);
            Turns = new TurnManager(Board);
            Turns.StartGame();
        }
        public Minion Add(Player owner, string name = "Ready", bool ready = true, bool charge = false, int attack = 2)
        {
            var minion = new Minion(name, attack, 8, false, null, charge);
            if (ready) minion.ResetForNewTurn();
            owner.BoardMinions.Add(minion);
            return minion;
        }
    }

    static void RejectAttack(Minion attacker, Target target, Board board, string reason)
    {
        int? health = attacker?.CurrentHealth;
        bool? attacked = attacker?.HasAttackedThisTurn;
        int? targetHealth = target?.GetCurrentHealth();
        Check(!Combat.TryAttack(attacker, target, board, out _), "Combat accepted " + reason);
        Check(attacker?.CurrentHealth == health && attacker?.HasAttackedThisTurn == attacked &&
            target?.GetCurrentHealth() == targetHealth, "Rejected combat changed state: " + reason);
    }

    static void RejectCard(PlayerHand hand, CardInstance card, GameContext context, Target target, string reason)
    {
        int mana = hand.CorePlayer.CurrentMana;
        var before = new List<CardInstance>(hand.Hand);
        int? targetHealth = target?.GetCurrentHealth();
        int events = 0;
        Action<CardInstance, Target> resolved = (c, t) => events++;
        Action<CardInstance, IReadOnlyList<Minion>> played = (c, affected) => events++;
        hand.CardResolved += resolved; hand.CardPlayed += played;
        try
        {
            Check(!hand.PlayCard(card, context, target), "Card play accepted " + reason);
            Check(hand.CorePlayer.CurrentMana == mana && hand.Hand.Count == before.Count && events == 0 &&
                target?.GetCurrentHealth() == targetHealth, "Rejected card spent resources or resolved: " + reason);
            for (int i = 0; i < before.Count; i++)
                Check(hand.Hand[i] == before[i], "Rejected card changed hand identity: " + reason);
        }
        finally { hand.CardResolved -= resolved; hand.CardPlayed -= played; }
    }

    static CardData NewCard(List<UnityEngine.Object> created, string name, TargetRequirement requirement,
        CardEffect effect = null, CardType type = CardType.Spell)
    {
        var card = ScriptableObject.CreateInstance<CardData>(); created.Add(card);
        card.cardName = name; card.cardType = type; card.manaCost = 2;
        card.targetRequirement = requirement; card.onPlayEffect = effect; card.attack = 2; card.health = 3;
        return card;
    }

    static void TargetBoundaries(List<string> results, List<UnityEngine.Object> created)
    {
        var match = new Match(); var attacker = match.Add(match.One, "Attacker");
        var friendly = match.Add(match.One, "Friendly"); var enemy = match.Add(match.Two, "Enemy");
        var outsider = new Player("Outside match"); var detached = new Minion("Detached", 2, 8); detached.ResetForNewTurn();
        var dead = new Minion("Dead", 2, 0); match.Two.BoardMinions.Add(dead);
        var dual = new Target(match.Two) { TargetMinion = enemy };
        foreach (var target in new Target[] { null, new Target((Player)null), dual, new Target(outsider),
            new Target(detached), new Target(dead), new Target(match.One), new Target(friendly) })
            RejectAttack(attacker, target, match.Board, "invalid/off-board/friendly target");
        RejectAttack(null, new Target(enemy), match.Board, "a null attacker");
        RejectAttack(attacker, new Target(enemy), null, "a null board");
        RejectAttack(detached, new Target(match.Two), match.Board, "an off-board attacker");
        Check(Combat.TryAttack(attacker, new Target(enemy), match.Board, out _) && enemy.CurrentHealth == 6 && attacker.CurrentHealth == 6,
            "Valid in-board minion combat was rejected or lost retaliation");
        attacker = match.Add(match.One, "Hero attacker");
        Check(Combat.TryAttack(attacker, new Target(match.Two), match.Board, out _) && match.Two.Health == 28,
            "Valid enemy hero attack was rejected");
        attacker = match.Add(match.One, "Late attacker"); match.Two.Health = 0;
        RejectAttack(attacker, new Target(enemy), match.Board, "combat after a defending hero died");
        match.Two.Health = 30; match.One.Health = 0;
        RejectAttack(attacker, new Target(enemy), match.Board, "combat after an attacking hero died");
        results.Add("Combat rejects null, empty, dual, outside-match, off-board, friendly and post-lethal attacks without damage or exhaustion; valid combat still retaliates");

        var damage = ScriptableObject.CreateInstance<DealDamageEffect>(); created.Add(damage); damage.damageAmount = 3;
        var any = NewCard(created, "Any target", TargetRequirement.Any, damage);
        match = new Match(); friendly = match.Add(match.One); enemy = match.Add(match.Two);
        match.Two.BoardMinions.Add(dead); match.One.CurrentMana = 10;
        var context = new GameContext(match.Board); var hand = new PlayerHand(match.One, new List<CardData>());
        hand.AddCardToHand(any); var cardInHand = hand.Hand[0];
        dual = new Target(match.Two) { TargetMinion = enemy };
        foreach (var target in new Target[] { null, new Target((Player)null), dual, new Target(outsider), new Target(detached), new Target(dead) })
            RejectCard(hand, cardInHand, context, target, "missing/malformed/outside-match chosen target");
        RejectCard(hand, null, context, new Target(enemy), "a null card");
        RejectCard(hand, new CardInstance(null), context, new Target(enemy), "missing card data");
        RejectCard(hand, cardInHand, null, new Target(enemy), "a null context");
        RejectCard(hand, cardInHand, new GameContext(new Board(new Player("Other One"), new Player("Other Two"))),
            new Target(enemy), "a caster outside this board");
        Check(hand.PlayCard(cardInHand, context, new Target(match.Two)) && match.Two.Health == 27 && hand.Hand.Count == 0 && match.One.CurrentMana == 8,
            "Valid chosen enemy hero target failed");
        hand.AddCardToHand(any);
        Check(hand.PlayCard(hand.Hand[0], context, new Target(match.One)) && match.One.Health == 27,
            "Any incorrectly rejected a friendly hero");
        hand.AddCardToHand(any);
        Check(hand.PlayCard(hand.Hand[0], context, new Target(enemy)) && enemy.CurrentHealth == 5,
            "Any incorrectly rejected a living board minion");
        hand.AddCardToHand(any); match.Two.Health = 0;
        RejectCard(hand, hand.Hand[0], context, new Target(enemy), "a cast after a hero died");
        results.Add("Chosen spells reject malformed/missing/dead/off-board targets and invalid context before mana, hand or events change; legal hero and minion targets work");

        var growth = ScriptableObject.CreateInstance<GrowthEffect>(); created.Add(growth);
        var minionOnly = NewCard(created, "Minion only", TargetRequirement.AnyMinion, growth);
        match = new Match(); friendly = match.Add(match.One); enemy = match.Add(match.Two); match.One.CurrentMana = 10;
        context = new GameContext(match.Board); hand = new PlayerHand(match.One, new List<CardData>()); hand.AddCardToHand(minionOnly);
        RejectCard(hand, hand.Hand[0], context, new Target(match.One), "AnyMinion on a hero");
        Check(hand.PlayCard(hand.Hand[0], context, new Target(friendly)) && friendly.CurrentAttack == 5 && friendly.CurrentHealth == 11,
            "AnyMinion incorrectly rejected a friendly minion");
        hand.AddCardToHand(minionOnly);
        Check(hand.PlayCard(hand.Hand[0], context, new Target(enemy)) && enemy.CurrentAttack == 5,
            "AnyMinion incorrectly rejected an enemy minion");
        var heal = ScriptableObject.CreateInstance<HealEffect>(); created.Add(heal);
        var friendlyOnly = NewCard(created, "Friendly only", TargetRequirement.Friendly, heal);
        hand.AddCardToHand(friendlyOnly);
        RejectCard(hand, hand.Hand[0], context, new Target(match.Two), "Friendly on an enemy hero");
        RejectCard(hand, hand.Hand[0], context, new Target(enemy), "Friendly on an enemy minion");
        friendly.CurrentHealth = 4;
        Check(hand.PlayCard(hand.Hand[0], context, new Target(friendly)) && friendly.CurrentHealth == 10,
            "Friendly minion healing failed");
        hand.AddCardToHand(friendlyOnly); match.One.Health = 24;
        Check(hand.PlayCard(hand.Hand[0], context, new Target(match.One)) && match.One.Health == 30,
            "Friendly hero healing failed");
        results.Add("AnyMinion rejects heroes while accepting either board; Friendly rejects enemies and accepts friendly minions/heroes");

        var mana = ScriptableObject.CreateInstance<GainManaEffect>(); created.Add(mana);
        foreach (var requirement in new[] { TargetRequirement.None, TargetRequirement.Self })
        {
            var self = NewCard(created, "Default caster", requirement, mana);
            match = new Match(); match.One.CurrentMana = 8; context = new GameContext(match.Board);
            hand = new PlayerHand(match.One, new List<CardData>()); hand.AddCardToHand(self);
            RejectCard(hand, hand.Hand[0], context, new Target(match.Two), "None/Self on the opponent");
            Target resolved = null; hand.CardResolved += (c, t) => resolved = t;
            Check(hand.PlayCard(hand.Hand[0], context) && match.One.CurrentMana == 7 && resolved?.TargetPlayer == match.One,
                "Omitted None/Self did not resolve its effect and event against the caster");
        }
        var area = ScriptableObject.CreateInstance<EnemyBoardDamageEffect>(); created.Add(area);
        var sweep = NewCard(created, "Untargeted area", TargetRequirement.Self, area);
        match = new Match(); friendly = match.Add(match.One); enemy = match.Add(match.Two); match.One.CurrentMana = 10;
        context = new GameContext(match.Board); hand = new PlayerHand(match.One, new List<CardData>()); hand.AddCardToHand(sweep);
        Check(hand.PlayCard(hand.Hand[0], context) && enemy.CurrentHealth == 7 && friendly.CurrentHealth == 8,
            "Untargeted enemy area effect changed ownership or required a chosen target");
        match.Two.BoardMinions.Clear(); hand.AddCardToHand(sweep);
        Check(hand.PlayCard(hand.Hand[0], context) && match.One.CurrentMana == 6, "Empty-board untargeted area play was rejected");
        var summon = NewCard(created, "Ordinary minion", TargetRequirement.None, type: CardType.Minion);
        hand.AddCardToHand(summon);
        Check(hand.PlayCard(hand.Hand[0], context) && match.One.BoardMinions.Count == 2 && match.One.CurrentMana == 4,
            "Ordinary minion summon was rejected without an effect target");
        var untargetedMinion = NewCard(created, "Untargeted minion effect", TargetRequirement.None, mana, CardType.Minion);
        hand.AddCardToHand(untargetedMinion);
        Check(hand.PlayCard(hand.Hand[0], context) && match.One.BoardMinions.Count == 3 && match.One.CurrentMana == 3,
            "Untargeted minion effect did not summon and resolve against the default caster");
        results.Add("None/Self default to the caster; untargeted and empty-board area casts, ordinary summons and untargeted minion effects remain legal");
    }

    public static string[] Main()
    {
        var results = new List<string>();
        var match = new Match();
        var minion = match.Add(match.One, attack: 0);
        Check(!minion.CanAttack && !Combat.TryAttack(minion, new Target(match.Two), match.Board, out _),
            "Zero attack was eligible for combat");
        Check(!minion.HasAttackedThisTurn && match.Two.Health == 30, "Rejected zero attack changed state");
        minion.CurrentAttack = 1;
        Check(minion.CanAttack && Combat.TryAttack(minion, new Target(match.Two), match.Board, out _),
            "Gaining positive attack did not allow the otherwise-ready minion to attack");
        Check(!minion.CanAttack && match.Two.Health == 29, "Positive attack bypassed once-per-turn limit");
        results.Add("Zero attack is ineligible; a positive buff enables the unused attack without granting extra attacks");

        match = new Match(); minion = match.Add(match.One); minion.Freeze();
        Check(!minion.CanAttack, "Freeze did not immediately block an unused attack");
        match.Turns.EndTurn();
        Check(!minion.IsFrozen, "Friendly ready freeze did not thaw at this owner turn end");
        match.Turns.EndTurn(); Check(minion.CanAttack, "Friendly ready freeze incorrectly lost the following turn");
        results.Add("Freezing a friendly ready minion consumes the current attack opportunity and thaws at owner turn end");

        match = new Match(); minion = match.Add(match.One);
        Check(Combat.TryAttack(minion, new Target(match.Two), match.Board, out _), "Post-attack fixture could not attack");
        minion.Freeze(); match.Turns.EndTurn();
        Check(minion.IsFrozen, "Already-used attack consumed Freeze too early");
        match.Turns.EndTurn(); Check(minion.IsFrozen && !minion.CanAttack, "Post-attack freeze did not block the next owner turn");
        match.Turns.EndTurn(); Check(!minion.IsFrozen, "Post-attack freeze did not thaw after losing its next attack opportunity");
        results.Add("Freezing after attacking persists through the next owner attack phase, then thaws");

        match = new Match(); minion = match.Add(match.One, ready: false); minion.Freeze();
        match.Turns.EndTurn(); Check(minion.IsFrozen, "Summoning sickness incorrectly consumed Freeze");
        match.Turns.EndTurn(); Check(!minion.HasSummoningSickness && minion.IsFrozen && !minion.CanAttack,
            "Newly summoned frozen minion did not lose its first eligible attack phase");
        match.Turns.EndTurn(); Check(!minion.IsFrozen, "Summoned minion did not thaw at its next owner end");
        match = new Match(); minion = match.Add(match.One, ready: false, charge: true); minion.Freeze();
        match.Turns.EndTurn(); Check(!minion.IsFrozen, "An unused Charge attack did not consume Freeze this turn");
        results.Add("Summoning sickness retains Freeze; unused Charge consumes it on the summon turn");

        match = new Match(); minion = match.Add(match.Two); minion.Freeze();
        match.Turns.EndTurn(); Check(minion.IsFrozen && !minion.CanAttack, "Enemy-turn Freeze disappeared before owner's attack phase");
        minion.Freeze(); match.Turns.EndTurn();
        Check(!minion.IsFrozen, "Re-freezing an already-frozen minion extended its Freeze");
        minion = match.Add(match.One, attack: 0); minion.Freeze();
        match.Turns.EndTurn(); Check(!minion.IsFrozen, "Zero attack incorrectly retained Freeze");
        results.Add("Enemy-turn Freeze expires at that minion's next owner end; repeated Freeze and zero attack do not extend it");

        var created = new List<UnityEngine.Object>();
        try
        {
            TargetBoundaries(results, created);
            var cone = ScriptableObject.CreateInstance<ConeOfColdEffect>(); created.Add(cone);
            match = new Match(); var left = match.Add(match.One, "Left");
            var center = match.Add(match.One, "Center"); var right = match.Add(match.One, "Right");
            Check(Combat.TryAttack(center, new Target(match.Two), match.Board, out _), "Cone fixture attack failed");
            cone.Execute(new GameContext(match.Board), new Target(center), match.One);
            Check(left.IsFrozen && center.IsFrozen && right.IsFrozen, "Cone failed to freeze friendly neighbours");
            match.Turns.EndTurn();
            Check(!left.IsFrozen && center.IsFrozen && !right.IsFrozen, "Cone did not respect each minion's unused/spent attack");
            results.Add("Real Cone effect: ready friendly neighbours thaw now while the center that already attacked stays frozen");

            var damage = ScriptableObject.CreateInstance<DealDamageEffect>(); created.Add(damage); damage.damageAmount = 3;
            var spell = ScriptableObject.CreateInstance<CardData>(); created.Add(spell);
            spell.cardName = "Lethal test spell"; spell.cardType = CardType.Spell; spell.manaCost = 4;
            spell.targetRequirement = TargetRequirement.Any; spell.onPlayEffect = damage;
            var summon = ScriptableObject.CreateInstance<CardData>(); created.Add(summon);
            summon.cardName = "Must remain in hand"; summon.cardType = CardType.Minion; summon.manaCost = 1; summon.attack = 1; summon.health = 1;
            match = new Match(); match.One.Health = 3; match.Two.CurrentMana = 10;
            var hand = new PlayerHand(match.Two, new List<CardData>());
            hand.AddCardToHand(spell); hand.AddCardToHand(summon); hand.AddCardToHand(spell);
            int resolved = 0; hand.CardResolved += (card, target) => resolved++;
            new AIController(hand, new GameContext(match.Board), match.Board).TakeTurn();
            Check(match.One.Health == 0 && match.Two.CurrentMana == 6 && hand.Hand.Count == 2 && resolved == 1 && match.Two.BoardMinions.Count == 0,
                "AI played more cards after lethal");
            new AIController(hand, new GameContext(match.Board), match.Board).TakeTurn();
            Check(hand.Hand.Count == 2 && resolved == 1, "AI began a turn after the game had ended");
            results.Add("AI stops immediately after a lethal spell, retains later cards/mana and rejects a turn with a dead hero");
        }
        finally
        {
            foreach (var item in created) UnityEngine.Object.DestroyImmediate(item);
        }
        return results.ToArray();
    }
}
