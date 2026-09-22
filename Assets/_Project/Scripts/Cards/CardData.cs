using UnityEngine;
using HearthstoneClone.Core;
using HearthstoneClone.Effects;

namespace HearthstoneClone.Cards
{
    public enum CardType
    {
        Minion,
        Spell
    }

    // Controls how a dropped card resolves in CardDragResolver.ResolveCardDrag: None/Self play
    // immediately on any recognized drop zone, with the caster as the effect's Target (see
    // CardTargeting below). Any requires dropping on a MinionView/FaceView.
    // AnyMinion requires dropping on a MinionView specifically (either side) - a FaceView drop
    // is invalid, for effects (e.g. GrowthEffect) with no sensible behavior when cast on a face.
    // Friendly requires a friendly-side MinionView or the caster's own FaceView - an enemy
    // minion or the opponent's face is an invalid drop. This is a UX nicety only (e.g.
    // HealEffect); the actual "never heal the opponent" rule is enforced in HealEffect.Execute
    // itself, since AIController bypasses this drag-resolver path entirely.
    public enum TargetRequirement
    {
        None,
        Self,
        Any,
        AnyMinion,
        Friendly
    }

    // The single source of truth for "which Target does an effect get when the player never
    // chooses one" - shared by CardDragResolver (human) and AIController (AI) so the two paths
    // can't drift apart again. Before this existed, a None card got a null Target from the
    // resolver (PlayCard then spent the card and skipped the effect) but Target(opponent) from
    // the AI.
    //
    // The default is the caster: an effect that acts on someone else (e.g. FreezeAllEffect)
    // derives that from the caster itself via context.Board.GetOpponent(caster), and an effect
    // that ignores its Target (draw a card, most battlecries) is unaffected either way. Applies
    // to minion onPlayEffects too, whose requirement is None unless an asset says otherwise.
    public static class CardTargeting
    {
        // Any/AnyMinion/Friendly need a target the player (or AI) picks - there is no sensible
        // default for them, so DefaultTarget returns null and the caller must supply one.
        public static bool RequiresChosenTarget(CardData card)
        {
            return card.targetRequirement == TargetRequirement.Any
                || card.targetRequirement == TargetRequirement.AnyMinion
                || card.targetRequirement == TargetRequirement.Friendly;
        }

        public static Target DefaultTarget(CardData card, Player caster)
        {
            return RequiresChosenTarget(card) ? null : new Target(caster);
        }
    }

    // Which spell-VFX burst SpellAnimationSequencer plays on impact (SpellBurstFactory).
    // Property of the card's flavor/theme, not of its CardEffect - two different damage
    // spells can share DealDamageEffect and differ only in spellSchool. None covers non-spell
    // cards and spells with no visual school (e.g. GainManaEffect-based ones).
    public enum SpellSchool
    {
        None,
        Fire,
        Frost,
        Arcane,
        Nature
    }

    // Which animation shape CardDragResolver.TriggerSpellAnimation dispatches to.
    // SingleTarget is the existing point-travel + point-burst path (PlayTravelAndReaction) -
    // every card before Blizzard uses this, and it's the default so no existing asset needs
    // re-serializing (Constraint 12). BoardSweep is for effects with no single target to
    // travel to (e.g. FreezeAllEffect) - dispatches to PlayBoardSweep against the caster's
    // opponent's board region instead. A CardData field rather than checking the effect's
    // type, matching the existing targetRequirement/spellSchool precedent of keying UI
    // behavior off declarative data instead of the Effects-layer class.
    public enum SpellVisualShape
    {
        SingleTarget,
        BoardSweep
    }

    [CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public string cardName;
        [TextArea] public string description;
        public Sprite artwork;

        [Header("Stats")]
        public int manaCost;
        public CardType cardType;

        [Header("Minion Stats (ignored for Spells)")]
        public int attack;
        public int health;
        public bool hasTaunt;

        [Header("Behavior")]
        public CardEffect onPlayEffect;
        public TargetRequirement targetRequirement;

        [Header("Spell Visuals")]
        public SpellSchool spellSchool;
        public SpellVisualShape visualShape;
    }
}