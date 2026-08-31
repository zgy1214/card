public sealed class UnregisterTimerArg : Arg
{
    public string TimerId { get; }

    public UnregisterTimerArg(string timerId)
    {
        TimerId = timerId;
    }
}
