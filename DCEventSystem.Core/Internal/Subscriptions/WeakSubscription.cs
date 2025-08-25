using System;
using DCEventSystem.Core;
using DCEventSystem.Internal.Caching;

namespace DCEventSystem.Internal.Subscriptions;

/// <summary>
/// Weak reference subscription that allows garbage collection of handler
/// </summary>
internal sealed class WeakSubscription<T>(Action<T> handler, EventCache<T> cache) : ISubscription<T>
    where T : struct, IEvent
{
    private readonly WeakReference _handler = new(handler ?? throw new ArgumentNullException(nameof(handler)));
    private readonly EventCache<T> _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private bool _disposed;

    public bool IsAlive => !_disposed && _handler.IsAlive;

    public Action<T> GetAction()
    {
        if (_disposed) return null!;
        
        return _handler.Target as Action<T> ?? throw new InvalidOperationException();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cache.RemoveSubscription(this);
        }
    }
}