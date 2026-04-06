using CodeLogic.Framework.Libraries;

namespace CodeLogic.Framework.Application;

/// <summary>
/// Interface for the consuming application to participate in the CodeLogic lifecycle.
/// Register via CodeLogic.RegisterApplication() before ConfigureAsync().
/// The application is configured and started AFTER all libraries are running.
/// It is stopped BEFORE libraries are stopped.
/// </summary>
public interface IApplication
{
    ApplicationManifest Manifest { get; }

    /// <summary>Phase 1: Register config and localization models.</summary>
    Task OnConfigureAsync(ApplicationContext context);

    /// <summary>Phase 2: Setup services using loaded config and localization.</summary>
    Task OnInitializeAsync(ApplicationContext context);

    /// <summary>Phase 3: Start application services.</summary>
    Task OnStartAsync(ApplicationContext context);

    /// <summary>Phase 4: Stop application services gracefully.</summary>
    Task OnStopAsync();

    /// <summary>
    /// Returns the current health of the application.
    /// Called by the framework during health checks and --health CLI.
    /// Default implementation returns Healthy — override to add real checks.
    /// </summary>
    Task<Libraries.HealthStatus> HealthCheckAsync() =>
        Task.FromResult(Libraries.HealthStatus.Healthy($"{Manifest.Name} is running"));
}
