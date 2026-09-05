namespace HearthstoneClone.Cards
{
    // Root fix for the CardData reference-identity problem (see PROJECT_STATUS's
    // "CardData reference-identity" entry): CardData is a ScriptableObject asset, so two
    // "copies" of the same card in a deck are literally the same reference and can't be told
    // apart by identity. Hand/Deck now store CardInstance instead of raw CardData - each
    // instance is its own object, so reference equality (==, Contains, Remove,
    // HashSet<CardInstance>) correctly distinguishes duplicate copies even though they wrap
    // the same CardData asset. No explicit id is needed: two different CardInstance objects
    // are never == to each other unless they're literally the same instance.
    public class CardInstance
    {
        public CardData Data;

        public CardInstance(CardData data)
        {
            Data = data;
        }
    }
}
