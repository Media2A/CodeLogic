using CL.WebLogic;
using CL.WebLogic.Runtime;
using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Demo.Web.Events;
using CodeLogic.Framework.Application.Plugins;
using CodeLogic.Framework.Libraries;

namespace CodeLogic.Demo.Web.Plugins;

public class NotificationPlugin : IPlugin
{
    public PluginManifest Manifest { get; } = new()
    {
        Id = "demo.notifications",
        Name = "Notification Plugin",
        Version = "1.0.0",
        Description = "Keeps a rolling notification log and exposes it through a plugin route",
        Author = "CodeLogic Demo"
    };

    public PluginState State { get; private set; } = PluginState.Loaded;

    private NotificationConfig _config = new();
    private IEventSubscription? _notifySub;
    private readonly List<NotificationEntry> _log = [];
    private readonly object _lock = new();
    private int _errorCount;

    public Task OnConfigureAsync(PluginContext context)
    {
        context.Configuration.Register<NotificationConfig>();
        State = PluginState.Configured;
        return Task.CompletedTask;
    }

    public Task OnInitializeAsync(PluginContext context)
    {
        _config = context.Configuration.Get<NotificationConfig>();
        State = PluginState.Initialized;
        return Task.CompletedTask;
    }

    public Task OnStartAsync(PluginContext context)
    {
        _notifySub = context.Events.Subscribe<AppNotificationEvent>(e =>
        {
            var entry = new NotificationEntry(DateTime.UtcNow, e.Severity, e.Title, e.Message);
            lock (_lock)
            {
                _log.Add(entry);
                if (e.Severity.Equals("error", StringComparison.OrdinalIgnoreCase))
                    _errorCount++;
                if (_log.Count > _config.MaxLogSize)
                    _log.RemoveAt(0);
            }
        });

        var web = WebLogicLibrary.GetRequired();
        web.RegisterApi("/api/plugins/notifications", _ => Task.FromResult(WebResult.Json(new
        {
            total = _log.Count,
            errors = _errorCount,
            log = _log
        })), "GET");

        State = PluginState.Started;
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync()
    {
        _notifySub?.Dispose();
        State = PluginState.Stopped;
        return Task.CompletedTask;
    }

    public Task<HealthStatus> HealthCheckAsync()
    {
        lock (_lock)
        {
            var msg = $"log={_log.Count}/{_config.MaxLogSize} errors={_errorCount}";
            if (_errorCount >= _config.ErrorThreshold)
                return Task.FromResult(HealthStatus.Degraded(msg));
            return Task.FromResult(HealthStatus.Healthy(msg));
        }
    }

    public void Dispose() => _notifySub?.Dispose();
}

public record NotificationEntry(DateTime Timestamp, string Severity, string Title, string Message);

public class NotificationConfig : ConfigModelBase
{
    public int MaxLogSize { get; set; } = 50;
    public int ErrorThreshold { get; set; } = 5;
}
