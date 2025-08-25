using DCEventSystem.Core;

namespace DCEventSystem.Internal.Subscriptions;

internal interface ISubscription<in T> : IDisposable where T : struct, IEvent
{
    bool IsAlive { get; }
    Action<T> GetAction();
}