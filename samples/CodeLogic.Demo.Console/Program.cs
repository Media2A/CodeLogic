using CodeLogic;
using CodeLogic.Demo.Console.Application;
using CodeLogic.Demo.Console.Config;
using CodeLogic.Demo.Console.Events;
using CodeLogic.Demo.Console.Localization;

// ─────────────────────────────────────────────────────────────────────────────
// Program.cs — Entry point. Boot sequence only.
//
// Responsibilities:
//   - Configure CodeLogic paths and options
//   - Register the application
//   - Run the 3-step startup: Configure → Start
//   - Hand off to the main loop
//   - Ensure graceful shutdown
//
// Nothing domain-specific lives here. Everything is delegated to typed classes.
// ─────────────────────────────────────────────────────────────────────────────

// ── Step 1: Initialize the framework ──────────────────────────────────────
// Sets up paths, reads CLI args, scaffolds directories on first run,
// loads CodeLogic.json. Does NOT start any libraries yet.

var initResult = await CodeLogic.CodeLogic.InitializeAsync(opts =>
{
    opts.FrameworkRootPath    = "data/codelogic"; // framework configs + lib data
    opts.ApplicationRootPath  = "data/app";        // app config + localization
    opts.AppVersion           = "1.0.0";
    opts.HandleShutdownSignals = true;             // hooks CTRL+C → StopAsync()
});

if (!initResult.Success || initResult.ShouldExit)
{
    Console.Error.WriteLine($"Startup failed: {initResult.Message}");
    return;
}

// ── Step 2: Register the application ──────────────────────────────────────
// Tells the framework which IApplication to run. Must be called before
// ConfigureAsync so the app participates in config/localization generation.

CodeLogic.CodeLogic.RegisterApplication(new DemoApplication());

// ── Step 3: Configure ─────────────────────────────────────────────────────
// Discovers libraries (if any), runs OnConfigureAsync on all of them,
// generates config/localization files that don't exist yet, loads them all.

await CodeLogic.CodeLogic.ConfigureAsync();

// ── Step 4: Start ─────────────────────────────────────────────────────────
// Initializes + starts all libraries, then initializes + starts the app.
// After this returns, everything is running and ready.

await CodeLogic.CodeLogic.StartAsync();

// ── Main loop ─────────────────────────────────────────────────────────────
// At this point the framework is fully running. The app can do its work.

var ctx    = CodeLogic.CodeLogic.GetApplicationContext()!;
var config = ctx.Configuration.Get<DemoConfig>();
var bus    = CodeLogic.CodeLogic.GetEventBus();

Console.WriteLine($"\nMachine : {CodeLogicEnvironment.MachineName}");
Console.WriteLine($"Version : {CodeLogicEnvironment.AppVersion}");
Console.WriteLine($"Debug   : {CodeLogicEnvironment.IsDebugging}");
Console.WriteLine($"Batch   : {config.MaxItemsPerBatch} items");
Console.WriteLine();

// Subscribe to work events from Program scope
using var workSub = bus.Subscribe<WorkCompletedEvent>(e =>
{
    var icon = e.Success ? "+" : "x";
    Console.WriteLine($"  [{icon}] {e.TaskName} — {e.Duration.TotalMilliseconds:0}ms");
});

Console.WriteLine("Commands: [W] Do work   [H] Health   [Q] Quit\n");

// Simple REPL loop
bool running = true;
while (running)
{
    if (!Console.KeyAvailable)
    {
        await Task.Delay(50);
        continue;
    }

    var key = Console.ReadKey(intercept: true).Key;
    switch (key)
    {
        case ConsoleKey.W:
            // Simulate work and publish a result event
            bus.Publish(new UserActionEvent("do-work"));
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.Delay(config.WorkIntervalMs); // simulated work
            sw.Stop();
            bus.Publish(new WorkCompletedEvent("SimulatedBatch", sw.Elapsed, Success: true));
            break;

        case ConsoleKey.H:
            // Print a live health report
            var health = await CodeLogic.CodeLogic.GetHealthAsync();
            Console.WriteLine(health.ToConsoleString());
            break;

        case ConsoleKey.Q:
            running = false;
            break;
    }
}

// ── Step 5: Graceful shutdown ──────────────────────────────────────────────
// Stops the app first, then all libraries in reverse dependency order.

await CodeLogic.CodeLogic.StopAsync();
