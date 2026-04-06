using CL.WebLogic;
using CL.WebLogic.Runtime;
using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Framework.Application.Plugins;
using CodeLogic.Framework.Libraries;

namespace CodeLogic.Demo.Web.Plugins;

public class RequestLoggerPlugin : IPlugin
{
    public PluginManifest Manifest { get; } = new()
    {
        Id = "demo.request-logger",
        Name = "Request Logger Plugin",
        Version = "1.0.0",
        Description = "Tracks request counts and exposes them through a plugin route",
        Author = "CodeLogic Demo"
    };

    public PluginState State { get; private set; } = PluginState.Loaded;

    private readonly Dictionary<string, int> _byMethod = new();
    private readonly Dictionary<string, int> _byPath = new();
    private readonly object _lock = new();
    private int _total;

    private RequestLoggerConfig _config = new();
    private IEventSubscription? _requestSub;

    public Task OnConfigureAsync(PluginContext context)
    {
        context.Configuration.Register<RequestLoggerConfig>();
        State = PluginState.Configured;
        return Task.CompletedTask;
    }

    public Task OnInitializeAsync(PluginContext context)
    {
        _config = context.Configuration.Get<RequestLoggerConfig>();
        State = PluginState.Initialized;
        return Task.CompletedTask;
    }

    public Task OnStartAsync(PluginContext context)
    {
        _requestSub = context.Events.Subscribe<WebRequestHandledEvent>(e =>
        {
            lock (_lock)
            {
                _total++;
                _byMethod[e.Method] = _byMethod.GetValueOrDefault(e.Method) + 1;
                if (_config.TrackPaths)
                    _byPath[e.Path] = _byPath.GetValueOrDefault(e.Path) + 1;
            }
        });

        var web = WebLogicLibrary.GetRequired();
        web.RegisterApi("/api/plugins/request-stats", _ => Task.FromResult(WebResult.Json(new
        {
            total = _total,
            byMethod = _byMethod,
            byPath = _byPath
        })), "GET");

        State = PluginState.Started;
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync()
    {
        _requestSub?.Dispose();
        State = PluginState.Stopped;
        return Task.CompletedTask;
    }

    public Task<HealthStatus> HealthCheckAsync()
    {
        var topPath = _byPath.OrderByDescending(kv => kv.Value).FirstOrDefault();
        var msg = _total == 0
            ? "No requests tracked yet"
            : $"Total={_total}" + (topPath.Key != null ? $" top='{topPath.Key}'({topPath.Value})" : "");

        return Task.FromResult(HealthStatus.Healthy(msg));
    }

    public void Dispose() => _requestSub?.Dispose();
}

public class RequestLoggerConfig : ConfigModelBase
{
    public bool TrackPaths { get; set; } = true;
}
