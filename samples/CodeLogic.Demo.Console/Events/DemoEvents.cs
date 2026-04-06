using CodeLogic.Core.Events;

namespace CodeLogic.Demo.Console.Events;

// ─────────────────────────────────────────────────────────────────────────────
// Custom events for the console demo.
//
// Pattern:
//   - Define events as records implementing IEvent
//   - Events can carry any payload as record properties
//   - Subscribe via context.Events.Subscribe<T>() or CodeLogic.GetEventBus()
//   - Publish via context.Events.Publish(new MyEvent(...))
//
// For lib-to-lib communication without cross-references, use the built-in
// ComponentAlertEvent from CodeLogic.Core.Events.FrameworkEvents instead.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Raised when the demo application completes a work unit.
/// Demonstrates carrying structured payload in an event.
/// </summary>
public record WorkCompletedEvent(
    string TaskName,
    TimeSpan Duration,
    bool Success) : IEvent;

/// <summary>
/// Raised when the user triggers an action from the console menu.
/// </summary>
public record UserActionEvent(string Action) : IEvent;
