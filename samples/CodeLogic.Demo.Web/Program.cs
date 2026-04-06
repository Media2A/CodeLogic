using CodeLogic;
using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Framework.Application;

// ════════════════════════════════════════════════════════════════
//   CodeLogic 3 — ASP.NET Core Demo
//   Shows: CodeLogic boot before the host builds, DI integration,
//          health endpoint, config + localization in a request
// ════════════════════════════════════════════════════════════════

// ── 1. Boot CodeLogic BEFORE the ASP.NET host ─────────────────
//       CodeLogic runs its own lifecycle independently.
//       Libraries and app-level config/localization are ready
//       before the first HTTP request arrives.

var clResult = await CodeLogic.CodeLogic.InitializeAsync(opts =>
{
    opts.FrameworkRootPath    = "data/codelogic";
    opts.ApplicationRootPath  = "data/app";
    opts.AppVersion           = "1.0.0";
    opts.HandleShutdownSignals = false; // ASP.NET manages shutdown signals
});

if (!clResult.Success || clResult.ShouldExit)
{
    Console.Error.WriteLine($"CodeLogic init failed: {clResult.Message}");
    return;
}

CodeLogic.CodeLogic.RegisterApplication(new WebDemoApplication());
await CodeLogic.CodeLogic.ConfigureAsync();
await CodeLogic.CodeLogic.StartAsync();

// ── 2. Build ASP.NET host ──────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Register ICodeLogicRuntime for DI — lets controllers/services
// inject the runtime instead of using the static facade
builder.Services.AddSingleton<ICodeLogicRuntime>(new CodeLogicRuntime());

// Register the event bus so services can pub/sub
builder.Services.AddSingleton(CodeLogic.CodeLogic.GetEventBus());

var app = builder.Build();

// ── 3. Map endpoints ───────────────────────────────────────────

// Root — shows config + localization
app.MapGet("/", () =>
{
    var ctx = CodeLogic.CodeLogic.GetApplicationContext();
    if (ctx == null) return Results.Problem("CodeLogic not initialized");

    var config  = ctx.Configuration.Get<WebDemoConfig>();
    var strings = ctx.Localization.Get<WebDemoStrings>();

    return Results.Ok(new
    {
        title       = config.SiteTitle,
        welcome     = strings.Welcome,
        machine     = CodeLogicEnvironment.MachineName,
        appVersion  = CodeLogicEnvironment.AppVersion,
        isDebugging = CodeLogicEnvironment.IsDebugging
    });
});

// Health endpoint — aggregates all library + app health
app.MapGet("/health", async () =>
{
    var report = await CodeLogic.CodeLogic.GetHealthAsync();
    var status = report.IsHealthy ? Results.Ok(report.ToJson()) : Results.StatusCode(503);
    return status;
});

// Event bus demo — publish a custom event via HTTP
app.MapPost("/events/demo", (IEventBus bus) =>
{
    bus.Publish(new WebDemoEvent("Triggered via HTTP POST /events/demo"));
    return Results.Ok(new { message = "Event published" });
});

// Shutdown CodeLogic gracefully when ASP.NET stops
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
    CodeLogic.CodeLogic.StopAsync().GetAwaiter().GetResult());

// Subscribe to demo events — log them server-side
var eventBus = CodeLogic.CodeLogic.GetEventBus();
eventBus.Subscribe<WebDemoEvent>(e =>
    Console.WriteLine($"[EventBus] {e.Message}"));

// ── 4. Run ─────────────────────────────────────────────────────
app.Run();


// ── Application ───────────────────────────────────────────────────────────

class WebDemoApplication : IApplication
{
    public ApplicationManifest Manifest { get; } = new()
    {
        Id          = "demo.web",
        Name        = "CodeLogic Web Demo",
        Version     = "1.0.0",
        Description = "Demonstrates CodeLogic 3 with ASP.NET Core",
        Author      = "CodeLogic"
    };

    public async Task OnConfigureAsync(ApplicationContext context)
    {
        context.Configuration.Register<WebDemoConfig>();
        context.Localization.Register<WebDemoStrings>();
        context.Logger.Info("WebDemoApplication configured");
        await Task.CompletedTask;
    }

    public async Task OnInitializeAsync(ApplicationContext context)
    {
        context.Logger.Info("WebDemoApplication initialized");
        await Task.CompletedTask;
    }

    public async Task OnStartAsync(ApplicationContext context)
    {
        context.Logger.Info("WebDemoApplication started");
        context.Events.Publish(new WebDemoEvent("Web application has started!"));
        await Task.CompletedTask;
    }

    public async Task OnStopAsync()
    {
        Console.WriteLine("[WebApp] Stopping gracefully...");
        await Task.CompletedTask;
    }
}

// ── Custom event ──────────────────────────────────────────────────────────

record WebDemoEvent(string Message) : IEvent;

// ── Config model ──────────────────────────────────────────────────────────

class WebDemoConfig : ConfigModelBase
{
    public string SiteTitle { get; set; } = "CodeLogic Web Demo";
    public string DefaultLanguage { get; set; } = "en-US";
    public bool MaintenanceMode { get; set; } = false;
}

// ── Localization model ────────────────────────────────────────────────────

[LocalizationSection("web-demo")]
class WebDemoStrings : LocalizationModelBase
{
    public string Welcome { get; set; } = "Welcome to the CodeLogic web demo!";
    public string NotFound { get; set; } = "The requested resource was not found.";
    public string ServerError { get; set; } = "An internal error occurred.";
}
