using System;
using System.Buffers;
using System.Collections.Generic;
using DCEventSystem.Core.Internal.Events;
using DCEventSystem.Core.Internal.Subscriptions;

namespace DCEventSystem.Core.Internal.Caching;

/// <summary>
/// Cache for managing subscriptions and pooling for a specific event type
/// </summary>
internal sealed class EventCache<T> : IEventCache where T : struct, IDCEvent
{
    private readonly HashSet<ISubscription<T>> _subscriptions = [];
    private readonly List<ISubscription<T>> _toRemove = [];
    private readonly Stack<QueuedEvent<T>> _eventPool = new(64);
        
    private Action<T>[] _actionBuffer = null!;
    private readonly ArrayPool<Action<T>> _actionArrayPool = ArrayPool<Action<T>>.Shared;

    public void Publish(ref T evt)
    {
        var count = 0;
        _actionBuffer = _actionArrayPool.Rent(_subscriptions.Count);
            
        try
        {
            foreach (var subscription in _subscriptions)
            {
                if (!subscription.IsAlive) continue;
                
                var action = subscription.GetAction();
                _actionBuffer[count++] = action;
            }
            
            for (var i = 0; i < count; i++)
            {
                try
                {
                    _actionBuffer[i](evt);
                }
                catch (Exception e)
                {
                    // Access host through EventSystem instead of static reference
                    Core.DCEventHub.Host?.LogError($"Error in event handler for {typeof(T).Name}: {e}");
                }
                finally
                {
                    _actionBuffer[i] = null!;
                }
            }
        }
        finally
        {
            _actionArrayPool.Return(_actionBuffer, clearArray: true);
            _actionBuffer = null!;
        }
    }

    public IDisposable Subscribe(Action<T> handler, bool useStrongReference)
    {
        ISubscription<T> subscription = useStrongReference
            ? new StrongSubscription<T>(handler, this)
            : new WeakSubscription<T>(handler, this);
            
        _subscriptions.Add(subscription);
        return subscription;
    }

    public QueuedEvent<T> CreateQueuedEvent(T data)
    {
        var evt = _eventPool.Count > 0 ? _eventPool.Pop() : new QueuedEvent<T>(this);
        evt.SetData(data);
        return evt;
    }

    public void ReturnQueuedEvent(QueuedEvent<T> evt)
    {
        if (_eventPool.Count < 4096)
        {
            _eventPool.Push(evt);
        }
    }

    public void RemoveSubscription(ISubscription<T> subscription)
    {
        _subscriptions.Remove(subscription);
    }

    public void CleanupDead()
    {
        _toRemove.Clear();
        foreach (var subscription in _subscriptions)
        {
            if (!subscription.IsAlive)
            {
                _toRemove.Add(subscription);
            }
        }
        foreach (var dead in _toRemove)
        {
            RemoveSubscription(dead);
        }
    }

    public void CleanupPool()
    {
        var targetSize = Math.Max(32, _eventPool.Count / 2);
        while (_eventPool.Count > targetSize)
        {
            _eventPool.Pop();
        }
    }
}