using System;

namespace DCEventSystem.Core;

/// <summary>
/// Static facade for convenience, wrapping a single default DCEventHub instance.
/// </summary>
public static class DCEventHubFacade
{
    public static DCEventHub Default { get; private set; } = null!;

    /// <summary>
    /// Initialize the default hub. Calling it again will replace the previous hub.
    /// </summary>
    public static void Initialise(IDCEventSystemHost host)
    {
        Default?.Dispose();
        Default = new DCEventHub(host);
    }

    private static DCEventHub RequireDefault()
    {
        return Default ?? throw new DCEventSystemNotInitialisedException();
    }

    public static IDisposable Subscribe<T>(Action<T> handler, bool useStrongReference = false) where T : struct, IDCEvent
        => RequireDefault().Subscribe(handler, useStrongReference);

    public static void Unsubscribe(IDisposable subscription) => RequireDefault().Unsubscribe(subscription);

    public static void Publish<T>(T evt) where T : struct, IDCEvent => RequireDefault().Publish(evt);

    public static void Queue<T>(T evt, int priority = 0) where T : struct, IDCEvent => RequireDefault().Queue(evt, priority);

    public static void ProcessQueuedEvents() => RequireDefault().ProcessQueuedEvents();

#if DEBUG
    public static void DebugPrintStats() => Default.DebugPrintStats();
#endif
}