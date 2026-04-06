using CodeLogic;
using CodeLogic.Core.Events;
using CodeLogic.Demo.Web.Application;
using CodeLogic.Demo.Web.Endpoints;

// ─────────────────────────────────────────────────────────────────────────────
// Program.cs — Entry point. Boot sequence + host configuration only.
//
// CodeLogic boots BEFORE the ASP.NET host so libraries and app context are
// ready before the first HTTP request. The ASP.NET host then gets access
// to the already-initialized framework via DI registration.
// ─────────────────────────────────────────────────────────────────────────────

// ── Step 1: Initialize CodeLogic ──────────────────────────────────────────
// Must happen before WebApplication.CreateBuilder so that all config,
// localization, and library state is ready when the host starts.

var clResult = await CodeLogic.CodeLogic.InitializeAsync(opts =>
{
    opts.FrameworkRootPath     = "data/codelogic";
    opts.ApplicationRootPath   = "data/app";
    opts.AppVersion            = "1.0.0";
    opts.HandleShutdownSignals = false; // ASP.NET Core manages CTRL+C
});

if (!clResult.Success || clResult.ShouldExit)
{
    Console.Error.WriteLine($"CodeLogic init failed: {clResult.Message}");
    return;
}

// ── Step 2: Register the application ──────────────────────────────────────
CodeLogic.CodeLogic.RegisterApplication(new WebDemoApplication());

// ── Step 3: Configure + Start CodeLogic ───────────────────────────────────
await CodeLogic.CodeLogic.ConfigureAsync();
await CodeLogic.CodeLogic.StartAsync();

// ── Step 4: Build the ASP.NET host ────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// ── DI registrations ──────────────────────────────────────────────────────
// Expose the shared event bus so endpoints and services can inject it
// instead of calling the static CodeLogic.GetEventBus() directly.
// This makes them testable and decoupled.
builder.Services.AddSingleton<IEventBus>(CodeLogic.CodeLogic.GetEventBus());

// Optionally expose the runtime itself for advanced scenarios
builder.Services.AddSingleton<ICodeLogicRuntime>(provider =>
    // The static CodeLogic facade wraps the singleton runtime — we
    // can't easily extract it, so create a new one for DI use.
    // For real apps, pass the runtime instance directly.
    throw new InvalidOperationException(
        "Use CodeLogic.GetApplicationContext() directly for now, " +
        "or wire ICodeLogicRuntime from your own CodeLogicRuntime instance."));

// ── Step 5: Build the app ─────────────────────────────────────────────────
var app = builder.Build();

// ── Step 6: Map endpoints (each file registers its own routes) ────────────
HomeEndpoints.Map(app);
HealthEndpoints.Map(app);
EventEndpoints.Map(app);

// ── Step 7: Wire CodeLogic shutdown to ASP.NET host lifetime ─────────────
// When the host stops (CTRL+C, SIGTERM, etc.), stop CodeLogic gracefully.
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
    CodeLogic.CodeLogic.StopAsync().GetAwaiter().GetResult());

// ── Step 8: Run ───────────────────────────────────────────────────────────
app.Run();
