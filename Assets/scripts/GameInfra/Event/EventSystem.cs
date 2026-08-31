using System;
using System.Collections.Generic;

public sealed class EventSystem
{
    private readonly Dictionary<string, List<Action<Arg>>> _eventHandlersByRoute =
        new Dictionary<string, List<Action<Arg>>>();

    public void Register(string eventRoute, Action<Arg> eventHandler)
    {
        if (string.IsNullOrEmpty(eventRoute))
        {
            throw new ArgumentException("Event route cannot be null or empty.", nameof(eventRoute));
        }

        if (eventHandler == null)
        {
            throw new ArgumentNullException(nameof(eventHandler));
        }

        if (!_eventHandlersByRoute.TryGetValue(eventRoute, out List<Action<Arg>> eventHandlers))
        {
            eventHandlers = new List<Action<Arg>>();
            _eventHandlersByRoute.Add(eventRoute, eventHandlers);
        }

        eventHandlers.Add(eventHandler);
    }

    public void Unregister(string eventRoute, Action<Arg> eventHandler)
    {
        if (string.IsNullOrEmpty(eventRoute))
        {
            throw new ArgumentException("Event route cannot be null or empty.", nameof(eventRoute));
        }

        if (eventHandler == null)
        {
            throw new ArgumentNullException(nameof(eventHandler));
        }

        if (!_eventHandlersByRoute.TryGetValue(eventRoute, out List<Action<Arg>> eventHandlers))
        {
            return;
        }

        eventHandlers.Remove(eventHandler);

        if (eventHandlers.Count == 0)
        {
            _eventHandlersByRoute.Remove(eventRoute);
        }
    }

    public void Trigger(string eventRoute, Arg arg)
    {
        if (string.IsNullOrEmpty(eventRoute))
        {
            throw new ArgumentException("Event route cannot be null or empty.", nameof(eventRoute));
        }

        if (!_eventHandlersByRoute.TryGetValue(eventRoute, out List<Action<Arg>> eventHandlers))
        {
            return;
        }

        Action<Arg>[] eventHandlerSnapshot = eventHandlers.ToArray();
        foreach (Action<Arg> eventHandler in eventHandlerSnapshot)
        {
            eventHandler(arg);
        }
    }

    public void Clear()
    {
        _eventHandlersByRoute.Clear();
    }
}
