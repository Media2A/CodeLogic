# Health Checks

CodeLogic provides a built-in health check system that aggregates status from all libraries, plugins, and the application into a single `HealthReport`.

---

## HealthStatus

Returned by `HealthCheckAsync()` on libraries, plugins, and the application:

```csharp
public sealed class HealthStatus
{
    HealthStatusLevel Status { get; init; }       // Healthy, Degraded, Unhealthy
    string Message { get; init; }                 // human-readable description
    Dictionary<string, object>? Data { get; init; } // optional structured data
    DateTime CheckedAt { get; init; }             // UTC timestamp of the check

    bool IsHealthy   => Status == HealthStatusLevel.Healthy;
    bool IsDegraded  => Status == HealthStatusLevel.Degraded;
    bool IsUnhealthy => Status == HealthStatusLevel.Unhealthy;
}
```

### HealthStatusLevel

```csharp
public enum HealthStatusLevel
{
    Healthy,    // Component is fully operational
    Degraded,   // Running but with reduced capability
    Unhealthy   // Not functioning — requires attention
}
```

### Factory Methods

```csharp
// Success
HealthStatus.Healthy("All 10 connections active")

// Partial failure
HealthStatus.Degraded("Queue depth is 8000 (threshold: 5000)")

// Full failure
HealthStatus.Unhealthy("Cannot connect to database")

// From exception
try { await db.PingAsync(); }
catch (Exception ex) { return HealthStatus.FromException(ex); }
// → Unhealthy with ex.Message as the message
```

### Adding structured data

```csharp
return new HealthStatus
{
    Status  = HealthStatusLevel.Degraded,
    Message = "High queue depth",
    Data    = new Dictionary<string, object>
    {
        ["queueDepth"]   = 8542,
        ["threshold"]    = 5000,
        ["oldestItem"]   = DateTime.UtcNow.AddMinutes(-15)
    }
};
```

---

## HealthReport

Aggregates health from all components:

```csharp
public sealed class HealthReport
{
    bool IsHealthy { get; init; }           // overall — true only if ALL components are healthy
    DateTime CheckedAt { get; init; }
    string MachineName { get; init; }
    string AppVersion { get; init; }

    Dictionary<string, HealthStatus> Libraries { get; init; }  // keyed by library ID
    Dictionary<string, HealthStatus> Plugins   { get; init; }  // keyed by plugin ID
    HealthStatus? Application { get; init; }

    string ToJson();           // pretty JSON suitable for monitoring systems
    string ToConsoleString();  // human-readable table for the terminal
}
```

### ToConsoleString()

```
Health Report — 2026-04-06 10:30:00 UTC
Machine: MY-MACHINE  App: 1.0.0
Overall: HEALTHY

Libraries:
  Healthy    CL.SQLite: Database at data/mydb.sqlite
  Healthy    CL.Mail: SMTP connected to smtp.example.com
  Degraded   CL.ZWave: 2 devices unreachable

Application: Healthy — HomePoint is running
```

### ToJson()

```json
{
  "isHealthy": false,
  "checkedAt": "2026-04-06T10:30:00Z",
  "machineName": "MY-MACHINE",
  "appVersion": "1.0.0",
  "libraries": {
    "CL.SQLite": { "status": "Healthy", "message": "Database at data/mydb.sqlite" },
    "CL.ZWave":  { "status": "Degraded", "message": "2 devices unreachable" }
  },
  "plugins": {},
  "application": { "status": "Healthy", "message": "HomePoint is running" }
}
```

`IsHealthy` is `false` if ANY component is Degraded or Unhealthy.

---

## Scheduled Checks

When `healthChecks.enabled = true` in `CodeLogic.json`, the `LibraryManager` runs health checks on a timer:

```json
{
  "healthChecks": {
    "enabled": true,
    "intervalSeconds": 30
  }
}
```

After each round, a `HealthCheckCompletedEvent` is published on the event bus for each component:

```csharp
context.Events.Subscribe<HealthCheckCompletedEvent>(e =>
{
    if (!e.IsHealthy)
        logger.Warning($"{e.ComponentId} is unhealthy: {e.Message}");
});
```

The timer starts during `StartAsync()` after all libraries are running.

---

## --health CLI Flag

Run `myapp --health` to perform a health check after startup and print the result:

```
$ myapp --health
Health Report — 2026-04-06 10:30:00 UTC
Machine: MY-MACHINE  App: 1.0.0
Overall: HEALTHY

Libraries:
  Healthy    CL.SQLite: Database at data/mydb.sqlite
```

Handle this in your startup code:

```csharp
var result = await CodeLogic.InitializeAsync(o => o.AppVersion = "1.0.0");
if (result.ShouldExit) return;

await Libraries.LoadAsync<SqliteLibrary>();
CodeLogic.RegisterApplication(new MyApp());
await CodeLogic.ConfigureAsync();
await CodeLogic.StartAsync();

// --health sets RunHealthCheck = true
if (result.RunHealthCheck)
{
    var report = await CodeLogic.GetHealthAsync();
    Console.WriteLine(report.ToConsoleString());
    await CodeLogic.StopAsync();
    return;
}
```

---

## Implementing HealthCheckAsync

### In a library

```csharp
public class SqliteLibrary : ILibrary
{
    private Database? _db;

    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_db == null)
            return HealthStatus.Unhealthy("Database not initialized");

        try
        {
            var latency = await _db.MeasureLatencyAsync();

            if (latency > TimeSpan.FromSeconds(5))
                return HealthStatus.Degraded($"High latency: {latency.TotalMilliseconds:F0}ms");

            return HealthStatus.Healthy($"Database OK ({latency.TotalMilliseconds:F0}ms)");
        }
        catch (Exception ex)
        {
            return HealthStatus.FromException(ex);
        }
    }
}
```

### In an application

```csharp
public class MyApp : IApplication
{
    public async Task<HealthStatus> HealthCheckAsync()
    {
        var sqlite = Libraries.Get<SqliteLibrary>();
        if (sqlite == null)
            return HealthStatus.Unhealthy("Required library CL.SQLite not available");

        // Check a business-level condition
        int pendingJobs = await GetPendingJobCount();
        if (pendingJobs > 1000)
            return HealthStatus.Degraded($"{pendingJobs} jobs pending (threshold: 1000)");

        return HealthStatus.Healthy($"Application healthy ({pendingJobs} pending jobs)");
    }
}
```

### In a plugin

Identical pattern to libraries — implement `HealthCheckAsync()` on `IPlugin`:

```csharp
public async Task<HealthStatus> HealthCheckAsync()
{
    return _server?.IsRunning == true
        ? HealthStatus.Healthy("Plugin server running")
        : HealthStatus.Unhealthy("Plugin server not running");
}
```

---

## Requesting a Health Report Programmatically

```csharp
// Get the full report
var report = await CodeLogic.GetHealthAsync();

Console.WriteLine($"Overall: {(report.IsHealthy ? "OK" : "DEGRADED")}");

foreach (var (id, status) in report.Libraries)
    Console.WriteLine($"  {id}: {status.Status} — {status.Message}");

// Get just library health (without plugins or app)
var libraryHealth = await CodeLogic.GetLibraryManager()?.GetHealthAsync()
    ?? new Dictionary<string, HealthStatus>();

// Get just plugin health
var pluginHealth = await CodeLogic.GetPluginManager()?.GetHealthAsync()
    ?? new Dictionary<string, HealthStatus>();

// Serialize for a monitoring endpoint
string json = report.ToJson();
```
