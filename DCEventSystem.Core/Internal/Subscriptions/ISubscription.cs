using System;

namespace DCEventSystem.Core.Internal.Subscriptions;

internal interface ISubscription<in T> : IDisposable where T : struct, IDCEvent
{
    bool IsAlive { get; }
    Action<T> GetAction();
}