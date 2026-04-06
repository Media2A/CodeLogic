using CodeLogic.Demo.Web.Config;
using CodeLogic.Demo.Web.Events;
using CodeLogic.Demo.Web.Localization;
using CodeLogic.Framework.Application;

namespace CodeLogic.Demo.Web.Application;

// ─────────────────────────────────────────────────────────────────────────────
// The consuming application — implements IApplication for the web demo.
//
// Runs its lifecycle phases BEFORE the ASP.NET host starts handling requests:
//   Configure → Initialize → Start
//
// The application is stopped AFTER the ASP.NET host shuts down,
// hooked via IHostApplicationLifetime.ApplicationStopping in Program.cs.
// ─────────────────────────────────────────────────────────────────────────────

public class WebDemoApplication : IApplication
{
    public ApplicationManifest Manifest { get; } = new()
    {
        Id          = "demo.web",
        Name        = "CodeLogic Web Demo",
        Version     = "1.0.0",
        Description = "Reference implementation showing CodeLogic + ASP.NET Core",
        Author      = "CodeLogic"
    };

    // ── Phase 1: Configure ────────────────────────────────────────────────
    public async Task OnConfigureAsync(ApplicationContext context)
    {
        // Register all config models — generates config.json on first run
        context.Configuration.Register<WebConfig>();

        // Register localization — generates web.en-US.json, web.da-DK.json, etc.
        context.Localization.Register<WebStrings>();

        context.Logger.Info($"{Manifest.Name} configured");
        await Task.CompletedTask;
    }

    // ── Phase 2: Initialize ───────────────────────────────────────────────
    public async Task OnInitializeAsync(ApplicationContext context)
    {
        var config = context.Configuration.Get<WebConfig>();
        context.Logger.Info($"{Manifest.Name} initialized — site='{config.SiteTitle}'");
        await Task.CompletedTask;
    }

    // ── Phase 3: Start ────────────────────────────────────────────────────
    public async Task OnStartAsync(ApplicationContext context)
    {
        context.Logger.Info($"{Manifest.Name} starting");

        // Subscribe to incoming request events (published by endpoints)
        context.Events.Subscribe<RequestReceivedEvent>(e =>
            context.Logger.Debug($"Request: {e.Method} {e.Path}"));

        // Subscribe to app notifications
        context.Events.Subscribe<AppNotificationEvent>(e =>
            context.Logger.Info($"[{e.Severity.ToUpper()}] {e.Title}: {e.Message}"));

        // Announce startup
        context.Events.Publish(new AppNotificationEvent(
            "Startup", $"{Manifest.Name} is ready", "info"));

        context.Logger.Info($"{Manifest.Name} started");
        await Task.CompletedTask;
    }

    // ── Phase 4: Stop ─────────────────────────────────────────────────────
    public async Task OnStopAsync()
    {
        Console.WriteLine($"[{Manifest.Name}] Stopping gracefully...");
        await Task.CompletedTask;
    }
}
