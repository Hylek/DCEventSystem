using System;
using DCEventSystem.Core;

namespace DCEventSystem.Extensions;

/// <summary>
/// Extension methods for common EventSystem operations
/// </summary>
public static class DCEventSystemExtensions
{
    /// <summary>
    /// Subscribe to events with automatic disposal when the target is destroyed
    /// </summary>
    public static IDisposable SubscribeWhileAlive<T>(this object target, Action<T> handler) 
        where T : struct, IEvent
    {
        // Could implement weak reference tracking tied to object lifetime
        return Core.DCEventSystem.Subscribe(handler, useStrongReference: false);
    }
        
    /// <summary>
    /// Queue an event with high priority (negative priority value)
    /// </summary>
    public static void QueueHighPriority<T>(T evt, int priority = -100) where T : struct, IEvent
    {
        Core.DCEventSystem.Queue(evt, priority);
    }
        
    /// <summary>
    /// Queue an event with low priority (positive priority value)
    /// </summary>
    public static void QueueLowPriority<T>(T evt, int priority = 100) where T : struct, IEvent
    {
        Core.DCEventSystem.Queue(evt, priority);
    }
}