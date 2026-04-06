# Getting Started with CodeLogic 3

CodeLogic is a modular .NET 10 framework for building structured applications with libraries, plugins, configuration, localization, events, and health checks. This guide takes you from zero to a running application.

---

## Installation

CodeLogic is distributed as a project reference (source library). Add it to your project:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/CodeLogic/src/CodeLogic.csproj" />
</ItemGroup>
```

Your project should target .NET 10:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

---

## The 5-Step Boot Sequence

Every CodeLogic application follows the same boot sequence:

```csharp
// Program.cs
using CodeLogic;

// Step 1: Initialize — parse CLI args, scaffold directories, load CodeLogic.json
var result = await CodeLogic.InitializeAsync(o =>
{
    o.AppVersion = "1.0.0";
    o.FrameworkRootPath = "CodeLogic";   // default
});

// Always check ShouldExit — handles --version, --generate-configs, etc.
if (result.ShouldExit) return;

// Step 2: Register libraries (between Init and Configure)
await Libraries.LoadAsync<MyLibrary>();

// Step 3: Register the application
CodeLogic.RegisterApplication(new MyApp());

// Step 4: Configure — generates/loads config, runs OnConfigureAsync
await CodeLogic.ConfigureAsync();

// Step 5: Start — runs Initialize + Start phases for libraries, then the application
await CodeLogic.StartAsync();

// Check --health flag
if (result.RunHealthCheck)
{
    var report = await CodeLogic.GetHealthAsync();
    Console.WriteLine(report.ToConsoleString());
    return;
}

// Keep running until shutdown signal
await Task.Delay(Timeout.Infinite);
```

### What each step does

| Step | Method | What happens |
|------|--------|--------------|
| 1 | `InitializeAsync` | Parses CLI args, runs first-run scaffold, loads `CodeLogic.json`, creates `LibraryManager` |
| 2 | `Libraries.LoadAsync<T>` | Registers library types with the `LibraryManager` |
| 3 | `RegisterApplication` | Registers your `IApplication` implementation |
| 4 | `ConfigureAsync` | Calls `OnConfigureAsync` on the app, generates and loads all config/localization files |
| 5 | `StartAsync` | Runs Configure → Initialize → Start on libraries, then Initialize → Start on the app |

---

## First Run Behavior

On first run (when the `CodeLogic/` directory does not yet exist), the framework:

1. Detects the missing directory and prints: `First run detected — scaffolding directory structure...`
2. Creates the full directory tree (see below)
3. Generates `CodeLogic.json` with defaults
4. Continues normally (no exit)

You will see output like:

```
First run detected — scaffolding directory structure...
  Created 12 directories

[CodeLogic] Using CodeLogic.Development.json (DEBUG build)
  ✓ Configured: MyApp
  ✓ Configured: MyLibrary
  ✓ Initialized: MyLibrary
  ✓ Started: MyLibrary
  ✓ Application started: MyApp
```

---

## Directory Structure Created

After first run, the framework creates:

```
YourApp.exe
CodeLogic/
  Framework/
    CodeLogic.json              ← main framework config
    CodeLogic.Development.json  ← development overrides (add to .gitignore)
    logs/
      framework.log             ← framework startup log
  Libraries/
    CL.YourLibrary/
      config.json               ← library config (generated)
      localization/
        strings.en-US.json      ← localization templates (generated)
      logs/
      data/
  Application/
    config.json                 ← application config
    localization/
    logs/
    data/
  Plugins/                      ← plugin folders go here
```

---

## Development vs Production Config

The framework automatically selects the right config file:

| Condition | File loaded |
|-----------|-------------|
| `DEBUG` build OR debugger attached | `CodeLogic.Development.json` |
| `Release` build, no debugger | `CodeLogic.json` |

`CodeLogic.Development.json` is your per-machine development override. Typical development settings:

```json
{
  "logging": {
    "globalLevel": "Debug",
    "enableConsoleOutput": true,
    "consoleMinimumLevel": "Debug"
  }
}
```

**Add `CodeLogic.Development.json` to `.gitignore`** — it is per-machine and should never be committed.

---

## Your First Application

Implement `IApplication`:

```csharp
using CodeLogic.Framework.Application;
using CodeLogic.Framework.Libraries;
using CodeLogic.Core.Configuration;

public class MyApp : IApplication
{
    public ApplicationManifest Manifest => new()
    {
        Id          = "myapp",
        Name        = "My Application",
        Version     = "1.0.0",
        Description = "My first CodeLogic app"
    };

    private AppConfig _config = null!;

    // Phase 1: Register config models (called during ConfigureAsync)
    public Task OnConfigureAsync(ApplicationContext context)
    {
        context.Configuration.Register<AppConfig>();
        return Task.CompletedTask;
    }

    // Phase 2: Load config and initialize services (called during StartAsync, after libraries)
    public Task OnInitializeAsync(ApplicationContext context)
    {
        _config = context.Configuration.Get<AppConfig>();
        context.Logger.Info($"App initialized. GreetingName={_config.GreetingName}");
        return Task.CompletedTask;
    }

    // Phase 3: Start (called immediately after Initialize)
    public Task OnStartAsync(ApplicationContext context)
    {
        context.Logger.Info("Application started!");
        return Task.CompletedTask;
    }

    // Phase 4: Stop (called before libraries stop)
    public Task OnStopAsync()
    {
        return Task.CompletedTask;
    }
}

public class AppConfig : ConfigModelBase
{
    public string GreetingName { get; set; } = "World";
}
```

---

## Your First Library

Implement `ILibrary`:

```csharp
using CodeLogic.Framework.Libraries;
using CodeLogic.Core.Configuration;

public class MyLibrary : ILibrary
{
    public LibraryManifest Manifest => new()
    {
        Id      = "CL.MyLibrary",
        Name    = "My Library",
        Version = "1.0.0"
    };

    private LibraryContext _context = null!;

    // Phase 1: Register config models
    public Task OnConfigureAsync(LibraryContext context)
    {
        context.Configuration.Register<MyLibraryConfig>();
        return Task.CompletedTask;
    }

    // Phase 2: Load config and set up services
    public Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;
        var config = context.Configuration.Get<MyLibraryConfig>();
        context.Logger.Info($"Initialized. MaxItems={config.MaxItems}");
        return Task.CompletedTask;
    }

    // Phase 3: Start background services
    public Task OnStartAsync(LibraryContext context)
    {
        context.Logger.Info("Library started");
        return Task.CompletedTask;
    }

    // Phase 4: Graceful shutdown
    public Task OnStopAsync()
    {
        _context.Logger.Info("Library stopped");
        return Task.CompletedTask;
    }

    // Health check
    public Task<HealthStatus> HealthCheckAsync()
        => Task.FromResult(HealthStatus.Healthy("All good"));

    public void Dispose() { }
}

public class MyLibraryConfig : ConfigModelBase
{
    public int MaxItems { get; set; } = 100;
}
```

---

## Minimal Working Example

The smallest possible CodeLogic application:

```csharp
// Program.cs
using CodeLogic;

var result = await CodeLogic.InitializeAsync(o => o.AppVersion = "1.0.0");
if (result.ShouldExit) return;

await CodeLogic.ConfigureAsync();
await CodeLogic.StartAsync();

Console.WriteLine("Running. Press Ctrl+C to stop.");
await Task.Delay(Timeout.Infinite);
```

No libraries, no application class — just the framework running. From here, add libraries with `Libraries.LoadAsync<T>()` and an application with `CodeLogic.RegisterApplication(new MyApp())`.
