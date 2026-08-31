using UnityEngine;

public class Launch : MonoBehaviour
{
    private GameApp _gameApp;

    private void Awake()
    {
        _gameApp = new GameApp();
        _gameApp.Initialize();
    }

    private void Update()
    {
        _gameApp?.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        _gameApp?.Shutdown();
    }
}
