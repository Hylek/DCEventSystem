using System;

namespace DCEventSystem.Core;

/// <summary>
/// Host interface that provides platform-specific services to the EventSystem
/// </summary>
public interface IEventSystemHost
{
    /// <summary>Gets the current time in seconds</summary>
    float CurrentTime { get; }
        
    /// <summary>Log an error message</summary>
    void LogError(string message);
        
    /// <summary>Log a warning message</summary>
    void LogWarning(string message);
        
    /// <summary>Schedule the update callback to be called each frame</summary>
    void ScheduleUpdate(Action updateCallback);
}