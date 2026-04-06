using System.Text.Json;
using CodeLogic.Framework.Libraries;

namespace CodeLogic;

public sealed class HealthReport
{
    public bool IsHealthy { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
    public string MachineName { get; init; } = Environment.MachineName;
    public string AppVersion { get; init; } = CodeLogicEnvironment.AppVersion;
    public Dictionary<string, HealthStatus> Libraries { get; init; } = new();
    public Dictionary<string, HealthStatus> Plugins { get; init; } = new();
    public HealthStatus? Application { get; init; }

    public string ToJson() => JsonSerializer.Serialize(new
    {
        isHealthy   = IsHealthy,
        checkedAt   = CheckedAt,
        machineName = MachineName,
        appVersion  = AppVersion,
        libraries   = Libraries.ToDictionary(k => k.Key, v => new { status = v.Value.Status.ToString(), v.Value.Message }),
        plugins     = Plugins.ToDictionary(k => k.Key, v => new { status = v.Value.Status.ToString(), v.Value.Message }),
        application = Application == null ? null : new { status = Application.Status.ToString(), Application.Message }
    }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    public string ToConsoleString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Health Report — {CheckedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Machine: {MachineName}  App: {AppVersion}");
        sb.AppendLine($"Overall: {(IsHealthy ? "HEALTHY" : "UNHEALTHY")}");
        sb.AppendLine();

        if (Libraries.Count > 0)
        {
            sb.AppendLine("Libraries:");
            foreach (var (id, s) in Libraries)
                sb.AppendLine($"  {s.Status,-10} {id}: {s.Message}");
        }
        if (Plugins.Count > 0)
        {
            sb.AppendLine("Plugins:");
            foreach (var (id, s) in Plugins)
                sb.AppendLine($"  {s.Status,-10} {id}: {s.Message}");
        }
        if (Application != null)
            sb.AppendLine($"Application: {Application.Status} — {Application.Message}");

        return sb.ToString();
    }
}
