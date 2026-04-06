# Getting Started

This guide walks you through installing CodeLogic 3, understanding the directory layout, and writing your first library and application.

---

## Prerequisites

- .NET 10 SDK
- A console application project (`OutputType=Exe`)

---

## Installation

CodeLogic 3 is distributed as a source project reference. Clone or reference it:

```xml
<!-- YourApp.csproj -->
<ItemGroup>
  <ProjectReference Include="path/to/CodeLogic/src/CodeLogic.csproj" />
</ItemGroup>
```

Your project must target `net10.0`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

---

## The 5-Step Boot Sequence

Every CodeLogic application follows the same five steps in `Program.cs`:

```csharp
using CodeLogic;

// Step 1: Initialize
var result = await CodeLogic.InitializeAsync(o =>
{
    o.AppVersion = "1.0.0";
    o.FrameworkRootPath = "CodeLogic";   // optional, this is the default
});

if (result.ShouldExit) return;   // handles --version, --generate-configs, etc.

// Step 2: Register libraries
await Libraries.LoadAsync<MyDatabaseLibrary>();

// Step 3: Register the application
CodeLogic.RegisterApplication(new MyApp());

// Step 4: Configure (generates/loads all config and localization files)
await CodeLogic.ConfigureAsync();

// Step 5: Start
await CodeLogic.StartAsync();

// Optional: run health check from --health flag
if (result.RunHealthCheck)
{
    var report = await CodeLogic.GetHealthAsync();
    Console.WriteLine(report.ToConsoleString());
    return;
}

await Task.Delay(Timeout.Infinite);
```

### What each step does

| Step | Method | What happens |
|------|--------|--------------|
| 1 | `InitializeAsync` | Parses CLI args, runs first-run scaffold, loads `CodeLogic.json`, creates runtime |
| 2 | `Libraries.LoadAsync<T>` | Registers library types with the `LibraryManager` |
| 3 | `RegisterApplication` | Registers your `IApplication` implementation |
| 4 | `ConfigureAsync` | Calls `OnConfigureAsync` on all components, generates and loads all config files |
| 5 | `StartAsync` | Calls `OnInitializeAsync` then `OnStartAsync` on all components in order |

---

## First Run

On first run, CodeLogic creates the `CodeLogic/` directory next to your executable and scaffolds the required files:

```
CodeLogic/
  CodeLogic.json           ← framework configuration
  logs/                    ← framework-level logs
  Libraries/               ← one subdirectory per library
    MyApp.Database/
      config.database.json ← auto-generated config file (edit and restart)
      logs/
      data/
  Plugins/                 ← plugin directories
```

Config files are generated with default values on first run. The application exits with a message instructing you to review and edit them, then restart.

Use `--generate-configs --force` to regenerate configs without exiting, overwriting existing ones.

---

## Your First Library

```csharp
using CodeLogic.Core;
using CodeLogic.Framework;

public class MyDatabaseLibrary : ILibrary
{
    public LibraryManifest Manifest => new()
    {
        Id          = "MyApp.Database",
        Name        = "Database Library",
        Version     = "1.0.0",
        Description = "Manages the application database connection"
    };

    private ILogger _logger = null!;
    private MyDbConfig _config = null!;

    // Phase 1: Register config and localization models
    public Task OnConfigureAsync(LibraryContext context)
    {
        context.Configuration.Register<MyDbConfig>();
        return Task.CompletedTask;
    }

    // Phase 2: Read config, open connections
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _logger = context.Logger;
        _config = context.Configuration.Get<MyDbConfig>();

        _logger.LogInformation("Connecting to {ConnectionString}", _config.ConnectionString);
        // open connection pool, validate schema, etc.
    }

    // Phase 3: Start background tasks
    public Task OnStartAsync(LibraryContext context)
    {
        _logger.LogInformation("Database library started");
        return Task.CompletedTask;
    }

    // Phase 4: Graceful shutdown
    public Task OnStopAsync()
    {
        _logger.LogInformation("Database library stopping");
        // close connections, flush, release
        return Task.CompletedTask;
    }

    public Task<HealthStatus> HealthCheckAsync()
        => Task.FromResult(HealthStatus.Healthy("Connected"));

    public void Dispose() { }
}
```

---

## Your First Application

```csharp
using CodeLogic.Framework;

public class MyApp : IApplication
{
    public ApplicationManifest Manifest => new()
    {
        Id      = "MyApp",
        Name    = "My Application",
        Version = "1.0.0"
    };

    public Task OnConfigureAsync(ApplicationContext context)
    {
        // Register app-level config here
        return Task.CompletedTask;
    }

    public async Task RunAsync(ApplicationContext context)
    {
        context.Logger.LogInformation("Application running");

        // Subscribe to events
        using var sub = context.Events.Subscribe<MyEvent>(e =>
            context.Logger.LogInformation("Received: {Value}", e.Value));

        // Main application loop
        await Task.Delay(Timeout.Infinite, context.CancellationToken);
    }

    public Task<HealthStatus> HealthCheckAsync()
        => Task.FromResult(HealthStatus.Healthy("Running"));
}
```

---

## CLI Flags

CodeLogic provides built-in CLI flags:

| Flag | Description |
|------|-------------|
| `--version` | Print version and exit |
| `--info` | Print framework and library info |
| `--health` | Run health checks and print report |
| `--generate-configs` | Generate missing config files and exit |
| `--generate-configs --force` | Regenerate all config files (overwrites) |
| `--dry-run` | Initialize and configure only, do not start |

---

## Next Steps

- [Library Lifecycle](library-lifecycle.md) — the 4 phases explained in depth
- [Configuration](configuration.md) — config models and file naming
- [Localization](localization.md) — locale files and culture fallback
- [Event Bus](event-bus.md) — publish and subscribe to events
- [Health Checks](health-checks.md) — implementing and querying health
- [Plugins](plugins.md) — hot-reloadable plugin system
