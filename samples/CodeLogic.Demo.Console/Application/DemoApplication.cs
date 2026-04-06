using CodeLogic.Demo.Console.Config;
using CodeLogic.Demo.Console.Events;
using CodeLogic.Demo.Console.Localization;
using CodeLogic.Framework.Application;

namespace CodeLogic.Demo.Console.Application;

// ─────────────────────────────────────────────────────────────────────────────
// The consuming application — implements IApplication to participate in the
// CodeLogic lifecycle.
//
// Lifecycle order (guaranteed by the framework):
//   1. OnConfigureAsync  — register config + localization models
//   2. OnInitializeAsync — read loaded config, set up internal services
//   3. OnStartAsync      — begin processing, start workers, etc.
//   4. OnStopAsync       — graceful shutdown (called before process exits)
//
// Libraries are always started BEFORE the application.
// The application is always stopped BEFORE libraries.
// ─────────────────────────────────────────────────────────────────────────────

public class DemoApplication : IApplication
{
    // ── Manifest ──────────────────────────────────────────────────────────
    // Metadata about this application. Id must be unique.
    public ApplicationManifest Manifest { get; } = new()
    {
        Id          = "demo.console",
        Name        = "CodeLogic Console Demo",
        Version     = "1.0.0",
        Description = "Reference implementation showing the CodeLogic boot pattern",
        Author      = "CodeLogic"
    };

    // Internal state populated during Initialize
    private DemoConfig _config = new();
    private DemoStrings _strings = new();

    // ── Phase 1: Configure ────────────────────────────────────────────────
    // Called by the framework to register models BEFORE files are generated.
    // Do NOT read config here — it hasn't been loaded yet.
    public async Task OnConfigureAsync(ApplicationContext context)
    {
        // Register config models → generates data/app/config.json
        context.Configuration.Register<DemoConfig>();

        // Register localization → generates data/app/localization/demo.*.json
        context.Localization.Register<DemoStrings>();

        context.Logger.Info($"{Manifest.Name} configured");
        await Task.CompletedTask;
    }

    // ── Phase 2: Initialize ───────────────────────────────────────────────
    // Config and localization are loaded. Set up internal services here.
    public async Task OnInitializeAsync(ApplicationContext context)
    {
        // Read the loaded configuration
        _config = context.Configuration.Get<DemoConfig>();

        // Read localization for the default culture
        _strings = context.Localization.Get<DemoStrings>();

        context.Logger.Info($"{Manifest.Name} initialized — " +
                            $"title={_config.AppTitle}, batch={_config.MaxItemsPerBatch}");
        await Task.CompletedTask;
    }

    // ── Phase 3: Start ────────────────────────────────────────────────────
    // All libraries are running. Start your services here.
    public async Task OnStartAsync(ApplicationContext context)
    {
        context.Logger.Info($"{Manifest.Name} starting");

        // Announce startup via the event bus
        context.Events.Publish(new UserActionEvent("startup"));

        // Subscribe to events from within the application context
        context.Events.Subscribe<WorkCompletedEvent>(e =>
        {
            var status = e.Success ? "OK" : "FAILED";
            context.Logger.Info($"Work '{e.TaskName}' {status} in {e.Duration.TotalMilliseconds:0}ms");
        });

        // Print welcome using localized strings
        System.Console.WriteLine(string.Format(_strings.Welcome, _config.AppTitle));

        context.Logger.Info($"{Manifest.Name} started");
        await Task.CompletedTask;
    }

    // ── Phase 4: Stop ─────────────────────────────────────────────────────
    // Clean up resources. Called before process exits.
    public async Task OnStopAsync()
    {
        System.Console.WriteLine(_strings.Goodbye);
        await Task.CompletedTask;
    }
}
