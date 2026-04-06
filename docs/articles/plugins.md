# Plugins

Plugins are hot-reloadable, isolated components managed by your application via `PluginManager`. Unlike libraries, plugins are loaded into separate `AssemblyLoadContext` instances and can be unloaded and reloaded at runtime without restarting the host process.

---

## IPlugin Interface

```csharp
public interface IPlugin : IDisposable
{
    PluginManifest Manifest { get; }
    PluginState State       { get; }

    Task OnConfigureAsync(PluginContext context);
    Task OnInitializeAsync(PluginContext context);
    Task OnStartAsync(PluginContext context);
    Task OnUnloadAsync();

    Task<HealthStatus> HealthCheckAsync();
}
```

The 4-phase lifecycle mirrors `ILibrary`. The stop phase is called `OnUnloadAsync` instead of `OnStopAsync`.

---

## PluginManifest

```csharp
public sealed class PluginManifest
{
    public string Id          { get; init; }   // "myapp.dashboard"
    public string Name        { get; init; }   // "Dashboard Plugin"
    public string Version     { get; init; }   // "1.0.0"
    public string? Description { get; init; }
    public string? Author     { get; init; }
    public string[] Tags      { get; init; } = [];
}
```

---

## PluginContext

Full parity with `LibraryContext`. Scoped to the plugin's directory:

```csharp
public sealed class PluginContext
{
    public string PluginId               { get; }
    public string PluginDirectory        { get; }   // {FrameworkRoot}/Plugins/{Id}/
    public string ConfigDirectory        { get; }
    public string LocalizationDirectory  { get; }
    public string LogsDirectory          { get; }
    public string DataDirectory          { get; }

    public ILogger Logger                { get; }
    public IConfigurationManager Configuration { get; }
    public ILocalizationManager Localization   { get; }
    public IEventBus Events              { get; }   // same shared bus
}
```

---

## PluginState

```csharp
public enum PluginState
{
    Unloaded,
    Loading,
    Configuring,
    Initializing,
    Running,
    Unloading,
    Error
}
```

---

## PluginManager

The application manages plugins through `PluginManager`. Get or set it on the application context:

```csharp
// In your application:
public Task OnConfigureAsync(ApplicationContext context)
{
    var manager = new PluginManager(context);
    context.SetPluginManager(manager);
    return Task.CompletedTask;
}

public async Task RunAsync(ApplicationContext context)
{
    var manager = context.GetPluginManager();

    // Load a plugin from its directory
    await manager.LoadAsync("myapp.dashboard");

    // Unload
    await manager.UnloadAsync("myapp.dashboard");

    // Reload (unload + load)
    await manager.ReloadAsync("myapp.dashboard");
}
```

---

## Hot Reload with FileSystemWatcher

`PluginManager` can watch the `Plugins/` directory for changes and automatically reload plugins when their DLL changes:

```csharp
var manager = new PluginManager(context)
{
    EnableHotReload = true,
    HotReloadDebounceMs = 500
};
```

When a plugin's assembly is replaced on disk, `PluginManager`:

1. Calls `OnUnloadAsync()` on the running plugin
2. Unloads the `AssemblyLoadContext`
3. Loads the new assembly into a fresh context
4. Calls `OnConfigureAsync`, `OnInitializeAsync`, `OnStartAsync` on the new instance

---

## Writing a Plugin

```csharp
public class DashboardPlugin : IPlugin
{
    public PluginManifest Manifest => new()
    {
        Id      = "myapp.dashboard",
        Name    = "Dashboard Plugin",
        Version = "1.0.0"
    };

    public PluginState State { get; private set; } = PluginState.Unloaded;

    private DashboardConfig _config = null!;
    private IEventSubscription _sub = null!;

    public Task OnConfigureAsync(PluginContext context)
    {
        context.Configuration.Register<DashboardConfig>();
        State = PluginState.Configuring;
        return Task.CompletedTask;
    }

    public async Task OnInitializeAsync(PluginContext context)
    {
        _config = context.Configuration.Get<DashboardConfig>();
        State = PluginState.Initializing;
        // set up services
    }

    public Task OnStartAsync(PluginContext context)
    {
        _sub = context.Events.Subscribe<DeviceStateChangedEvent>(OnDeviceState);
        State = PluginState.Running;
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync()
    {
        _sub?.Dispose();
        State = PluginState.Unloaded;
        return Task.CompletedTask;
    }

    public Task<HealthStatus> HealthCheckAsync()
        => Task.FromResult(HealthStatus.Healthy("Dashboard running"));

    public void Dispose() { }

    private void OnDeviceState(DeviceStateChangedEvent e)
    {
        // update dashboard state
    }
}
```

---

## Plugin Directory Layout

Each plugin lives in its own subdirectory of `Plugins/`:

```
CodeLogic/
  Plugins/
    myapp.dashboard/
      MyApp.Dashboard.dll      ← plugin assembly
      MyApp.Dashboard.deps.json
      config.dashboard.json    ← plugin config
      localization/
      logs/
      data/
```

The plugin assembly must export a class implementing `IPlugin`. `PluginManager` discovers it by scanning all types in the assembly.

---

## Isolation Model

Plugins run in an `AssemblyLoadContext` derived context. This means:

- Plugin types are isolated from the host — you cannot cast a plugin type to the host's version of the same type
- Only shared abstractions (those in `CodeLogic.Framework`) are shared across the context boundary
- Hot reload works by discarding the old context and creating a fresh one
- The `PluginContext.Events` bus bridges events across the context boundary using a relay pattern
