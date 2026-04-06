using CodeLogic;
using CodeLogic.Demo.Console.Application;
using CodeLogic.Demo.Console.Config;
using CodeLogic.Demo.Console.Events;
using CodeLogic.Demo.Console.Localization;

// ─────────────────────────────────────────────────────────────────────────────
// Program.cs — Entry point. Boot sequence + main loop only.
// ─────────────────────────────────────────────────────────────────────────────

// ── Step 1: Initialize ────────────────────────────────────────────────────
var initResult = await CodeLogic.CodeLogic.InitializeAsync(opts =>
{
    opts.FrameworkRootPath     = "data/codelogic";
    opts.ApplicationRootPath   = "data/app";
    opts.AppVersion            = "1.0.0";
    opts.HandleShutdownSignals = true;
});

if (!initResult.Success || initResult.ShouldExit)
{
    Console.Error.WriteLine($"Startup failed: {initResult.Message}");
    return;
}

// ── Step 2: Register + Configure + Start ──────────────────────────────────
CodeLogic.CodeLogic.RegisterApplication(new DemoApplication());
await CodeLogic.CodeLogic.ConfigureAsync();
await CodeLogic.CodeLogic.StartAsync();

// ── Main loop ─────────────────────────────────────────────────────────────
var ctx    = CodeLogic.CodeLogic.GetApplicationContext()!;
var config = ctx.Configuration.Get<DemoConfig>();
var bus    = CodeLogic.CodeLogic.GetEventBus();

// Show where the log file is so the user knows where to look
var logFile = Path.Combine(
    Path.GetFullPath("data/app/logs"),
    "application.log");

Console.WriteLine($"\nMachine : {CodeLogicEnvironment.MachineName}");
Console.WriteLine($"Version : {CodeLogicEnvironment.AppVersion}");
Console.WriteLine($"Debug   : {CodeLogicEnvironment.IsDebugging}");
Console.WriteLine($"Log     : {logFile}");
Console.WriteLine($"Level   : {(CodeLogicEnvironment.IsDebugging ? "Debug (Development.json)" : "Warning (CodeLogic.json)")}");
Console.WriteLine();

// Subscribe to work completion events — displayed in console
using var workSub = bus.Subscribe<WorkCompletedEvent>(e =>
{
    var icon = e.Success ? "+" : "x";
    Console.WriteLine($"  [{icon}] {e.TaskName} — {e.Duration.TotalMilliseconds:0}ms");
});

Console.WriteLine("Commands:");
Console.WriteLine("  [W] Simulate work   (logs Info/Warning to application.log)");
Console.WriteLine("  [L] Log all levels  (shows what writes to disk vs filtered)");
Console.WriteLine("  [H] Health report");
Console.WriteLine("  [Q] Quit");
Console.WriteLine();

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
            // Simulate work — publishes WorkCompletedEvent which DemoApplication
            // subscribes to and logs at Info level → writes to application.log
            bus.Publish(new UserActionEvent("do-work"));
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Randomly succeed or fail to show both Info and Warning in logs
            await Task.Delay(config.WorkIntervalMs);
            sw.Stop();
            var success = Random.Shared.NextDouble() > 0.3; // 70% success rate
            bus.Publish(new WorkCompletedEvent("SimulatedBatch", sw.Elapsed, success));
            break;

        case ConsoleKey.L:
            // Write one entry at every log level.
            // With debugger (Development.json): ALL levels write to disk
            // Without debugger (CodeLogic.json): only Warning+ writes to disk
            Console.WriteLine("\n  Writing all log levels to application.log...");
            DemoApplication.LogAllLevels(ctx, "Demo");
            Console.WriteLine($"  Done. Check: {logFile}");
            Console.WriteLine($"  (Current level filter: " +
                $"{(CodeLogicEnvironment.IsDebugging ? "Trace+" : "Warning+")})\n");
            break;

        case ConsoleKey.H:
            var health = await CodeLogic.CodeLogic.GetHealthAsync();
            Console.WriteLine(health.ToConsoleString());
            break;

        case ConsoleKey.Q:
            running = false;
            break;
    }
}

// ── Graceful shutdown ──────────────────────────────────────────────────────
await CodeLogic.CodeLogic.StopAsync();
