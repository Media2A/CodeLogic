# CLI Arguments

CodeLogic automatically parses command-line arguments during `InitializeAsync`. Arguments override programmatic options — CLI always wins.

---

## All Supported Arguments

| Argument | Description |
|----------|-------------|
| `--version` | Print framework and app version, then exit |
| `--info` | Print framework info (paths, machine, version), then exit |
| `--health` | Run a health check after startup and print the report, then exit |
| `--generate-configs` | Generate missing config files with defaults |
| `--generate-configs-force` | Regenerate ALL config files (overwrites existing) |
| `--dry-run` | Parse args and validate but do not persist changes |

---

## --version

Prints the framework and application version, then exits:

```
$ myapp --version
CodeLogic 3.0.0 | App 1.0.0
```

`InitializationResult.ShouldExit = true` — check this and return immediately:

```csharp
var result = await CodeLogic.InitializeAsync(o => o.AppVersion = "1.0.0");
if (result.ShouldExit) return;
```

---

## --info

Prints detailed information about the framework's configuration, then exits:

```
$ myapp --info
CodeLogic 3.0.0
  App version  : 1.0.0
  Machine      : MY-MACHINE
  Framework    : C:\MyApp\CodeLogic
  Application  : C:\MyApp\CodeLogic\Application
  Libraries    : C:\MyApp\CodeLogic\Libraries
  Development  : False
```

---

## --health

Signals the caller that a health check should be run after startup:

```csharp
var result = await CodeLogic.InitializeAsync(o => o.AppVersion = "1.0.0");
if (result.ShouldExit) return;

await Libraries.LoadAsync<SqliteLibrary>();
CodeLogic.RegisterApplication(new MyApp());
await CodeLogic.ConfigureAsync();
await CodeLogic.StartAsync();

// Handle --health after startup (libraries must be running for accurate results)
if (result.RunHealthCheck)
{
    var report = await CodeLogic.GetHealthAsync();
    Console.WriteLine(report.ToConsoleString());
    await CodeLogic.StopAsync();
    return;
}
```

Unlike `--version` and `--info`, `--health` does NOT set `ShouldExit = true` immediately. It sets `RunHealthCheck = true` so the caller can run the full startup sequence first, then check health and exit.

```
$ myapp --health
Health Report — 2026-04-06 10:30:00 UTC
Machine: MY-MACHINE  App: 1.0.0
Overall: HEALTHY

Libraries:
  Healthy    CL.SQLite: Database at data/mydb.sqlite
  Healthy    CL.Mail: SMTP connected

Application: Healthy — HomePoint is running
```

---

## --generate-configs

Generates missing config files with defaults, then continues (or exits if `ExitAfterGenerate = true`):

```
$ myapp --generate-configs
  Generated: CodeLogic/Libraries/CL.SQLite/config.json
  Generated: CodeLogic/Application/config.json
```

Does NOT overwrite existing files. Combine with `--dry-run` to preview without writing.

### Scoping to specific libraries

Append library IDs to limit generation:

```
$ myapp --generate-configs CL.SQLite CL.Mail
```

This sets `GenerateConfigsFor = ["CL.SQLite", "CL.Mail"]`. Only those libraries' configs are generated.

---

## --generate-configs-force

Regenerates ALL config files, overwriting existing ones:

```
$ myapp --generate-configs-force
  Overwritten: CodeLogic/Libraries/CL.SQLite/config.json
  Overwritten: CodeLogic/Application/config.json
```

**Destructive** — existing customizations are lost. Use with caution.

Also supports scoping:

```
$ myapp --generate-configs-force CL.SQLite
```

---

## --dry-run

Parse and validate arguments without writing any files. Useful for CI:

```
$ myapp --generate-configs --dry-run
[dry-run] Would generate: CodeLogic/Libraries/CL.SQLite/config.json
[dry-run] Would generate: CodeLogic/Application/config.json
```

---

## InitializationResult

`InitializeAsync` returns an `InitializationResult` that captures the outcome:

```csharp
public sealed class InitializationResult
{
    bool Success { get; init; }           // false = initialization failed
    bool IsFirstRun { get; init; }        // true if directory structure was just scaffolded
    bool ShouldExit { get; init; }        // true if the process should exit now
    string Message { get; init; }         // human-readable description

    /// When true: start fully, run health check, then exit.
    bool RunHealthCheck { get; init; }

    static InitializationResult Succeeded(bool isFirstRun, bool runHealthCheck);
    static InitializationResult Failed(string message);
    static InitializationResult Exit(string message);  // success + ShouldExit=true
}
```

### Handling all cases

```csharp
var result = await CodeLogic.InitializeAsync(o => o.AppVersion = "1.0.0");

if (!result.Success)
{
    Console.Error.WriteLine($"Startup failed: {result.Message}");
    Environment.Exit(1);
}

if (result.ShouldExit)
{
    // --version or --info was handled
    return;
}

// ... rest of startup ...

if (result.RunHealthCheck)
{
    var report = await CodeLogic.GetHealthAsync();
    Console.WriteLine(report.ToConsoleString());
    await CodeLogic.StopAsync();
    return;
}
```

---

## Programmatic Equivalents

Any CLI arg can also be set programmatically (CLI overrides programmatic):

```csharp
await CodeLogic.InitializeAsync(o =>
{
    o.AppVersion         = "1.0.0";
    o.GenerateConfigs    = true;        // --generate-configs
    o.GenerateConfigsForce = false;     // --generate-configs-force
    o.GenerateConfigsFor = null;        // null = all libraries
    o.ExitAfterGenerate  = false;       // exit after generating
});
```
