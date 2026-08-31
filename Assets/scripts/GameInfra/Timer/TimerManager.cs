using System;
using System.Collections.Generic;

public sealed class TimerManager
{
    private readonly Dictionary<string, TimerTask> _timerTaskById = new Dictionary<string, TimerTask>();
    private readonly EventSystem _gameEventSystem;

    public TimerManager(EventSystem gameEventSystem)
    {
        _gameEventSystem = gameEventSystem ?? throw new ArgumentNullException(nameof(gameEventSystem));
        _gameEventSystem.Register(EventRoute.RegisterTimer, OnRegisterTimer);
        _gameEventSystem.Register(EventRoute.UnregisterTimer, OnUnregisterTimer);
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time cannot be negative.");
        }

        if (_timerTaskById.Count == 0)
        {
            return;
        }

        TimerTask[] gameTimerTasks = new TimerTask[_timerTaskById.Count];
        _timerTaskById.Values.CopyTo(gameTimerTasks, 0);
        foreach (TimerTask gameTimerTask in gameTimerTasks)
        {
            if (gameTimerTask == null || gameTimerTask.IsRemoved || !_timerTaskById.ContainsKey(gameTimerTask.TimerId))
            {
                continue;
            }

            bool shouldRemove = gameTimerTask.Update(deltaTime);
            if (shouldRemove)
            {
                RemoveTimer(gameTimerTask.TimerId);
            }
        }
    }

    public void Shutdown()
    {
        _gameEventSystem.Unregister(EventRoute.RegisterTimer, OnRegisterTimer);
        _gameEventSystem.Unregister(EventRoute.UnregisterTimer, OnUnregisterTimer);

        foreach (TimerTask gameTimerTask in _timerTaskById.Values)
        {
            gameTimerTask.Remove();
        }

        _timerTaskById.Clear();
    }

    private void RegisterTimer(RegisterTimerArg registerTimerArg)
    {
        if (_timerTaskById.ContainsKey(registerTimerArg.TimerId))
        {
            throw new InvalidOperationException($"Timer id already exists: {registerTimerArg.TimerId}");
        }

        TimerTask gameTimerTask = new TimerTask(
            registerTimerArg.TimerId,
            registerTimerArg.TickInterval,
            registerTimerArg.StartDelay,
            registerTimerArg.Duration,
            registerTimerArg.IsImmediateTickOnStart,
            registerTimerArg.OnTick);
        _timerTaskById.Add(gameTimerTask.TimerId, gameTimerTask);
    }

    private void RemoveTimer(string timerId)
    {
        if (!_timerTaskById.TryGetValue(timerId, out TimerTask gameTimerTask))
        {
            return;
        }

        gameTimerTask.Remove();
        _timerTaskById.Remove(timerId);
    }

    private void OnRegisterTimer(Arg arg)
    {
        RegisterTimerArg registerTimerArg = arg as RegisterTimerArg;
        if (registerTimerArg == null)
        {
            throw new ArgumentException("RegisterTimer event requires RegisterTimerArg.", nameof(arg));
        }

        RegisterTimer(registerTimerArg);
    }

    private void OnUnregisterTimer(Arg arg)
    {
        UnregisterTimerArg unregisterTimerArg = arg as UnregisterTimerArg;
        if (unregisterTimerArg == null)
        {
            throw new ArgumentException("UnregisterTimer event requires UnregisterTimerArg.", nameof(arg));
        }

        RemoveTimer(unregisterTimerArg.TimerId);
    }
}
