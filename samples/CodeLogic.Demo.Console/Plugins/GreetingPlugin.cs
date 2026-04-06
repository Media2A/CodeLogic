using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Demo.Console.Events;
using CodeLogic.Framework.Application.Plugins;
using CodeLogic.Framework.Libraries;

namespace CodeLogic.Demo.Console.Plugins;

// ─────────────────────────────────────────────────────────────────────────────
// GreetingPlugin — a simple plugin that:
//   - Has its own config (greeting message, repeat count)
//   - Subscribes to WorkCompletedEvent via the shared event bus
//   - Logs a greeting every time work completes successfully
//   - Demonstrates the full 4-phase plugin lifecycle
//
// Note: In a real app, plugins would be separate DLL files loaded at runtime
// from the Plugins/ directory. In this demo they are in-process classes to
// keep things simple and compilable without extra projects.
// ─────────────────────────────────────────────────────────────────────────────

public class GreetingPlugin : IPlugin
{
    // ── Manifest ─────────────────────────────────────────────────────────────
    public PluginManifest Manifest { get; } = new()
    {
        Id          = "demo.greeting",
        Name        = "Greeting Plugin",
        Version     = "1.0.0",
        Description = "Logs a greeting when work completes successfully",
        Author      = "CodeLogic Demo"
    };

    public PluginState State { get; private set; } = PluginState.Loaded;

    // Internal state
    private GreetingConfig _config = new();
    private IEventSubscription? _workSub;
    private int _greetingsSent;

    // ── Phase 1: Configure ────────────────────────────────────────────────────
    // Register config models — generates data/codelogic/Plugins/demo.greeting/config.json
    public async Task OnConfigureAsync(PluginContext context)
    {
        context.Configuration.Register<GreetingConfig>();

        context.Logger.Trace("GreetingPlugin: OnConfigureAsync");
        context.Logger.Info($"{Manifest.Name} configured");
        State = PluginState.Configured;
        await Task.CompletedTask;
    }

    // ── Phase 2: Initialize ───────────────────────────────────────────────────
    // Read loaded config — set up internal state
    public async Task OnInitializeAsync(PluginContext context)
    {
        _config = context.Configuration.Get<GreetingConfig>();

        context.Logger.Debug($"GreetingPlugin: greeting='{_config.Greeting}', " +
                             $"maxGreetings={_config.MaxGreetings}");
        context.Logger.Info($"{Manifest.Name} initialized");
        State = PluginState.Initialized;
        await Task.CompletedTask;
    }

    // ── Phase 3: Start ────────────────────────────────────────────────────────
    // Subscribe to events — begin plugin work
    public async Task OnStartAsync(PluginContext context)
    {
        context.Logger.Debug("GreetingPlugin: subscribing to WorkCompletedEvent");

        // Subscribe to work events via the shared event bus
        _workSub = context.Events.Subscribe<WorkCompletedEvent>(e =>
        {
            if (!e.Success) return;
            if (_config.MaxGreetings > 0 && _greetingsSent >= _config.MaxGreetings) return;

            _greetingsSent++;
            context.Logger.Info(
                $"{_config.Greeting} (task='{e.TaskName}', " +
                $"#{_greetingsSent}/{(_config.MaxGreetings > 0 ? _config.MaxGreetings : "∞")})");

            // Also write to console so it's visible in the demo
            System.Console.WriteLine(
                $"  [GreetingPlugin] {_config.Greeting} — '{e.TaskName}' done in {e.Duration.TotalMilliseconds:0}ms");
        });

        context.Logger.Info($"{Manifest.Name} started — listening for work events");
        State = PluginState.Started;
        await Task.CompletedTask;
    }

    // ── Phase 4: Unload ───────────────────────────────────────────────────────
    public async Task OnUnloadAsync()
    {
        _workSub?.Dispose();
        System.Console.WriteLine($"  [GreetingPlugin] Unloaded — sent {_greetingsSent} greeting(s)");
        State = PluginState.Stopped;
        await Task.CompletedTask;
    }

    // ── Health check ──────────────────────────────────────────────────────────
    public Task<HealthStatus> HealthCheckAsync()
    {
        var msg = $"Sent {_greetingsSent} greeting(s), max={(_config.MaxGreetings > 0 ? _config.MaxGreetings : "unlimited")}";
        return Task.FromResult(HealthStatus.Healthy(msg));
    }

    public void Dispose() => _workSub?.Dispose();
}

// ── Plugin config ─────────────────────────────────────────────────────────────

public class GreetingConfig : ConfigModelBase
{
    /// <summary>The greeting message to log when work completes.</summary>
    public string Greeting { get; set; } = "Great job!";

    /// <summary>Maximum number of greetings to send. 0 = unlimited.</summary>
    public int MaxGreetings { get; set; } = 0;
}
