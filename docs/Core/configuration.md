# Configuration

CodeLogic's configuration system provides per-component JSON config files with auto-generation, DataAnnotations validation, and hot-reload support.

---

## ConfigModelBase

All config classes inherit from `ConfigModelBase`:

```csharp
public abstract class ConfigModelBase
{
    public virtual ConfigValidationResult Validate();
}
```

The `Validate()` method runs `DataAnnotations` validation automatically. Override it to add custom rules:

```csharp
public class DatabaseConfig : ConfigModelBase
{
    [Required]
    public string ConnectionString { get; set; } = "Data Source=mydb.sqlite";

    [Range(1, 100)]
    public int MaxConnections { get; set; } = 10;

    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 30;

    public override ConfigValidationResult Validate()
    {
        var result = base.Validate();
        if (!result.IsValid) return result;

        // Custom validation
        if (ConnectionString.Contains(".."))
            return ConfigValidationResult.Invalid(["Connection string must not contain '..'."]);

        return ConfigValidationResult.Valid();
    }
}
```

---

## ConfigSectionAttribute

Controls the file name for a config model:

```csharp
[ConfigSection("database")]
public class DatabaseConfig : ConfigModelBase { ... }
```

File naming rules:

| Attribute | File name |
|-----------|-----------|
| No attribute | `config.json` |
| `[ConfigSection("database")]` | `config.database.json` |
| `Register<T>("db")` (subConfigName param) | `config.db.json` |

---

## IConfigurationManager

Each component gets its own `IConfigurationManager` scoped to its directory.

```csharp
public interface IConfigurationManager
{
    void Register<T>(string? subConfigName = null) where T : ConfigModelBase, new();
    T Get<T>() where T : ConfigModelBase, new();
    Task GenerateDefaultAsync<T>() where T : ConfigModelBase, new();
    Task LoadAsync<T>() where T : ConfigModelBase, new();
    Task SaveAsync<T>(T config) where T : ConfigModelBase, new();
    Task GenerateAllDefaultsAsync();
    Task LoadAllAsync();
    Task ReloadAsync<T>() where T : ConfigModelBase, new();
    Task ReloadAllAsync();
}
```

### Method reference

| Method | Description |
|--------|-------------|
| `Register<T>()` | Registers a config type. Call in `OnConfigureAsync`. |
| `Get<T>()` | Returns the loaded instance. Throws if not loaded. |
| `GenerateDefaultAsync<T>()` | Writes `config.json` with defaults if the file doesn't exist. |
| `LoadAsync<T>()` | Loads from disk, generates defaults if missing, validates. |
| `SaveAsync<T>(config)` | Validates then writes to disk. Throws if validation fails. |
| `GenerateAllDefaultsAsync()` | Generates defaults for all registered types that don't have files. |
| `LoadAllAsync()` | Loads all registered configs. Called automatically by the framework. |
| `ReloadAsync<T>()` | Reloads a specific config from disk and updates the in-memory instance. |
| `ReloadAllAsync()` | Reloads all registered configs from disk. |

---

## Register → Generate → Load → Save → Reload Lifecycle

### The standard pattern (automatic)

The framework handles `GenerateAllDefaultsAsync()` and `LoadAllAsync()` automatically after `OnConfigureAsync`:

```csharp
public Task OnConfigureAsync(LibraryContext context)
{
    // Just register — framework generates and loads automatically
    context.Configuration.Register<DatabaseConfig>();
    context.Configuration.Register<CacheConfig>("cache");   // → config.cache.json
    return Task.CompletedTask;
}

public Task OnInitializeAsync(LibraryContext context)
{
    // Config is loaded — safe to read
    var db = context.Configuration.Get<DatabaseConfig>();
    context.Logger.Info($"Connecting to {db.ConnectionString}");
    return Task.CompletedTask;
}
```

### Saving changes at runtime

```csharp
var config = context.Configuration.Get<DatabaseConfig>();
config.MaxConnections = 20;
await context.Configuration.SaveAsync(config);
```

`SaveAsync` runs validation before writing. If validation fails, an exception is thrown and the file is not written.

### Hot-reloading

Some config values can be safely reloaded at runtime without a restart:

```csharp
// Safe to reload: log levels, intervals, pool sizes, thresholds
await context.Configuration.ReloadAsync<DatabaseConfig>();

// NOT safe to reload without a restart: connection strings, paths, ports
// (the service using them is already initialized with the old value)
```

Subscribe to `ConfigReloadedEvent` to react to reloads:

```csharp
context.Events.Subscribe<ConfigReloadedEvent>(e =>
{
    if (e.ComponentId == "CL.MyLibrary" && e.ConfigType == typeof(DatabaseConfig))
    {
        var newConfig = context.Configuration.Get<DatabaseConfig>();
        ApplyNewPoolSize(newConfig.MaxConnections);
    }
});
```

---

## Validation with DataAnnotations

```csharp
using System.ComponentModel.DataAnnotations;

public class AppConfig : ConfigModelBase
{
    [Required]
    [MinLength(1)]
    public string AppName { get; set; } = "MyApp";

    [Range(1024, 65535)]
    public int Port { get; set; } = 8080;

    [EmailAddress]
    public string? AdminEmail { get; set; }

    [Url]
    public string ApiBaseUrl { get; set; } = "https://api.example.com";
}
```

When a config fails validation during `LoadAsync`, an exception is thrown with the validation errors listed. This prevents the application from starting with an invalid configuration.

---

## Code Examples

### Multiple config files per library

```csharp
public Task OnConfigureAsync(LibraryContext context)
{
    context.Configuration.Register<AppConfig>();            // → config.json
    context.Configuration.Register<DatabaseConfig>("db");   // → config.db.json
    context.Configuration.Register<SmtpConfig>("smtp");    // → config.smtp.json
    return Task.CompletedTask;
}
```

### Using ConfigSection attribute

```csharp
[ConfigSection("smtp")]
public class SmtpConfig : ConfigModelBase
{
    [Required]
    public string Host { get; set; } = "smtp.example.com";

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;
}
```

With `[ConfigSection("smtp")]`, you register without a subname and get the same file:

```csharp
context.Configuration.Register<SmtpConfig>(); // → config.smtp.json
```

### Manual generate-only (CI pattern)

```csharp
// Generate configs and exit without starting
var result = await CodeLogic.InitializeAsync(o =>
{
    o.GenerateConfigs = true;
    o.ExitAfterGenerate = true;
});
```

Or use the CLI: `myapp --generate-configs`
