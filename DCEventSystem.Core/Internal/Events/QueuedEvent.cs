using DCEventSystem.Core;
using DCEventSystem.Internal.Caching;

namespace DCEventSystem.Internal.Events;

/// <summary>
/// Concrete implementation of queued event for specific event type
/// </summary>
internal sealed class QueuedEvent<T>(EventCache<T> cache) : QueuedEventBase
    where T : struct, IEvent
{
    private readonly EventCache<T> _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private T _data;

    public void SetData(T data)
    {
        _data = data;
    }

    public override void Process()
    {
        _cache.Publish(ref _data);
    }

    public override void Return()
    {
        _data = default;
        _cache.ReturnQueuedEvent(this);
    }
}