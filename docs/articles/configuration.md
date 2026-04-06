# Configuration

CodeLogic's configuration system provides per-component JSON files with automatic generation, DataAnnotations validation, environment overrides, and hot-reload support.

---

## ConfigModelBase

All config classes inherit from `ConfigModelBase`:

```csharp
public abstract class ConfigModelBase
{
    public virtual ConfigValidationResult Validate();
}
```

The `Validate()` method runs DataAnnotations validation automatically. Override it to add custom rules:

```csharp
using System.ComponentModel.DataAnnotations;
using CodeLogic.Core;

public class DatabaseConfig : ConfigModelBase
{
    [Required]
    public string ConnectionString { get; set; } = "Data Source=myapp.db";

    [Range(1, 100)]
    public int MaxConnections { get; set; } = 10;

    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 30;

    public override ConfigValidationResult Validate()
    {
        var result = base.Validate();      // runs DataAnnotations
        if (!result.IsValid) return result;

        // custom rules
        if (ConnectionString.Contains(".."))
            return ConfigValidationResult.Invalid(["Path traversal not allowed in connection string"]);

        return ConfigValidationResult.Valid();
    }
}
```

---

## ConfigSectionAttribute

Controls the file name suffix for a config model:

```csharp
[ConfigSection("database")]
public class DatabaseConfig : ConfigModelBase { ... }
```

| Library ID | Attribute value | File name |
|------------|-----------------|-----------|
| `MyApp.Database` | `[ConfigSection("database")]` | `config.database.json` |
| `MyApp.Database` | `[ConfigSection("advanced")]` | `config.advanced.json` |
| `MyApp.Database` | *(none)* | `config.json` |

Files are placed in the library's `ConfigDirectory` (`{FrameworkRoot}/Libraries/{LibraryId}/`).

---

## Registering Config Models

Register models in `OnConfigureAsync`:

```csharp
public Task OnConfigureAsync(LibraryContext context)
{
    context.Configuration.Register<DatabaseConfig>();
    context.Configuration.Register<AdvancedConfig>();
    return Task.CompletedTask;
}
```

Reading config in `OnConfigureAsync` will throw — files are generated/loaded after this phase completes.

---

## Reading Config

Read models in `OnInitializeAsync` or later:

```csharp
public async Task OnInitializeAsync(LibraryContext context)
{
    var config = context.Configuration.Get<DatabaseConfig>();

    var validation = config.Validate();
    if (!validation.IsValid)
        throw new InvalidOperationException(string.Join("; ", validation.Errors));

    // use config values
}
```

---

## IConfigurationManager API

```csharp
public interface IConfigurationManager
{
    void Register<T>() where T : ConfigModelBase, new();
    T Get<T>() where T : ConfigModelBase;
    Task ReloadAsync();
    Task SaveAsync<T>(T model) where T : ConfigModelBase;
}
```

---

## Environment Overrides

For each `config.X.json`, CodeLogic also loads `config.X.Development.json` when running in development mode, merging the override on top. Use this for local settings that should not be committed to source control.

Example:
```
config.database.json             ← default values, committed
config.database.Development.json ← local overrides, git-ignored
```

```json
// config.database.Development.json
{
  "ConnectionString": "Data Source=dev-local.db",
  "MaxConnections": 5
}
```

---

## First-Run Generation

When a config file does not exist, CodeLogic:

1. Creates the config file with default property values
2. Exits with a message: `Config files generated. Please review and restart.`

Use the `--generate-configs` flag to generate without starting:

```bash
./MyApp --generate-configs
```

Use `--force` to regenerate even if files already exist:

```bash
./MyApp --generate-configs --force
```

---

## Hot Reload

Config can be reloaded at runtime:

```csharp
await context.Configuration.ReloadAsync();
var updated = context.Configuration.Get<DatabaseConfig>();
```

You can watch for external changes and trigger a reload via an event or a background timer.

---

## framework config: CodeLogic.json

The framework itself uses `CodeLogic.json` in the `FrameworkRootPath` directory. It controls:

- Logging level and output mode
- Library directories
- Development mode detection

See [API Reference](../api/index.md) for the full `CodeLogic.json` property reference.
