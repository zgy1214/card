using UnityEngine;

public sealed class OpenViewArg : Arg
{
    public string ViewName { get; }
    public string Layer { get; }
    public GameObject ParentGameObject { get; }
    public string ViewObjectName { get; }
    public Arg ViewArg { get; }

    public OpenViewArg(
        string viewName,
        string layer = null,
        GameObject parentGameObject = null,
        string viewObjectName = null,
        Arg viewArg = null)
    {
        ViewName = viewName;
        Layer = layer;
        ParentGameObject = parentGameObject;
        ViewObjectName = viewObjectName;
        ViewArg = viewArg;
    }
}
