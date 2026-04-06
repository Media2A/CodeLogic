using CodeLogic.Core.Events;
using CodeLogic.Framework.Application;
using CodeLogic.Framework.Application.Plugins;
using CodeLogic.Framework.Libraries;

namespace CodeLogic;

/// <summary>
/// Static facade for the CodeLogic framework.
/// Delegates to a singleton CodeLogicRuntime instance.
/// For testing or advanced scenarios, use ICodeLogicRuntime directly.
/// </summary>
public static class CodeLogic
{
    private static readonly ICodeLogicRuntime _runtime = new CodeLogicRuntime();

    public static Task<InitializationResult> InitializeAsync(Action<CodeLogicOptions>? configure = null)
        => _runtime.InitializeAsync(configure);

    public static void RegisterApplication(IApplication application)
        => _runtime.RegisterApplication(application);

    public static Task ConfigureAsync()  => _runtime.ConfigureAsync();
    public static Task StartAsync()      => _runtime.StartAsync();
    public static Task StopAsync()       => _runtime.StopAsync();
    public static Task ResetAsync()      => _runtime.ResetAsync();

    public static Task<HealthReport>      GetHealthAsync()           => _runtime.GetHealthAsync();
    public static LibraryManager?         GetLibraryManager()        => _runtime.GetLibraryManager();
    public static IApplication?           GetApplication()           => _runtime.GetApplication();
    public static ApplicationContext?     GetApplicationContext()     => _runtime.GetApplicationContext();
    public static IEventBus               GetEventBus()              => _runtime.GetEventBus();
    public static CodeLogicOptions        GetOptions()               => _runtime.GetOptions();
    public static CodeLogicConfiguration  GetConfiguration()         => _runtime.GetConfiguration();

    /// <summary>
    /// Registers an app-managed PluginManager so it participates in health
    /// checks and graceful shutdown. Call after InitializeAsync.
    /// </summary>
    public static void SetPluginManager(PluginManager manager)       => _runtime.SetPluginManager(manager);
    public static PluginManager?          GetPluginManager()         => _runtime.GetPluginManager();
}
