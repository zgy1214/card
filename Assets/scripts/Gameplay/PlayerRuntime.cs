using System;
using System.Collections.Generic;

public sealed class PlayerRuntime
{
    private readonly List<CardDefinition> _gameHandCardDefinitions = new List<CardDefinition>();
    private int _handCardCount;

    public string PlayerId { get; private set; }
    public string Name { get; private set; }
    public string PlayerType { get; private set; }
    public string CharacterId { get; private set; }
    public bool IsLocalPlayer { get; private set; }
    public int SeatIndex { get; }
    public IReadOnlyList<CardDefinition> GameHandCardDefinitions => _gameHandCardDefinitions;
    public int HandCardCount => _handCardCount;

    public PlayerRuntime(string playerId, int seatIndex, bool isLocalPlayer)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            throw new ArgumentException("Player id cannot be null or empty.", nameof(playerId));
        }

        if (seatIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seatIndex), seatIndex, "Seat index cannot be negative.");
        }

        PlayerId = playerId;
        Name = playerId;
        PlayerType = "human";
        CharacterId = "mengshen";
        SeatIndex = seatIndex;
        IsLocalPlayer = isLocalPlayer;
    }

    public void SetNetworkIdentity(string playerId, string name, string playerType, bool isLocalPlayer)
    {
        SetNetworkIdentity(playerId, name, playerType, isLocalPlayer, CharacterId);
    }

    public void SetNetworkIdentity(string playerId, string name, string playerType, bool isLocalPlayer, string characterId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            throw new ArgumentException("Player id cannot be null or empty.", nameof(playerId));
        }

        PlayerId = playerId;
        Name = string.IsNullOrEmpty(name) ? playerId : name;
        PlayerType = string.IsNullOrEmpty(playerType) ? "human" : playerType;
        IsLocalPlayer = isLocalPlayer;
        CharacterId = string.IsNullOrEmpty(characterId) ? "mengshen" : characterId;
    }

    public void AddHandCard(CardDefinition gameCardDefinition)
    {
        if (gameCardDefinition == null)
        {
            throw new ArgumentNullException(nameof(gameCardDefinition));
        }

        _gameHandCardDefinitions.Add(gameCardDefinition);
        _handCardCount = _gameHandCardDefinitions.Count;
    }

    public void ClearHandCards()
    {
        _gameHandCardDefinitions.Clear();
        _handCardCount = 0;
    }

    public void SetHandState(IEnumerable<CardDefinition> gameHandCardDefinitions, int handCardCount)
    {
        if (handCardCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(handCardCount), handCardCount, "Hand card count cannot be negative.");
        }

        _gameHandCardDefinitions.Clear();
        if (gameHandCardDefinitions != null)
        {
            foreach (CardDefinition gameHandCardDefinition in gameHandCardDefinitions)
            {
                if (gameHandCardDefinition == null)
                {
                    throw new ArgumentException("Hand card definition cannot be null.", nameof(gameHandCardDefinitions));
                }

                _gameHandCardDefinitions.Add(gameHandCardDefinition);
            }
        }

        _handCardCount = handCardCount;
    }

    public bool TryRemoveHandCard(string cardId, out CardDefinition removedCardDefinition)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            throw new ArgumentException("Card id cannot be null or empty.", nameof(cardId));
        }

        for (int index = 0; index < _gameHandCardDefinitions.Count; index += 1)
        {
            CardDefinition gameCardDefinition = _gameHandCardDefinitions[index];
            if (!string.Equals(gameCardDefinition.CardId, cardId, StringComparison.Ordinal))
            {
                continue;
            }

            removedCardDefinition = gameCardDefinition;
            _gameHandCardDefinitions.RemoveAt(index);
            _handCardCount = Math.Max(0, _handCardCount - 1);
            return true;
        }

        removedCardDefinition = null;
        return false;
    }

}
