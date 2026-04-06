using CodeLogic.Core.Events;
using CodeLogic.Framework.Application;
using CodeLogic.Framework.Application.Plugins;
using CodeLogic.Framework.Libraries;

namespace CodeLogic;

public interface ICodeLogicRuntime
{
    Task<InitializationResult> InitializeAsync(Action<CodeLogicOptions>? configure = null);
    void RegisterApplication(IApplication application);
    Task ConfigureAsync();
    Task StartAsync();
    Task StopAsync();
    Task ResetAsync();

    Task<HealthReport> GetHealthAsync();
    LibraryManager?    GetLibraryManager();
    IApplication?      GetApplication();
    ApplicationContext? GetApplicationContext();
    IEventBus          GetEventBus();
    CodeLogicOptions   GetOptions();
    CodeLogicConfiguration GetConfiguration();

    /// <summary>
    /// Registers an app-managed PluginManager with the runtime so it participates
    /// in health checks and graceful shutdown. Call after InitializeAsync.
    /// </summary>
    void SetPluginManager(PluginManager manager);

    /// <summary>Returns the registered PluginManager, or null if none registered.</summary>
    PluginManager? GetPluginManager();
}
