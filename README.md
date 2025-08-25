# DCEventSystem
A high-performance, zero-allocation event system designed specifically for games and real-time applications. Built for Unity, Stride, and any .NET environment where performance matters.

## ✨ Features
- **🚀 High Performance**: Zero-allocation publishing with object pooling
- **⚡ Priority Queuing**: Process events with custom priority levels  
- **🎯 Type Safety**: Compile-time type safety with struct-based events
- **🔄 Flexible Subscriptions**: Strong and weak reference options
- **🧹 Auto Cleanup**: Automatic cleanup of dead subscriptions and object pools

## 📋 Requirements
- .NET Standard 2.1 or higher
- C# 8.0+ (for performance optimizations)

### Unity
1. Add the NuGet package via Package Manager
2. Copy the Unity adapter files to your project:
   - `UnityEventSystemHost.cs`
   - `UnityEventSystemInitializer.cs`

### Stride Engine
1. Add the NuGet package to your project
2. Copy the Stride adapter files:
   - `StrideEventSystemHost.cs` 
   - `StrideEventSystemInitializer.cs`

### 1. Define Your Events

Events must be structs implementing `IEvent`:

```csharp
using EventSystem.Core;

public struct PlayerDiedEvent : IEvent
{
    public int PlayerId;
    public Vector3 Position;
    public string Cause;
}

public struct ScoreChangedEvent : IEvent
{
    public int PlayerId;
    public int NewScore;
    public int Delta;
}
```

### 2. Initialise the System

The EventSystem initialises automatically in Unity and Stride. For custom platforms:

```csharp
// Create a host implementation
public class MyGameHost : IEventSystemHost
{
    public float CurrentTime => /* your time source */;
    public void LogError(string message) => Console.WriteLine($"ERROR: {message}");
    public void LogWarning(string message) => Console.WriteLine($"WARN: {message}");
    public void ScheduleUpdate(Action updateCallback) => /* schedule for each frame */;
}

// Initialize
EventSystem.Initialize(new MyGameHost());
```

### 3. Publishing Events

```csharp
// Immediate publishing (processed right away)
EventSystem.Publish(new PlayerDiedEvent 
{ 
    PlayerId = 123, 
    Position = playerPosition,
    Cause = "Fell into lava"
});

// Queued publishing (processed at end of frame)
EventSystem.Queue(new ScoreChangedEvent 
{ 
    PlayerId = 123, 
    NewScore = 1500, 
    Delta = 100 
});

// Priority queuing (lower numbers = higher priority)
EventSystem.Queue(new PlayerDiedEvent { /* ... */ }, priority: -10); // High priority
EventSystem.Queue(new ScoreChangedEvent { /* ... */ }, priority: 10); // Low priority
```

### 4. Subscribing to Events

```csharp
// Basic subscription
var subscription = EventSystem.Subscribe<PlayerDiedEvent>(evt =>
{
    Debug.Log($"Player {evt.PlayerId} died at {evt.Position}: {evt.Cause}");
});

// Strong reference subscription (prevents GC of handler)
var strongSub = EventSystem.Subscribe<ScoreChangedEvent>(OnScoreChanged, useStrongReference: true);

// Don't forget to unsubscribe!
subscription.Dispose();

void OnScoreChanged(ScoreChangedEvent evt)
{
    UpdateScoreboard(evt.PlayerId, evt.NewScore);
}
```

### 5. Platform-Specific Usage Examples

#### Unity Example
```csharp
public class PlayerController : MonoBehaviour
{
    private IDisposable _subscription;
    
    void Start()
    {
        // Auto-cleanup when GameObject is destroyed
        gameObject.SubscribeUntilDestroyed<PlayerDiedEvent>(OnPlayerDied);
        
        // Manual subscription
        _subscription = EventSystem.Subscribe<ScoreChangedEvent>(OnScoreChanged);
    }
    
    void OnDestroy()
    {
        _subscription?.Dispose();
    }
    
    void OnPlayerDied(PlayerDiedEvent evt)
    {
        if (evt.PlayerId == myPlayerId)
            ShowDeathScreen();
    }
}
```

#### Stride Engine Example
```csharp
public class GameScript : AsyncScript
{
    public override async Task Execute()
    {
        // Auto-cleanup when entity is removed
        Entity.SubscribeUntilRemoved<PlayerDiedEvent>(OnPlayerDied);
        
        while (Game.IsRunning)
        {
            // Your game logic here
            await Script.NextFrame();
        }
    }
    
    void OnPlayerDied(PlayerDiedEvent evt)
    {
        Log.Info($"Player {evt.PlayerId} died!");
    }
}
```

