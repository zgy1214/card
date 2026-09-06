public sealed class CardViewArg : Arg
{
    public string CardName { get; }
    public int Rank { get; }

    public CardViewArg(string cardName, int rank)
    {
        if (string.IsNullOrEmpty(cardName))
        {
            throw new System.ArgumentException("Card name cannot be null or empty.", nameof(cardName));
        }

        CardName = cardName;
        Rank = rank;
    }
}
