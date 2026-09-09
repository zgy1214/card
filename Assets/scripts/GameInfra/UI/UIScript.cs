using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class UIScript
{
    protected EventSystem GameEventSystem { get; private set; }
    protected Arg GameViewArg { get; private set; }
    public GameObject ViewGameObject { get; private set; }
    public Transform ViewTransform
    {
        get
        {
            return ViewGameObject == null ? null : ViewGameObject.transform;
        }
    }

    public string GetViewName()
    {
        return GetType().Name;
    }

    public abstract string GetPath();

    public virtual void OnOpen()
    {
    }

    public virtual void OnClose()
    {
    }

    public virtual void OnTick(float deltaTime)
    {
    }

    protected Transform FindViewTransform(string path)
    {
        if (ViewTransform == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("View path cannot be null or empty.", nameof(path));
        }

        Transform targetTransform = ViewTransform.Find(path);
        if (targetTransform != null)
        {
            return targetTransform;
        }

        for (int index = 0; index < ViewTransform.childCount; index += 1)
        {
            targetTransform = ViewTransform.GetChild(index).Find(path);
            if (targetTransform != null)
            {
                return targetTransform;
            }
        }

        return null;
    }

    protected GameObject OpenGameObject(Transform parentTransform, string prefabPath, string gameObjectName = null)
    {
        if (parentTransform == null)
        {
            throw new ArgumentNullException(nameof(parentTransform));
        }

        if (string.IsNullOrEmpty(prefabPath))
        {
            throw new InvalidOperationException("UI prefab path cannot be null or empty.");
        }

        GameObject viewPrefab = Resources.Load<GameObject>(prefabPath);
#if UNITY_EDITOR
        if (viewPrefab == null)
        {
            viewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/{prefabPath}.prefab");
        }
#endif
        if (viewPrefab == null)
        {
            throw new InvalidOperationException($"Cannot load UI prefab at path: {prefabPath}");
        }

        GameObject gameViewGameObject = UnityEngine.Object.Instantiate(viewPrefab, parentTransform, false);
        if (!string.IsNullOrEmpty(gameObjectName))
        {
            gameViewGameObject.name = gameObjectName;
        }

        gameViewGameObject.transform.SetAsLastSibling();
        return gameViewGameObject;
    }

    internal void BindEventSystem(EventSystem gameEventSystem)
    {
        if (gameEventSystem == null)
        {
            throw new ArgumentNullException(nameof(gameEventSystem));
        }

        GameEventSystem = gameEventSystem;
    }

    internal void Open(Transform parentTransform, string viewObjectName, Arg gameViewArg)
    {
        if (parentTransform == null)
        {
            throw new ArgumentNullException(nameof(parentTransform));
        }

        if (ViewGameObject != null)
        {
            throw new InvalidOperationException("UI view is already opened.");
        }

        string gameObjectName = string.IsNullOrEmpty(viewObjectName) ? GetViewName() : viewObjectName;
        GameObject gameViewGameObject = OpenGameObject(parentTransform, GetPath(), gameObjectName);
        BindView(gameViewGameObject);
        GameViewArg = gameViewArg;
        OnOpen();
    }

    internal void Close()
    {
        if (ViewGameObject == null)
        {
            return;
        }

        OnClose();
        GameObject gameViewGameObject = ViewGameObject;
        UnbindView();
        GameViewArg = null;
        GameEventSystem = null;
        UnityEngine.Object.Destroy(gameViewGameObject);
    }

    private void BindView(GameObject gameViewGameObject)
    {
        if (gameViewGameObject == null)
        {
            throw new ArgumentNullException(nameof(gameViewGameObject));
        }

        ViewGameObject = gameViewGameObject;
    }

    private void UnbindView()
    {
        ViewGameObject = null;
    }
}
