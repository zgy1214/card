using System;

public sealed class TimerTask
{
    private readonly Action _onTick;
    private float _activeElapsedTime;
    private bool _hasStarted;
    private float _remainingDelayTime;
    private float _nextTickTime;

    public string TimerId { get; }
    public float TickInterval { get; }
    public float StartDelay { get; }
    public float Duration { get; }
    public bool IsImmediateTickOnStart { get; }
    public bool IsRemoved { get; private set; }

    public TimerTask(
        string timerId,
        float tickInterval,
        float startDelay,
        float duration,
        bool isImmediateTickOnStart,
        Action onTick)
    {
        if (string.IsNullOrEmpty(timerId))
        {
            throw new ArgumentException("Timer id cannot be null or empty.", nameof(timerId));
        }

        if (tickInterval <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(tickInterval), tickInterval, "Tick interval must be positive.");
        }

        if (startDelay < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(startDelay), startDelay, "Start delay cannot be negative.");
        }

        if (duration <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be positive.");
        }

        if (onTick == null)
        {
            throw new ArgumentNullException(nameof(onTick));
        }

        TimerId = timerId;
        TickInterval = tickInterval;
        StartDelay = startDelay;
        Duration = duration;
        IsImmediateTickOnStart = isImmediateTickOnStart;
        _onTick = onTick;
        _remainingDelayTime = startDelay;
        _nextTickTime = tickInterval;
    }

    public bool Update(float deltaTime)
    {
        if (IsRemoved)
        {
            return true;
        }

        if (deltaTime < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time cannot be negative.");
        }

        float remainingDeltaTime = deltaTime;
        if (!_hasStarted)
        {
            remainingDeltaTime = TryStart(remainingDeltaTime);
            if (!_hasStarted || IsRemoved)
            {
                return IsRemoved;
            }
        }

        float previousActiveElapsedTime = _activeElapsedTime;
        float newActiveElapsedTime = Math.Min(Duration, _activeElapsedTime + remainingDeltaTime);
        while (!IsRemoved && _nextTickTime <= newActiveElapsedTime)
        {
            if (_nextTickTime > previousActiveElapsedTime)
            {
                _onTick.Invoke();
            }

            if (IsRemoved)
            {
                return true;
            }

            _nextTickTime += TickInterval;
        }

        _activeElapsedTime = newActiveElapsedTime;
        return _activeElapsedTime >= Duration || IsRemoved;
    }

    public void Remove()
    {
        IsRemoved = true;
    }

    private float TryStart(float remainingDeltaTime)
    {
        if (_remainingDelayTime > 0f)
        {
            if (remainingDeltaTime < _remainingDelayTime)
            {
                _remainingDelayTime -= remainingDeltaTime;
                return 0f;
            }

            remainingDeltaTime -= _remainingDelayTime;
            _remainingDelayTime = 0f;
        }

        _hasStarted = true;
        if (IsImmediateTickOnStart)
        {
            _onTick.Invoke();
        }

        return remainingDeltaTime;
    }
}
