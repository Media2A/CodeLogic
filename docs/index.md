---
_layout: landing
---

# CodeLogic 3

**CodeLogic 3** is an opinionated, zero-dependency .NET 10 framework for building structured, lifecycle-aware applications. It provides a strict boot sequence, a library system, a plugin system, configuration, localization, an event bus, and health checks — all wired together into a coherent whole.

---

## Three Layers, One Lifecycle

CodeLogic organises every application into three distinct layers, each participating in the same 4-phase lifecycle:

```
┌─────────────────────────────────────────────────────┐
│                    Your Application                  │  IApplication
│        (IApplication — runs after all libraries)    │
├─────────────────────────────────────────────────────┤
│              CL.* Libraries / Your Libraries         │  ILibrary
│         (started in dependency order first)         │
├─────────────────────────────────────────────────────┤
│          Plugins (optional, app-managed)             │  IPlugin
│       (hot-reloadable, loaded by PluginManager)     │
└─────────────────────────────────────────────────────┘
```

**Libraries** provide services (database, email, storage, etc.) and are fully started before your application touches them.  
**Your Application** implements `IApplication` and is the entry point for your business logic — it runs after all libraries are ready.  
**Plugins** are optional hot-reloadable components managed by a `PluginManager` that your application owns.

---

## The 4-Phase Lifecycle

Every component — libraries, the application, and plugins — follows the same four phases:

| Phase | Method | Purpose |
|-------|--------|---------|
| 1 — Configure | `OnConfigureAsync` | Register config and localization models. Do not read config yet. |
| 2 — Initialize | `OnInitializeAsync` | Read config, open connections, set up services. |
| 3 — Start | `OnStartAsync` | Begin background tasks, subscribe to events, start processing. |
| 4 — Stop | `OnStopAsync` | Flush work, stop tasks, close connections, release resources. |

### Exact startup order

The framework guarantees this ordering across a single `ConfigureAsync()` + `StartAsync()` call pair:

```
ConfigureAsync():
  └─ application.OnConfigureAsync()        ← app registers its config models

StartAsync():
  ├─ libraries: OnConfigureAsync() x N     ← all libs register their config models
  ├─ libraries: OnInitializeAsync() x N    ← in dependency order
  ├─ libraries: OnStartAsync() x N         ← all libs fully running
  ├─ application.OnInitializeAsync()       ← app can now use library services
  └─ application.OnStartAsync()            ← app is running

StopAsync():
  ├─ application.OnStopAsync()             ← app stops first
  ├─ plugins: OnUnloadAsync() x N          ← plugins unload before libraries
  └─ libraries: OnStopAsync() x N          ← in reverse start order
```

This ordering guarantees that library services are always available when the application starts, and the application is always fully stopped before its dependencies are torn down.

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

if (result.ShouldExit) return;   // handles --version, --info, --generate-configs, etc.

// 2. Register libraries
await Libraries.LoadAsync<MyDatabaseLibrary>();
await Libraries.LoadAsync<MyEmailLibrary>();

// 3. Register your application
CodeLogic.RegisterApplication(new MyApp());

// 4. Configure — app's OnConfigureAsync runs, config files generated/loaded
await CodeLogic.ConfigureAsync();

// 5. Start — libraries fully started, then app Initialize + Start
await CodeLogic.StartAsync();

if (result.RunHealthCheck)
{
    var report = await CodeLogic.GetHealthAsync();
    Console.WriteLine(report.ToConsoleString());
    return;
}

await Task.Delay(Timeout.Infinite);
```

---

## Implementing IApplication

Your application is the centrepiece — it gets the same scoped services (logger, config, localization, event bus) as any library, but always starts after all libraries are ready:

```csharp
public class MyApp : IApplication
{
    public ApplicationManifest Manifest => new()
    {
        Id          = "MyApp",
        Name        = "My Application",
        Version     = "1.0.0",
        Description = "Does something useful"
    };

    private ApplicationContext _ctx = null!;

    // Phase 1: register your config models (called during ConfigureAsync)
    public Task OnConfigureAsync(ApplicationContext context)
    {
        context.Configuration.Register<MyAppConfig>();
        return Task.CompletedTask;
    }

    // Phase 2: libraries are fully running — resolve and set up your services
    public async Task OnInitializeAsync(ApplicationContext context)
    {
        _ctx = context;
        var config = context.Configuration.Get<MyAppConfig>();

        // Access a fully-started library service
        var db = Libraries.Get<MyDatabaseLibrary>();

        context.Logger.Info($"Initialized with config: {config.SomeSetting}");
    }

    // Phase 3: start background work, subscribe to events
    public Task OnStartAsync(ApplicationContext context)
    {
        context.Events.Subscribe<SomeLibraryEvent>(OnSomeEvent);
        context.Logger.Info("Application started");
        return Task.CompletedTask;
    }

    // Phase 4: clean up before libraries stop
    public Task OnStopAsync()
    {
        _ctx.Logger.Info("Application stopping");
        return Task.CompletedTask;
    }

    private void OnSomeEvent(SomeLibraryEvent e) { /* handle */ }
}
```

---

## Key Features

| Feature | Description |
|---------|-------------|
| **IApplication** | Your app participates in the framework lifecycle — runs after all libraries |
| **Library system** | Self-contained services with manifest, dependencies, and full lifecycle |
| **Plugin system** | Hot-reloadable assemblies via `PluginManager` owned by your application |
| **Configuration** | Per-component JSON files with auto-generation and DataAnnotations validation |
| **Localization** | Per-component locale files with culture fallback |
| **Event bus** | Shared `IEventBus` across libraries, application, and plugins |
| **Health checks** | `HealthCheckAsync()` on every component, aggregated into a `HealthReport` |
| **CLI flags** | Built-in `--version`, `--health`, `--generate-configs`, `--dry-run`, and more |

---

## Directory Layout

```
CodeLogic/
  CodeLogic.json              ← framework config
  Framework/
    logs/                     ← framework-level logs
  Application/
    config.myapp.json         ← your app's config (auto-generated)
    localization/
    logs/
    data/
  Libraries/
    MyApp.Database/
      config.database.json    ← library config (auto-generated)
      logs/
      data/
  Plugins/
    MyApp.Dashboard/
      MyApp.Dashboard.dll
      config.dashboard.json
```

---

## Next Steps

- [Getting Started](articles/getting-started.md) — full installation and first-run walkthrough
- [Application Lifecycle](articles/application-lifecycle.md) — IApplication, ApplicationContext, and ordering in depth
- [Library Lifecycle](articles/library-lifecycle.md) — the 4 phases for ILibrary in depth
- [Configuration](articles/configuration.md) — config models and file naming
- [Event Bus](articles/event-bus.md) — publish and subscribe to events
- [Plugins](articles/plugins.md) — hot-reloadable plugin system
- [API Reference](api/index.md) — full generated API documentation
