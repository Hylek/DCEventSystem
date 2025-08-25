using DCEventSystem.Core;
using DCEventSystem.Internal.Caching;

namespace DCEventSystem.Internal.Subscriptions;

/// <summary>
/// Strong reference subscription that prevents garbage collection of handler
/// </summary>
internal sealed class StrongSubscription<T>(Action<T> handler, EventCache<T> cache) : ISubscription<T>
    where T : struct, IEvent
{
    private readonly Action<T> _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    private readonly EventCache<T> _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private bool _disposed;

    public bool IsAlive => !_disposed;

    public Action<T> GetAction() => (_disposed ? null : _handler) ?? throw new InvalidOperationException();

    public void Dispose()
    {
        if (_disposed) return;
            
        _disposed = true;
        _cache.RemoveSubscription(this);
    }
}