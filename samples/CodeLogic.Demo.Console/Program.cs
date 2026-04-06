using CodeLogic;
using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Framework.Application;

// ════════════════════════════════════════════════════════════════
//   CodeLogic 3 — Console Demo
//   Shows: boot sequence, config, localization, event bus
// ════════════════════════════════════════════════════════════════

Console.WriteLine("════════════════════════════════════════════════════════");
Console.WriteLine("   CodeLogic 3.0 — Console Demo");
Console.WriteLine("════════════════════════════════════════════════════════\n");

// 1. Initialize — sets up paths, CLI args, first-run scaffold
var result = await CodeLogic.CodeLogic.InitializeAsync(opts =>
{
    opts.FrameworkRootPath    = "data/codelogic";
    opts.ApplicationRootPath  = "data/app";
    opts.AppVersion           = "1.0.0";
    opts.HandleShutdownSignals = true;
});

if (!result.Success || result.ShouldExit)
{
    Console.WriteLine($"Init: {result.Message}");
    return;
}

// 2. Register the demo application
CodeLogic.CodeLogic.RegisterApplication(new DemoApplication());

// 3. Configure — generates config/localization files, wires contexts
await CodeLogic.CodeLogic.ConfigureAsync();

// 4. Start — runs OnInitializeAsync + OnStartAsync on the app
await CodeLogic.CodeLogic.StartAsync();

Console.WriteLine("\n--- Framework started ---\n");

// 5. Use the event bus
var bus = CodeLogic.CodeLogic.GetEventBus();

using var sub = bus.Subscribe<DemoEvent>(e =>
    Console.WriteLine($"[EventBus] {e.Message}"));

bus.Publish(new DemoEvent("Hello from the event bus!"));

// 6. Read config and localization from application context
var ctx = CodeLogic.CodeLogic.GetApplicationContext()!;

var config = ctx.Configuration.Get<DemoConfig>();
Console.WriteLine($"[Config]        AppTitle={config.AppTitle}, MaxItems={config.MaxItems}");

var strings = ctx.Localization.Get<DemoStrings>();
Console.WriteLine($"[Localization]  Welcome={strings.Welcome}");

// 7. Environment info
Console.WriteLine($"[Environment]   Machine={CodeLogicEnvironment.MachineName}  " +
                  $"App={CodeLogicEnvironment.AppVersion}  " +
                  $"Debugging={CodeLogicEnvironment.IsDebugging}");

// 8. Health report
Console.WriteLine("\n[Health] Checking...");
var health = await CodeLogic.CodeLogic.GetHealthAsync();
Console.WriteLine(health.ToConsoleString());

// 9. Graceful shutdown
Console.WriteLine("Press any key to shut down...");
Console.ReadKey(intercept: true);

await CodeLogic.CodeLogic.StopAsync();
Console.WriteLine("\nGoodbye!");


// ── Application ───────────────────────────────────────────────────────────

class DemoApplication : IApplication
{
    public ApplicationManifest Manifest { get; } = new()
    {
        Id          = "demo.console",
        Name        = "CodeLogic Console Demo",
        Version     = "1.0.0",
        Description = "Demonstrates the CodeLogic 3 boot sequence",
        Author      = "CodeLogic"
    };

    public async Task OnConfigureAsync(ApplicationContext context)
    {
        context.Configuration.Register<DemoConfig>();
        context.Localization.Register<DemoStrings>();
        context.Logger.Info("DemoApplication configured");
        await Task.CompletedTask;
    }

    public async Task OnInitializeAsync(ApplicationContext context)
    {
        context.Logger.Info("DemoApplication initialized");
        await Task.CompletedTask;
    }

    public async Task OnStartAsync(ApplicationContext context)
    {
        context.Logger.Info("DemoApplication started");
        context.Events.Publish(new DemoEvent("Application has started!"));
        await Task.CompletedTask;
    }

    public async Task OnStopAsync()
    {
        Console.WriteLine("[App] Stopping gracefully...");
        await Task.CompletedTask;
    }
}

// ── Custom event ──────────────────────────────────────────────────────────

record DemoEvent(string Message) : IEvent;

// ── Config model ──────────────────────────────────────────────────────────

class DemoConfig : ConfigModelBase
{
    public string AppTitle { get; set; } = "My Demo App";
    public int MaxItems { get; set; } = 100;
    public bool EnableFeatureX { get; set; } = false;
}

// ── Localization model ────────────────────────────────────────────────────

[LocalizationSection("demo")]
class DemoStrings : LocalizationModelBase
{
    public string Welcome { get; set; } = "Welcome to CodeLogic!";
    public string Goodbye { get; set; } = "Goodbye!";
    public string ErrorOccurred { get; set; } = "An error occurred: {0}";
}
