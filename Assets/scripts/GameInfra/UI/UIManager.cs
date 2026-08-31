using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed class UIManager
{
    public const string UIRootName = "uiroot";
    public const string PageLayerName = "PageLayer";
    public const string PopupLayerName = "PopupLayer";
    public const string TopLayerName = "TopLayer";
    public const string SystemLayerName = "SystemLayer";

    public bool IsInitialized { get; private set; }
    public Transform UIRootTransform { get; private set; }
    public Transform PageLayerTransform { get; private set; }
    public Transform PopupLayerTransform { get; private set; }
    public Transform TopLayerTransform { get; private set; }
    public Transform SystemLayerTransform { get; private set; }

    private readonly Dictionary<GameObject, OpenedView> _openedViewByGameObject = new Dictionary<GameObject, OpenedView>();
    private readonly Dictionary<string, Type> _uiScriptTypeByViewName = new Dictionary<string, Type>();
    private readonly Dictionary<string, OpenedView> _openedViewByName = new Dictionary<string, OpenedView>();
    private EventSystem _gameEventSystem;

    public void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        GameObject uIRootGameObject = GameObject.Find(UIRootName);
        if (uIRootGameObject == null)
        {
            throw new System.InvalidOperationException("Cannot find ui root game object.");
        }

        UIRootTransform = uIRootGameObject.transform;
        PageLayerTransform = GetOrCreateLayer(PageLayerName, 0);
        PopupLayerTransform = GetOrCreateLayer(PopupLayerName, 1);
        TopLayerTransform = GetOrCreateLayer(TopLayerName, 2);
        SystemLayerTransform = GetOrCreateLayer(SystemLayerName, 3);
        RegisterAllUIScripts();
        IsInitialized = true;
    }

    public void BindEventSystem(EventSystem gameEventSystem)
    {
        if (gameEventSystem == null)
        {
            throw new System.ArgumentNullException(nameof(gameEventSystem));
        }

        if (!IsInitialized)
        {
            throw new System.InvalidOperationException("UIManager must be initialized before binding event system.");
        }

        if (ReferenceEquals(_gameEventSystem, gameEventSystem))
        {
            return;
        }

        UnbindEventSystem();
        _gameEventSystem = gameEventSystem;
        _gameEventSystem.Register(EventRoute.CloseView, OnCloseView);
        _gameEventSystem.Register(EventRoute.OpenView, OnOpenView);
    }

    public void Shutdown()
    {
        if (!IsInitialized)
        {
            return;
        }

        UnbindEventSystem();
        CloseAllOpenedViews();
        UIRootTransform = null;
        PageLayerTransform = null;
        PopupLayerTransform = null;
        TopLayerTransform = null;
        SystemLayerTransform = null;
        _uiScriptTypeByViewName.Clear();
        IsInitialized = false;
    }

    private Transform GetOrCreateLayer(string layerName, int siblingIndex)
    {
        Transform layerTransform = UIRootTransform.Find(layerName);
        if (layerTransform == null)
        {
            GameObject layerGameObject = new GameObject(layerName);
            layerTransform = layerGameObject.transform;
            layerTransform.SetParent(UIRootTransform, false);
        }

        layerTransform.SetSiblingIndex(siblingIndex);
        return layerTransform;
    }

    private void UnbindEventSystem()
    {
        if (_gameEventSystem == null)
        {
            return;
        }

        _gameEventSystem.Unregister(EventRoute.CloseView, OnCloseView);
        _gameEventSystem.Unregister(EventRoute.OpenView, OnOpenView);
        _gameEventSystem = null;
    }

    private void OnCloseView(Arg arg)
    {
        CloseViewArg closeViewArg = arg as CloseViewArg;
        if (closeViewArg == null)
        {
            throw new System.ArgumentException("CloseView event requires CloseViewArg.", nameof(arg));
        }

        if (closeViewArg.ViewGameObject != null)
        {
            CloseView(closeViewArg.ViewGameObject);
            return;
        }

        CloseView(closeViewArg.ViewName);
    }

    private void OnOpenView(Arg arg)
    {
        OpenViewArg openViewArg = arg as OpenViewArg;
        if (openViewArg == null)
        {
            throw new System.ArgumentException("OpenView event requires OpenViewArg.", nameof(arg));
        }

        Transform parentTransform = GetOpenParentTransform(openViewArg);
        OpenView(parentTransform, openViewArg.ViewName, openViewArg.ViewObjectName, openViewArg.ViewArg);
    }

    private Transform GetOpenParentTransform(OpenViewArg openViewArg)
    {
        if (openViewArg.ParentGameObject != null)
        {
            return openViewArg.ParentGameObject.transform;
        }

        if (!string.IsNullOrEmpty(openViewArg.Layer))
        {
            return GetLayerTransform(openViewArg.Layer);
        }

        return UIRootTransform;
    }

    private Transform GetLayerTransform(string layerName)
    {
        switch (layerName)
        {
            case PageLayerName:
                return PageLayerTransform;
            case PopupLayerName:
                return PopupLayerTransform;
            case TopLayerName:
                return TopLayerTransform;
            case SystemLayerName:
                return SystemLayerTransform;
            default:
                throw new System.ArgumentOutOfRangeException(nameof(layerName), layerName, "Unsupported UI layer.");
        }
    }

    private void OpenView(Transform parentTransform, string viewName, string viewObjectName, Arg gameViewArg)
    {
        if (string.IsNullOrEmpty(viewName))
        {
            throw new System.ArgumentException("View name cannot be null or empty.", nameof(viewName));
        }

        string openedViewName = string.IsNullOrEmpty(viewObjectName) ? viewName : viewObjectName;
        if (_openedViewByName.TryGetValue(openedViewName, out OpenedView openedView))
        {
            CloseOpenedView(openedView);
        }

        UIScript gameUIScript = CreateUIScript(viewName);
        gameUIScript.BindEventSystem(_gameEventSystem);
        gameUIScript.Open(parentTransform, openedViewName, gameViewArg);
        OpenedView newOpenedView = new OpenedView(gameUIScript);
        _openedViewByName.Add(newOpenedView.ViewObjectName, newOpenedView);
        _openedViewByGameObject.Add(newOpenedView.ViewGameObject, newOpenedView);
    }

    private void CloseView(string viewName)
    {
        if (string.IsNullOrEmpty(viewName))
        {
            throw new System.ArgumentException("View name cannot be null or empty.", nameof(viewName));
        }

        if (_openedViewByName.TryGetValue(viewName, out OpenedView openedView))
        {
            CloseOpenedView(openedView);
        }
    }

    private void CloseView(GameObject viewGameObject)
    {
        if (viewGameObject == null)
        {
            throw new System.ArgumentNullException(nameof(viewGameObject));
        }

        if (_openedViewByGameObject.TryGetValue(viewGameObject, out OpenedView openedView))
        {
            CloseOpenedView(openedView);
        }
    }

    private void RegisterAllUIScripts()
    {
        _uiScriptTypeByViewName.Clear();

        Type uiScriptType = typeof(UIScript);
        Type[] allTypes = Assembly.GetAssembly(uiScriptType).GetTypes();
        foreach (Type type in allTypes)
        {
            if (type.IsAbstract)
            {
                continue;
            }

            if (!uiScriptType.IsAssignableFrom(type))
            {
                continue;
            }

            if (_uiScriptTypeByViewName.ContainsKey(type.Name))
            {
                throw new InvalidOperationException($"Duplicate UIScript name detected: {type.Name}");
            }

            _uiScriptTypeByViewName.Add(type.Name, type);
        }
    }

    private UIScript CreateUIScript(string viewName)
    {
        if (!_uiScriptTypeByViewName.TryGetValue(viewName, out Type uiScriptType))
        {
            throw new ArgumentOutOfRangeException(nameof(viewName), viewName, "Unsupported view name.");
        }

        return (UIScript)Activator.CreateInstance(uiScriptType);
    }

    private void CloseAllOpenedViews()
    {
        OpenedView[] openedViews = new OpenedView[_openedViewByName.Count];
        _openedViewByName.Values.CopyTo(openedViews, 0);
        foreach (OpenedView openedView in openedViews)
        {
            CloseOpenedView(openedView);
        }
    }

    private void CloseOpenedView(OpenedView openedView)
    {
        if (openedView == null)
        {
            return;
        }

        _openedViewByName.Remove(openedView.ViewObjectName);
        _openedViewByGameObject.Remove(openedView.ViewGameObject);
        openedView.GameUIScript.Close();
    }

    private sealed class OpenedView
    {
        public GameObject ViewGameObject { get; }
        public string ViewObjectName { get; }
        public UIScript GameUIScript { get; }

        public OpenedView(UIScript gameUIScript)
        {
            if (gameUIScript == null)
            {
                throw new ArgumentNullException(nameof(gameUIScript));
            }

            if (gameUIScript.ViewGameObject == null)
            {
                throw new InvalidOperationException("UIScript view game object is missing.");
            }

            ViewGameObject = gameUIScript.ViewGameObject;
            ViewObjectName = ViewGameObject.name;
            GameUIScript = gameUIScript;
        }
    }
}
