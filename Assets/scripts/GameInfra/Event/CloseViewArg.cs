using UnityEngine;

public sealed class CloseViewArg : Arg
{
    public string ViewName { get; }
    public GameObject ViewGameObject { get; }

    public CloseViewArg(string viewName)
    {
        ViewName = viewName;
    }

    public CloseViewArg(GameObject viewGameObject)
    {
        ViewGameObject = viewGameObject;
    }
}
