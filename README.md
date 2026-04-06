# CodeLogic 3

A modular .NET 10 framework for building structured, lifecycle-managed applications with clean separation between core infrastructure, framework orchestration, and library integrations.

## Architecture

```
CodeLogic/
├── src/
│   ├── Core/               # Pure infrastructure engines — no framework coupling
│   │   ├── Logging/        # ILogger, Logger, LogLevel, LoggingOptions
│   │   ├── Configuration/  # IConfigurationManager, ConfigModelBase
│   │   ├── Localization/   # ILocalizationManager, LocalizationModelBase
│   │   └── Utilities/      # SemanticVersion, StartupValidator, FirstRunManager
│   │
│   ├── Framework/          # Lifecycle orchestration — uses Core
│   │   ├── Libraries/      # ILibrary, LibraryContext, LibraryManager
│   │   ├── Application/    # IApplication, ApplicationContext
│   │   └── Plugins/        # IPlugin, PluginContext, PluginManager (hot-reload)
│   │
│   └── CodeLogic.cs        # Static facade + ICodeLogicRuntime (injectable)
│
├── Libs/                   # Official CL.* integrations
│   ├── CL.Core/            # Shared utilities (hashing, caching, imaging, etc.)
│   ├── CL.SQLite/          # SQLite with LINQ query builder + table sync
│   ├── CL.MySQL2/          # MySQL with full ORM-like features
│   ├── CL.PostgreSQL/      # PostgreSQL integration
│   ├── CL.Mail/            # SMTP/IMAP + template engine
│   ├── CL.SystemStats/     # Cross-platform CPU/memory/process stats
│   ├── CL.GitHelper/       # Git repository management
│   ├── CL.NetUtils/        # DNS, IP geolocation, DNSBL
│   ├── CL.SocialConnect/   # Discord webhooks, Steam API
│   ├── CL.StorageS3/       # Amazon S3 storage
│   └── CL.TwoFactorAuth/   # TOTP 2FA + QR code generation
│
├── docs/                   # Documentation per layer
├── samples/                # Example applications
└── CodeLogic.sln
```

## Key Design Principles

**Core is standalone.** `Logging`, `Configuration`, and `Localization` have zero dependencies on the framework lifecycle. You can use them in any .NET app without CodeLogic at all.

**Framework wires Core into lifecycle.** `LibraryContext` and `ApplicationContext` bundle Core engines and hand them to libraries/apps at the right lifecycle phase — nothing gets constructed until it's needed.

**Static facade, injectable runtime.** The `CodeLogic` static class is a convenience facade. For testing or advanced scenarios, use `ICodeLogicRuntime` directly via DI.

**Plugins are first-class.** The plugin system is on par with libraries — same 4-phase lifecycle, same access to Core engines (config, logging, localization).

## Lifecycle

```
InitializeAsync()       — load CodeLogic.json, validate
RegisterApplication()   — declare the consuming app
ConfigureAsync()        — discover/load libraries, run OnConfigureAsync on all
                          (generates config + localization files)
StartAsync()            — OnInitializeAsync → OnStartAsync on all
                          (app starts after libraries)

--- application runs ---

StopAsync()             — OnStopAsync in reverse order
                          (app stops before libraries)
```

## Quick Start

```csharp
// Program.cs
await CodeLogic.InitializeAsync(opts => opts.RootDirectory = "data/codelogic");

CodeLogic.RegisterApplication(new MyApplication());

await CodeLogic.ConfigureAsync();
await Libraries.LoadAsync<SQLiteLibrary>();
await CodeLogic.GetLibraryManager()!.ConfigureAllAsync();
await CodeLogic.StartAsync();

// ... run your app ...

await CodeLogic.StopAsync();
```

## Building a Library

```csharp
[LibraryManifest(Id = "cl.mylib", Name = "CL.MyLib", Version = "1.0.0")]
public class MyLibrary : ILibrary
{
    public LibraryManifest Manifest { get; } = new() { Id = "cl.mylib", ... };

    public async Task OnConfigureAsync(LibraryContext context)
    {
        context.Configuration.Register<MyLibConfig>();
        context.Localization.Register<MyLibStrings>();
    }

    public async Task OnInitializeAsync(LibraryContext context)
    {
        var config = context.Configuration.Get<MyLibConfig>();
        // initialize services using config
    }

    public async Task OnStartAsync(LibraryContext context) { }
    public async Task OnStopAsync() { }
    public async Task<HealthStatus> HealthCheckAsync() => HealthStatus.Healthy("OK");
    public void Dispose() { }
}
```

## Building an Application

```csharp
public class MyApplication : IApplication
{
    public ApplicationManifest Manifest { get; } = new()
    {
        Id = "myapp", Name = "My App", Version = "1.0.0"
    };

    public async Task OnConfigureAsync(ApplicationContext context)
    {
        context.Configuration.Register<AppSettings>();
        context.Localization.Register<AppStrings>();
    }

    public async Task OnInitializeAsync(ApplicationContext context)
    {
        var settings = context.Configuration.Get<AppSettings>();
    }

    public async Task OnStartAsync(ApplicationContext context) { }
    public async Task OnStopAsync() { }
}
```

## Building a Plugin

Plugins are hot-loadable at runtime — no restart required.

```csharp
public class MyPlugin : IPlugin
{
    public string Id => "my.plugin";
    public string Name => "My Plugin";
    public string Version => "1.0.0";
    public string? Description => "Does something cool";
    public string? Author => "You";
    public bool IsLoaded { get; private set; }

    public async Task OnLoadAsync(PluginContext context)
    {
        var config = context.Configuration.Get<MyPluginConfig>();
        IsLoaded = true;
    }

    public async Task OnUnloadAsync() { IsLoaded = false; }
    public async Task<HealthStatus> HealthCheckAsync() => HealthStatus.Healthy("OK");
    public void Dispose() { }
}
```

## License

MIT — see [LICENSE](LICENSE)
