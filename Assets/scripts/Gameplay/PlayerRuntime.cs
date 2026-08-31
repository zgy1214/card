using System;
using System.Collections.Generic;

public sealed class PlayerRuntime
{
    private readonly List<CardDefinition> _gameDeckCardDefinitions = new List<CardDefinition>();
    private readonly List<CardDefinition> _gameHandCardDefinitions = new List<CardDefinition>();
    private int _handCardCount;

    public string PlayerId { get; private set; }
    public string Name { get; private set; }
    public string PlayerType { get; private set; }
    public bool IsLocalPlayer { get; private set; }
    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }
    public int SeatIndex { get; }
    public IReadOnlyList<CardDefinition> GameDeckCardDefinitions => _gameDeckCardDefinitions;
    public IReadOnlyList<CardDefinition> GameHandCardDefinitions => _gameHandCardDefinitions;
    public int HandCardCount => _handCardCount;

    public PlayerRuntime(string playerId, int maxHealth, IEnumerable<CardDefinition> gameDeckCardDefinitions = null)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            throw new ArgumentException("Player id cannot be null or empty.", nameof(playerId));
        }

        if (maxHealth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be positive.");
        }

        PlayerId = playerId;
        Name = playerId;
        PlayerType = "human";
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        SeatIndex = -1;

        if (gameDeckCardDefinitions == null)
        {
            return;
        }

        foreach (CardDefinition gameDeckCardDefinition in gameDeckCardDefinitions)
        {
            if (gameDeckCardDefinition == null)
            {
                throw new ArgumentException("Deck card definition cannot be null.", nameof(gameDeckCardDefinitions));
            }

            _gameDeckCardDefinitions.Add(gameDeckCardDefinition);
        }
    }

    public PlayerRuntime(
        string playerId,
        int seatIndex,
        bool isLocalPlayer,
        int maxHealth,
        IEnumerable<CardDefinition> gameDeckCardDefinitions = null)
        : this(playerId, maxHealth, gameDeckCardDefinitions)
    {
        if (seatIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seatIndex), seatIndex, "Seat index cannot be negative.");
        }

        SeatIndex = seatIndex;
        IsLocalPlayer = isLocalPlayer;
    }

    public void SetNetworkIdentity(string playerId, string name, string playerType, bool isLocalPlayer)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            throw new ArgumentException("Player id cannot be null or empty.", nameof(playerId));
        }

        PlayerId = playerId;
        Name = string.IsNullOrEmpty(name) ? playerId : name;
        PlayerType = string.IsNullOrEmpty(playerType) ? "human" : playerType;
        IsLocalPlayer = isLocalPlayer;
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

    public void TakeDamage(int damage)
    {
        if (damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage cannot be negative.");
        }

        CurrentHealth = Math.Max(0, CurrentHealth - damage);
    }

    public void Heal(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Heal amount cannot be negative.");
        }

        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
    }
}
