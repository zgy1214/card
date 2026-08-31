using System;

public sealed class CardDefinition
{
    public string CardId { get; }
    public string CardName { get; }
    public int ManaCost { get; }

    public CardDefinition(string cardId, string cardName, int manaCost)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            throw new ArgumentException("Card id cannot be null or empty.", nameof(cardId));
        }

        if (string.IsNullOrEmpty(cardName))
        {
            throw new ArgumentException("Card name cannot be null or empty.", nameof(cardName));
        }

        if (manaCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(manaCost), manaCost, "Mana cost cannot be negative.");
        }

        CardId = cardId;
        CardName = cardName;
        ManaCost = manaCost;
    }
}
