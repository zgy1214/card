using System;
using System.Collections.Generic;

public sealed class TurnManager
{
    private readonly IReadOnlyList<PlayerRuntime> _gamePlayerRuntimes;
    private float _tickElapsedTime;

    public int CurrentTurn { get; private set; }
    public int CurrentPlayerIndex { get; private set; }
    public int CurrentTurnRemainingSeconds { get; private set; }
    public bool IsTurnActive { get; private set; }
    public PlayerRuntime CurrentPlayerRuntime
    {
        get
        {
            if (CurrentPlayerIndex < 0 || CurrentPlayerIndex >= _gamePlayerRuntimes.Count)
            {
                return null;
            }

            return _gamePlayerRuntimes[CurrentPlayerIndex];
        }
    }

    public event Action TurnStarted;
    public event Action TurnTicked;
    public event Action TurnEnded;

    public TurnManager(IReadOnlyList<PlayerRuntime> gamePlayerRuntimes)
    {
        if (gamePlayerRuntimes == null)
        {
            throw new ArgumentNullException(nameof(gamePlayerRuntimes));
        }

        if (gamePlayerRuntimes.Count == 0)
        {
            throw new ArgumentException("At least one player runtime is required.", nameof(gamePlayerRuntimes));
        }

        _gamePlayerRuntimes = gamePlayerRuntimes;
    }

    public void Initialize()
    {
        CurrentTurn = 0;
        CurrentPlayerIndex = -1;
        CurrentTurnRemainingSeconds = 0;
        IsTurnActive = false;
        _tickElapsedTime = 0f;
    }

    public void Synchronize(int currentTurn, int currentPlayerIndex)
    {
        if (currentTurn < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentTurn), currentTurn, "Current turn cannot be negative.");
        }

        if (currentPlayerIndex < -1 || currentPlayerIndex >= _gamePlayerRuntimes.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentPlayerIndex),
                currentPlayerIndex,
                "Current player index is out of range.");
        }

        if (IsTurnActive)
        {
            IsTurnActive = false;
            CurrentTurnRemainingSeconds = 0;
            _tickElapsedTime = 0f;
            TurnEnded?.Invoke();
        }

        CurrentTurn = currentTurn;
        CurrentPlayerIndex = currentPlayerIndex;
    }

    public void ApplyTurnStart(int currentTurn, int currentPlayerIndex, int remainingSeconds)
    {
        if (remainingSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingSeconds), remainingSeconds, "Remaining seconds cannot be negative.");
        }

        Synchronize(currentTurn, currentPlayerIndex);
        CurrentTurnRemainingSeconds = remainingSeconds;
        IsTurnActive = true;
        TurnStarted?.Invoke();
    }

    public void Finish()
    {
        if (!IsTurnActive)
        {
            return;
        }

        IsTurnActive = false;
        CurrentTurnRemainingSeconds = 0;
        _tickElapsedTime = 0f;
        TurnEnded?.Invoke();
    }

    public void Tick(float deltaTime)
    {
        if (!IsTurnActive)
        {
            return;
        }

        if (deltaTime < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time cannot be negative.");
        }

        _tickElapsedTime += deltaTime;
        while (IsTurnActive && _tickElapsedTime >= 1f)
        {
            _tickElapsedTime -= 1f;
            if (CurrentTurnRemainingSeconds > 0)
            {
                CurrentTurnRemainingSeconds -= 1;
            }

            TurnTicked?.Invoke();
            if (CurrentTurnRemainingSeconds <= 0)
            {
                IsTurnActive = false;
                TurnEnded?.Invoke();
                break;
            }
        }
    }
}
