public sealed class SwitchStateArg : Arg
{
    public string StateName { get; }

    public SwitchStateArg(string stateName)
    {
        StateName = stateName;
    }
}
