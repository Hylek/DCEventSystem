namespace DCEventSystem.Internal.Events;

/// <summary>
/// Base class for queued events that can be processed and returned to pools
/// </summary>
internal abstract class QueuedEventBase
{
    public abstract void Process();
    public abstract void Return();
}