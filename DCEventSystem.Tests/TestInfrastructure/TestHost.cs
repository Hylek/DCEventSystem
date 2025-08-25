using System;
using System.Collections.Generic;
using DCEventSystem.Core;

namespace DCEventSystem.Tests.TestInfrastructure;

public sealed class TestHost : IEventSystemHost
{
    private Action _updateCallback = null!;

    public readonly List<string> Errors = new();
    public readonly List<string> Warnings = new();

    private float _currentTime;
    public float CurrentTime => _currentTime;

    public void LogError(string message) => Errors.Add(message);
    public void LogWarning(string message) => Warnings.Add(message);

    public void ScheduleUpdate(Action updateCallback)
    {
        _updateCallback = updateCallback ?? throw new ArgumentNullException(nameof(updateCallback));
    }

    public void Tick(int frames = 1, float secondsPerFrame = 1f/60f)
    {
        if (_updateCallback == null) return;
        for (int i = 0; i < frames; i++)
        {
            _currentTime += secondsPerFrame;
            _updateCallback();
        }
    }

    public void SetTime(float time)
    {
        _currentTime = time;
    }
}
