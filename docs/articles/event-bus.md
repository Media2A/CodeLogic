# Event Bus

CodeLogic provides a shared publish-subscribe event bus (`IEventBus`) available on every context object. Libraries, the application, and plugins all share the same bus instance.

---

## Defining Events

Events are plain C# records implementing `IEvent`:

```csharp
public interface IEvent { }

public record UserCreatedEvent(string UserId, string Email) : IEvent;
public record DeviceStateChangedEvent(string DeviceId, string NewState, string OldState) : IEvent;
public record AlertEvent(string Source, string Message, AlertSeverity Severity) : IEvent;
```

Events should be immutable — use `record` types.

---

## IEventBus

```csharp
public interface IEventBus
{
    // Synchronous publish — sync handlers run immediately, async handlers are fire-and-forget
    void Publish<T>(T @event) where T : IEvent;

    // Asynchronous publish — awaits all handlers (sync and async)
    Task PublishAsync<T>(T @event) where T : IEvent;

    // Subscribe a synchronous handler
    IEventSubscription Subscribe<T>(Action<T> handler) where T : IEvent;

    // Subscribe an asynchronous handler
    IEventSubscription SubscribeAsync<T>(Func<T, Task> handler) where T : IEvent;
}
```

---

## Publishing Events

From any phase after `OnStartAsync` (context is available):

```csharp
// fire-and-forget sync publish
context.Events.Publish(new UserCreatedEvent("user-123", "alice@example.com"));

// awaited async publish — waits for all handlers
await context.Events.PublishAsync(new DeviceStateChangedEvent("switch-1", "On", "Off"));
```

---

## Subscribing to Events

Subscribe in `OnStartAsync`. Store the subscription to dispose it in `OnStopAsync`:

```csharp
private IEventSubscription _userCreatedSub = null!;

public Task OnStartAsync(LibraryContext context)
{
    // synchronous handler
    _userCreatedSub = context.Events.Subscribe<UserCreatedEvent>(e =>
    {
        context.Logger.LogInformation("User created: {Email}", e.Email);
    });

    return Task.CompletedTask;
}

public Task OnStopAsync()
{
    _userCreatedSub.Dispose();   // unsubscribes
    return Task.CompletedTask;
}
```

### Async handler

```csharp
_sub = context.Events.SubscribeAsync<UserCreatedEvent>(async e =>
{
    await _emailService.SendWelcomeAsync(e.Email);
});
```

---

## IEventSubscription

`Subscribe` and `SubscribeAsync` return `IEventSubscription`, which implements `IDisposable`. Disposing it unsubscribes the handler:

```csharp
public interface IEventSubscription : IDisposable
{
    Type EventType { get; }
    bool IsDisposed { get; }
}
```

A common pattern is to collect subscriptions and dispose them all on stop:

```csharp
private readonly List<IEventSubscription> _subscriptions = [];

public Task OnStartAsync(LibraryContext context)
{
    _subscriptions.Add(context.Events.Subscribe<UserCreatedEvent>(OnUserCreated));
    _subscriptions.Add(context.Events.Subscribe<AlertEvent>(OnAlert));
    return Task.CompletedTask;
}

public Task OnStopAsync()
{
    foreach (var sub in _subscriptions) sub.Dispose();
    _subscriptions.Clear();
    return Task.CompletedTask;
}
```

---

## Thread Safety

`IEventBus` is thread-safe. Handlers are called on the thread that calls `Publish`. For `PublishAsync`, handlers are awaited sequentially.

If a handler throws, the exception is caught and logged — it does not propagate to the publisher (unless you use `PublishAsync` with strict mode, if configured).

---

## Framework Events

CodeLogic publishes these events internally:

| Event | Published when |
|-------|---------------|
| `LibraryStartedEvent` | A library completes `OnStartAsync` |
| `LibraryStoppedEvent` | A library completes `OnStopAsync` |
| `PluginLoadedEvent` | A plugin is loaded by `PluginManager` |
| `PluginUnloadedEvent` | A plugin is unloaded |
| `HealthCheckCompletedEvent` | A scheduled health check finishes |

Subscribe to these to observe framework lifecycle in your application.

---

## ComponentAlertEvent Pattern

A common pattern for cross-component alerts:

```csharp
public record ComponentAlertEvent(
    string ComponentId,
    string Message,
    AlertSeverity Severity,
    Exception? Exception = null
) : IEvent;

// In any library or plugin:
await context.Events.PublishAsync(new ComponentAlertEvent(
    ComponentId: Manifest.Id,
    Message:     "Queue depth critical",
    Severity:    AlertSeverity.Error
));

// In the application — aggregate and forward to monitoring:
_sub = context.Events.Subscribe<ComponentAlertEvent>(alert =>
{
    _monitor.SendAlert(alert.ComponentId, alert.Message, alert.Severity);
});
```
