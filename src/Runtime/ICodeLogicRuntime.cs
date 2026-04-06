using CodeLogic.Core.Events;
using CodeLogic.Framework.Application;
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
    LibraryManager? GetLibraryManager();
    IApplication? GetApplication();
    ApplicationContext? GetApplicationContext();
    IEventBus GetEventBus();
    CodeLogicOptions GetOptions();
    CodeLogicConfiguration GetConfiguration();
}
