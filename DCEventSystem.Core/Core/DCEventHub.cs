using System;
using System.Collections.Generic;
using DCEventSystem.Core.Internal.Caching;
using DCEventSystem.Core.Internal.Collections;
using DCEventSystem.Core.Internal.Events;

namespace DCEventSystem.Core;

    /// <summary>
    /// High-performance event system with queuing and priority support
    /// </summary>
    public static class DCEventHub
    {
        private static readonly Dictionary<Type, IEventCache> Caches = new();
        private static readonly Queue<QueuedEventBase> EventQueue = new();
        private static readonly MinHeap<QueuedEventBase> PriorityQueue = new(256);
        
        private static int _frameCounter;
        private static float _lastPoolCleanupTime;
        private const int WeakRefCleanupInterval = 60;
        private const float PoolCleanupInterval = 5f;
        private const int MaxQueueSize = 10000;
        private const int QueueWarningThreshold = 5000;
        
#if DEBUG
        private static readonly Dictionary<Type, int> SubscriptionCounts = new();
        private const int SubscriptionWarningThreshold = 100;
#endif

        /// <summary>
        /// Internal access to the host for other classes
        /// </summary>
        internal static IDCEventSystemHost Host { get; private set; } = null!;

        /// <summary>
        /// Initialize the event system with a host implementation
        /// </summary>
        /// <param name="host">Host that provides platform-specific services</param>
        /// <exception cref="ArgumentNullException">Thrown when host is null</exception>
        /// <exception cref="DCEventSystemAlreadyInitialisedException">Thrown when already initialized</exception>
        public static void Initialise(IDCEventSystemHost host)
        {
            if (Host != null)
            {
                throw new DCEventSystemAlreadyInitialisedException();
            }
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Host.ScheduleUpdate(Update);
        }

        /// <summary>
        /// Publish an event immediately to all subscribers
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="evt">Event data</param>
        /// <exception cref="DCEventSystemNotInitialisedException">Thrown when not initialized</exception>
        public static void Publish<T>(T evt) where T : struct, IDCEvent
        {
            if (Host == null)
                throw new DCEventSystemNotInitialisedException();
                
            var cache = GetOrCreateCache<T>();
            cache.Publish(ref evt);
        }

        /// <summary>
        /// Queue an event for processing at the end of the frame
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="evt">Event data</param>
        /// <param name="priority">Priority (lower values = higher priority, 0 = standard queue)</param>
        /// <exception cref="DCEventSystemNotInitialisedException">Thrown when not initialized</exception>
        public static void Queue<T>(T evt, int priority = 0) where T : struct, IDCEvent
        {
            if (Host == null)
                throw new DCEventSystemNotInitialisedException();
                
            switch (EventQueue.Count + PriorityQueue.Count)
            {
                case >= MaxQueueSize:
                    Host.LogError($"Event queue overflow! Dropping event of type {typeof(T).Name}");
                    return;
                
                case >= QueueWarningThreshold:
                    Host.LogWarning($"Event queue size warning: {EventQueue.Count + PriorityQueue.Count} events queued");
                    break;
            }

            var cache = GetOrCreateCache<T>();
            var queuedEvent = cache.CreateQueuedEvent(evt);
            
            if (priority == 0)
            {
                EventQueue.Enqueue(queuedEvent);
            }
            else
            {
                PriorityQueue.Push(queuedEvent, priority);
            }
        }

        /// <summary>
        /// Subscribe to events of a specific type
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="handler">Handler to invoke when event is published</param>
        /// <param name="useStrongReference">Whether to use strong reference (prevents GC of handler)</param>
        /// <returns>Disposable subscription that can be disposed to unsubscribe</returns>
        /// <exception cref="DCEventSystemNotInitialisedException">Thrown when not initialized</exception>
        public static IDisposable Subscribe<T>(Action<T> handler, bool useStrongReference = false) where T : struct, IDCEvent
        {
            if (Host == null)
                throw new DCEventSystemNotInitialisedException();
                
            var cache = GetOrCreateCache<T>();
            
#if DEBUG
            if (!SubscriptionCounts.ContainsKey(typeof(T)))
                SubscriptionCounts[typeof(T)] = 0;
            
            SubscriptionCounts[typeof(T)]++;
            
            if (SubscriptionCounts[typeof(T)] > SubscriptionWarningThreshold)
            {
                Host.LogWarning($"High subscription count for {typeof(T).Name}: {SubscriptionCounts[typeof(T)]}");
            }
#endif
            
            return cache.Subscribe(handler, useStrongReference);
        }

        /// <summary>
        /// Process all queued events
        /// </summary>
        public static void ProcessQueuedEvents()
        {
            while (PriorityQueue.Count > 0)
            {
                var evt = PriorityQueue.Pop();
                evt.Process();
                evt.Return();
            }
            
            while (EventQueue.Count > 0)
            {
                var evt = EventQueue.Dequeue();
                evt.Process();
                evt.Return();
            }
        }

        private static void Update()
        {
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

        private static EventCache<T> GetOrCreateCache<T>() where T : struct, IDCEvent
        {
            if (Caches.TryGetValue(typeof(T), out var cache)) return (EventCache<T>)cache;
            
            cache = new EventCache<T>();
            Caches[typeof(T)] = cache;
            return (EventCache<T>)cache;
        }

        private static void CleanupDeadReferences()
        {
            foreach (var cache in Caches.Values)
            {
                cache.CleanupDead();
            }
        }

        private static void CleanupPools()
        {
            foreach (var cache in Caches.Values)
            {
                cache.CleanupPool();
            }
        }

#if DEBUG
        /// <summary>
        /// Print debug statistics about the event system
        /// </summary>
        public static void DebugPrintStats()
        {
            Host.LogWarning("=== EventSystem Debug Stats ===");
            Host.LogWarning($"Event Types Cached: {Caches.Count}");
            Host.LogWarning($"Queue Sizes - Standard: {EventQueue.Count}, Priority: {PriorityQueue.Count}");
            
            foreach (var kvp in SubscriptionCounts)
            {
                Host?.LogWarning($"  {kvp.Key.Name}: {kvp.Value} subscriptions");
            }
        }

        /// <summary>
        /// Internal method to decrement subscription count for debug tracking
        /// </summary>
        internal static void DecrementSubscriptionCount<T>() where T : struct, IDCEvent
        {
            if (SubscriptionCounts.ContainsKey(typeof(T)))
            {
                SubscriptionCounts[typeof(T)]--;
            }
        }
#endif
}