## 🏗️ Architecture

```
DCEventSystem.Core/         # Platform-agnostic core
├── Core/
│   ├── DCEventSystem.cs    # Main static API
│   ├── IEvent.cs           # Event marker interface
│   └── IEventSystemHost.cs # Platform abstraction
├── Internal/
│   ├── Collections/
│   │   └── MinHeap.cs      # Priority queue implementation
│   ├── Events/
│   │   ├── QueuedEventBase.cs
│   │   └── QueuedEvent.cs
│   ├── Caching/
│   │   ├── EventCache.cs   # Per-type event management
│   │   └── IEventCache.cs
│   └── Subscriptions/
│       ├── ISubscription.cs
│       ├── StrongSubscription.cs
│       └── WeakSubscription.cs
```

## ⚡ Performance Features

### Zero-Allocation Publishing
```csharp
// No boxing, no allocations during normal operation
EventSystem.Publish(new PlayerDiedEvent { PlayerId = 123 });
```

### Object Pooling
- Automatic pooling of queued events
- ArrayPool usage for temporary arrays
- Configurable pool sizes and cleanup intervals

### Weak References
```csharp
// Allows garbage collection of handlers (default)
EventSystem.Subscribe<MyEvent>(handler, useStrongReference: false);

// Prevents garbage collection of handlers
EventSystem.Subscribe<MyEvent>(handler, useStrongReference: true);
```

### Batch Processing
```csharp
// Process all queued events at once (typically end-of-frame)
EventSystem.ProcessQueuedEvents();
```

## 🛠️ Advanced Usage

### Custom Event Processing
```csharp
public class EventManager : MonoBehaviour
{
    void LateUpdate()
    {
        // Custom processing timing
        EventSystem.ProcessQueuedEvents();
    }
}
```

### Multiple Subscription Patterns
```csharp
public class GameManager : MonoBehaviour
{
    private readonly List<IDisposable> _subscriptions = new();
    
    void Start()
    {
        // Subscribe to multiple events
        _subscriptions.Add(EventSystem.Subscribe<PlayerDiedEvent>(OnPlayerDied));
        _subscriptions.Add(EventSystem.Subscribe<GameOverEvent>(OnGameOver));
        _subscriptions.Add(EventSystem.Subscribe<ScoreChangedEvent>(OnScoreChanged));
    }
    
    void OnDestroy()
    {
        // Clean up all subscriptions
        foreach (var sub in _subscriptions)
            sub?.Dispose();
        _subscriptions.Clear();
    }
}
```

### Conditional Event Handling
```csharp
EventSystem.Subscribe<PlayerDiedEvent>(evt =>
{
    // Only handle events for local player
    if (evt.PlayerId == localPlayerId)
    {
        HandleLocalPlayerDeath(evt);
    }
});
```

## 🧪 Debugging

### Debug Statistics
```csharp
#if DEBUG
// Print detailed statistics about the event system
EventSystem.DebugPrintStats();
#endif
```

### Custom Logging
The system will log warnings and errors through your `IEventSystemHost` implementation:
- Queue overflow warnings
- High subscription count warnings  
- Event handler exceptions

## 🎯 Best Practices

### Event Design
```csharp
// ✅ Good: Small, focused events
public struct PlayerHealthChangedEvent : IEvent
{
    public int PlayerId;
    public int NewHealth;
    public int PreviousHealth;
}

// ❌ Avoid: Large events with lots of data
public struct MassiveGameStateEvent : IEvent
{
    public Player[] AllPlayers; // Prefer smaller, focused events
    public WorldData World;
    public UIState UI;
}
```

### Subscription Management
```csharp
// ✅ Good: Proper cleanup
public class MyComponent : IDisposable
{
    private IDisposable _subscription;
    
    public MyComponent()
    {
        _subscription = EventSystem.Subscribe<MyEvent>(HandleEvent);
    }
    
    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

### Performance Tips
- Use immediate publishing (`Publish`) for critical events
- Use queued publishing (`Queue`) for less critical events
- Prefer weak references for temporary objects
- Use strong references for long-lived objects
- Process queued events at consistent times (e.g., end of frame)

## 🙏 Acknowledgments

- Inspired by TinyMessenger https://github.com/grumpydev/TinyMessenger
- Built with performance and game development in mind
- Inspired by modern event-driven architecture patterns
- Designed for zero-allocation, high-frequency scenarios
