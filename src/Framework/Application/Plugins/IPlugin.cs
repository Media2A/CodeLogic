using CodeLogic.Framework.Libraries;

namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// Interface for hot-loadable CodeLogic plugins.
/// Plugins are app-managed: the consuming application decides what to load.
/// Full 4-phase lifecycle matching ILibrary.
/// </summary>
public interface IPlugin : IDisposable
{
    PluginManifest Manifest { get; }
    PluginState State { get; }

    /// <summary>Phase 1: Register config and localization models.</summary>
    Task OnConfigureAsync(PluginContext context);

    /// <summary>Phase 2: Setup services using loaded config.</summary>
    Task OnInitializeAsync(PluginContext context);

    /// <summary>Phase 3: Start plugin services.</summary>
    Task OnStartAsync(PluginContext context);

    /// <summary>Phase 4: Stop and clean up.</summary>
    Task OnUnloadAsync();

    Task<HealthStatus> HealthCheckAsync();
}
