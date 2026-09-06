using System;

public sealed class CardDefinition
{
    public string CardId { get; }
    public string CardName { get; }
    public int Rank { get; }

    public CardDefinition(string cardId, string cardName, int rank)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            throw new ArgumentException("Card id cannot be null or empty.", nameof(cardId));
        }

        if (string.IsNullOrEmpty(cardName))
        {
            throw new ArgumentException("Card name cannot be null or empty.", nameof(cardName));
        }

        if (rank < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), rank, "Card rank cannot be negative.");
        }

        CardId = cardId;
        CardName = cardName;
        Rank = rank;
    }
}
