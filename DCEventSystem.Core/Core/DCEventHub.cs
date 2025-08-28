using System;
using System.Collections.Generic;
using DCEventSystem.Core.Internal.Caching;
using DCEventSystem.Core.Internal.Collections;
using DCEventSystem.Core.Internal.Events;

namespace DCEventSystem.Core;

/// <summary>
/// High-performance event system with queuing and priority support (instance-based)
/// </summary>
public sealed class DCEventHub : IDisposable
{
    private readonly Dictionary<Type, IEventCache> _caches = new();
    private readonly Queue<QueuedEventBase> _eventQueue = new();
    private readonly MinHeap<QueuedEventBase> _priorityQueue = new(256);

    private int _frameCounter;
    private float _lastPoolCleanupTime;

    public int WeakRefCleanupInterval { get; set; } = 60;
    public float PoolCleanupInterval { get; set; } = 5f;
    public int MaxQueueSize { get; set; } = 10000;
    public int QueueWarningThreshold { get; set; } = 5000;

#if DEBUG
    private readonly Dictionary<Type, int> _subscriptionCounts = new();
    public int SubscriptionWarningThreshold { get; set; } = 100;
#endif

    /// <summary>
    /// Access to the host for other classes (kept internal for internal components)
    /// </summary>
    internal IDCEventSystemHost Host { get; }

    private bool _disposed;

    public DCEventHub(IDCEventSystemHost host)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Host.ScheduleUpdate(Update);
    }

    /// <summary>
    /// Publish an event immediately to all subscribers
    /// </summary>
    public void Publish<T>(T evt) where T : struct, IDCEvent
    {
        EnsureNotDisposed();
        var cache = GetOrCreateCache<T>();
        cache.Publish(ref evt);
    }

    /// <summary>
    /// Queue an event for processing at the end of the frame
    /// </summary>
    public void Queue<T>(T evt, int priority = 0) where T : struct, IDCEvent
    {
        EnsureNotDisposed();

        var totalQueued = _eventQueue.Count + _priorityQueue.Count;
        if (totalQueued >= MaxQueueSize)
        {
            Host.LogError($"Event queue overflow! Dropping event of type {typeof(T).Name}");
            return;
        }
        if (totalQueued >= QueueWarningThreshold)
        {
            Host.LogWarning($"Event queue size warning: {totalQueued} events queued");
        }

        var cache = GetOrCreateCache<T>();
        var queuedEvent = cache.CreateQueuedEvent(evt);

        if (priority == 0)
        {
            _eventQueue.Enqueue(queuedEvent);
        }
        else
        {
            _priorityQueue.Push(queuedEvent, priority);
        }
    }

    /// <summary>
    /// Subscribe to events of a specific type
    /// </summary>
    public IDisposable Subscribe<T>(Action<T> handler, bool useStrongReference = false) where T : struct, IDCEvent
    {
        EnsureNotDisposed();
        var cache = GetOrCreateCache<T>();

#if DEBUG
        if (!_subscriptionCounts.ContainsKey(typeof(T)))
            _subscriptionCounts[typeof(T)] = 0;
        _subscriptionCounts[typeof(T)]++;
        if (_subscriptionCounts[typeof(T)] > SubscriptionWarningThreshold)
        {
            Host.LogWarning($"High subscription count for {typeof(T).Name}: {_subscriptionCounts[typeof(T)]}");
        }
#endif
        return cache.Subscribe(handler, useStrongReference);
    }

    /// <summary>
    /// Unsubscribe helper to dispose a subscription
    /// </summary>
    public void Unsubscribe(IDisposable subscription)
    {
        subscription?.Dispose();
    }

    /// <summary>
    /// Process all queued events
    /// </summary>
    public void ProcessQueuedEvents()
    {
        EnsureNotDisposed();
        while (_priorityQueue.Count > 0)
        {
            var evt = _priorityQueue.Pop();
            evt.Process();
            evt.Return();
        }
        while (_eventQueue.Count > 0)
        {
            var evt = _eventQueue.Dequeue();
            evt.Process();
            evt.Return();
        }
    }

    private void Update()
    {
        if (_disposed) return;
        ProcessQueuedEvents();

        _frameCounter++;
        if (_frameCounter % WeakRefCleanupInterval == 0)
        {
            CleanupDeadReferences();
        }

        if (!(Host.CurrentTime - _lastPoolCleanupTime > PoolCleanupInterval)) return;
        _lastPoolCleanupTime = Host.CurrentTime;
        CleanupPools();
    }

    private EventCache<T> GetOrCreateCache<T>() where T : struct, IDCEvent
    {
        if (_caches.TryGetValue(typeof(T), out var cache)) return (EventCache<T>)cache;
        cache = new EventCache<T>();
        _caches[typeof(T)] = cache;
        return (EventCache<T>)cache;
    }

    private void CleanupDeadReferences()
    {
        foreach (var cache in _caches.Values)
        {
            cache.CleanupDead();
        }
    }

    private void CleanupPools()
    {
        foreach (var cache in _caches.Values)
        {
            cache.CleanupPool();
        }
    }

#if DEBUG
    /// <summary>
    /// Print debug statistics about the event system
    /// </summary>
    public void DebugPrintStats()
    {
        Host.LogWarning("=== EventSystem Debug Stats ===");
        Host.LogWarning($"Event Types Cached: {_caches.Count}");
        Host.LogWarning($"Queue Sizes - Standard: {_eventQueue.Count}, Priority: {_priorityQueue.Count}");
        foreach (var kvp in _subscriptionCounts)
        {
            Host?.LogWarning($"  {kvp.Key.Name}: {kvp.Value} subscriptions");
        }
    }
#endif

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DCEventHub));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // No Unschedule in host API; rely on host to stop invoking Update when hub disposed or tolerate no-op calls.
        _eventQueue.Clear();
        while (_priorityQueue.Count > 0) _priorityQueue.Pop();
        _caches.Clear();
    }
}