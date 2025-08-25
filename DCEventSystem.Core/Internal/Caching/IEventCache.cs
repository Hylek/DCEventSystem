namespace DCEventSystem.Internal.Caching;

internal interface IEventCache
{
    void CleanupDead();
    void CleanupPool();
}