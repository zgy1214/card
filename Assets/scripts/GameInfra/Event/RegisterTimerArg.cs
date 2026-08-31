using System;

public sealed class RegisterTimerArg : Arg
{
    public string TimerId { get; }
    public float TickInterval { get; }
    public float StartDelay { get; }
    public float Duration { get; }
    public bool IsImmediateTickOnStart { get; }
    public Action OnTick { get; }

    public RegisterTimerArg(
        string timerId,
        float tickInterval,
        float startDelay,
        float duration,
        Action onTick,
        bool isImmediateTickOnStart = true)
    {
        TimerId = timerId;
        TickInterval = tickInterval;
        StartDelay = startDelay;
        Duration = duration;
        OnTick = onTick;
        IsImmediateTickOnStart = isImmediateTickOnStart;
    }
}
