namespace CodeLogic.Framework.Libraries;

public interface ILibrary : IDisposable
{
    LibraryManifest Manifest { get; }

    Task OnConfigureAsync(LibraryContext context);
    Task OnInitializeAsync(LibraryContext context);
    Task OnStartAsync(LibraryContext context);
    Task OnStopAsync();

    Task<HealthStatus> HealthCheckAsync();
}
