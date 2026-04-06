namespace CodeLogic.Framework.Libraries;

public enum HealthStatusLevel { Healthy, Degraded, Unhealthy }

public sealed class HealthStatus
{
    public HealthStatusLevel Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, object>? Data { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;

    public bool IsHealthy  => Status == HealthStatusLevel.Healthy;
    public bool IsDegraded => Status == HealthStatusLevel.Degraded;
    public bool IsUnhealthy => Status == HealthStatusLevel.Unhealthy;

    public static HealthStatus Healthy(string message = "Healthy") =>
        new() { Status = HealthStatusLevel.Healthy, Message = message };

    public static HealthStatus Degraded(string message) =>
        new() { Status = HealthStatusLevel.Degraded, Message = message };

    public static HealthStatus Unhealthy(string message) =>
        new() { Status = HealthStatusLevel.Unhealthy, Message = message };

    public static HealthStatus FromException(Exception ex) =>
        new() { Status = HealthStatusLevel.Unhealthy, Message = ex.Message };

    public override string ToString() => $"{Status}: {Message}";
}
