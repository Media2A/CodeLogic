---
_layout: landing
---

# CodeLogic 3

**CodeLogic 3** is an opinionated, zero-dependency .NET 10 modular framework for building structured, lifecycle-aware applications. It provides libraries, plugins, configuration, localization, an event bus, and health checks — all wired together through a strict boot sequence.

---

## What is CodeLogic 3?

CodeLogic 3 is a host framework that brings discipline to application composition. Instead of wiring up services manually with DI containers, you define self-contained **libraries** that each own their configuration, localization, logging, and lifecycle. The framework then orchestrates them in a deterministic order.

Key design goals:

- **Zero external NuGet dependencies** in the core framework
- **Strict lifecycle contract** — every component goes through the same 4-phase sequence
- **Per-component config and localization files** — no shared `appsettings.json` sprawl
- **Shared event bus** across all libraries, the application, and plugins
- **Hot-reloadable plugins** via separate `AssemblyLoadContext` instances

---

## The 4-Phase Library Lifecycle

Every library and plugin follows the same four phases, called in strict order:

| Phase | Method | Purpose |
|-------|--------|---------|
| 1 — Configure | `OnConfigureAsync` | Register config and localization models. Do not read config yet. |
| 2 — Initialize | `OnInitializeAsync` | Read config, validate, open connections, set up services. |
| 3 — Start | `OnStartAsync` | Begin background tasks, start processing. |
| 4 — Stop | `OnStopAsync` | Gracefully stop tasks, flush, close connections, release resources. |

The framework calls Configure on all libraries, then Initialize on all libraries (in dependency order), then Start on all libraries — ensuring every dependency is fully initialized before its consumer starts.

---

## Key Features

| Feature | Description |
|---------|-------------|
| **Library system** | Self-contained services with manifest, dependency declarations, and full lifecycle |
| **Plugin system** | Hot-reloadable assemblies via `PluginManager` with `FileSystemWatcher` support |
| **Configuration** | Per-component JSON files with auto-generation, DataAnnotations validation, and reload |
| **Localization** | Per-component locale files with culture fallback and hot-reload |
| **Event bus** | Shared publish/subscribe bus (`IEventBus`) across all components |
| **Health checks** | `HealthCheckAsync()` on every component, aggregated into a `HealthReport` |
| **CLI flags** | Built-in `--version`, `--health`, `--generate-configs`, `--dry-run`, and more |

---

## Quick Start

```csharp
// Program.cs
using CodeLogic;

// 1. Initialize — parse CLI args, scaffold directories, load CodeLogic.json
var result = await CodeLogic.InitializeAsync(o =>
{
    o.AppVersion = "1.0.0";
});

// --version, --generate-configs, etc. set ShouldExit
if (result.ShouldExit) return;

// 2. Register libraries
await Libraries.LoadAsync<MyDatabaseLibrary>();
await Libraries.LoadAsync<MyEmailLibrary>();

// 3. Register the application
CodeLogic.RegisterApplication(new MyApp());

// 4. Configure — generates/loads all config and localization files
await CodeLogic.ConfigureAsync();

// 5. Start — Initialize then Start all libraries, then run the application
await CodeLogic.StartAsync();

if (result.RunHealthCheck)
{
    var report = await CodeLogic.GetHealthAsync();
    Console.WriteLine(report.ToConsoleString());
    return;
}

await Task.Delay(Timeout.Infinite);
```

A minimal library looks like this:

```csharp
public class MyDatabaseLibrary : ILibrary
{
    public LibraryManifest Manifest => new()
    {
        Id = "MyApp.Database",
        Name = "Database Library",
        Version = "1.0.0"
    };

    private MyDbConfig _config = null!;

    public Task OnConfigureAsync(LibraryContext context)
    {
        context.Configuration.Register<MyDbConfig>();
        return Task.CompletedTask;
    }

    public async Task OnInitializeAsync(LibraryContext context)
    {
        _config = context.Configuration.Get<MyDbConfig>();
        // validate, open connection pool, etc.
    }

    public Task OnStartAsync(LibraryContext context) => Task.CompletedTask;
    public Task OnStopAsync() => Task.CompletedTask;

    public Task<HealthStatus> HealthCheckAsync()
        => Task.FromResult(HealthStatus.Healthy("Connected"));

    public void Dispose() { }
}
```

---

## Directory Layout

After the first run, CodeLogic creates a `CodeLogic/` directory next to your executable:

```
CodeLogic/
  CodeLogic.json           ← framework config
  Libraries/
    MyApp.Database/
      config.database.json ← MyDbConfig
      logs/
      data/
  Plugins/
  logs/
```

---

## Next Steps

- [Getting Started](articles/getting-started.md) — full installation and first-run walkthrough
- [Library Lifecycle](articles/library-lifecycle.md) — the 4 phases in depth
- [Configuration](articles/configuration.md) — config models and file naming
- [Event Bus](articles/event-bus.md) — publish and subscribe to events
- [API Reference](api/index.md) — full API documentation
