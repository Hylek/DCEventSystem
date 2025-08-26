namespace DCEventSystem.Core.Internal.Caching;

internal interface IEventCache
{
    void CleanupDead();
    void CleanupPool();
}