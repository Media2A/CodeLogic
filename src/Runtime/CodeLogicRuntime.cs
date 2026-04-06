using System.Diagnostics;
using System.Text.Json;
using CodeLogic.Core.Events;
using CodeLogic.Core.Logging;
using CodeLogic.Core.Utilities;
using CodeLogic.Framework.Application;
using CodeLogic.Framework.Application.Plugins;
using CodeLogic.Framework.Libraries;

namespace CodeLogic;

public sealed class CodeLogicRuntime : ICodeLogicRuntime
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly EventBus _eventBus = new();

    // Framework-level logger — writes to Framework/logs/framework.log
    // Created after config is loaded so it respects CodeLogic.json settings.
    // Before that, startup messages go to Console only.
    private ILogger _frameworkLogger = NullLogger.Instance;

    // App-managed PluginManager — optional, registered via SetPluginManager()
    private PluginManager? _pluginManager;

    private CodeLogicOptions? _options;
    private CodeLogicConfiguration? _config;
    private LibraryManager? _libraryManager;
    private IApplication? _application;
    private ApplicationContext? _applicationContext;
    private bool _initialized;
    private bool _shutdownRegistered;

    // ── Initialization ───────────────────────────────────────────────────────

    public async Task<InitializationResult> InitializeAsync(Action<CodeLogicOptions>? configure = null)
    {
        await _lock.WaitAsync();
        try
        {
            if (_initialized)
                return InitializationResult.Failed("CodeLogic already initialized.");

            // Build options
            _options = new CodeLogicOptions();
            configure?.Invoke(_options);

            // Merge CLI args (CLI wins)
            var cli = CliArgParser.Parse();
            if (cli.GenerateConfigs)      _options.GenerateConfigs      = true;
            if (cli.GenerateConfigsForce) _options.GenerateConfigsForce = true;
            if (cli.GenerateConfigsFor != null) _options.GenerateConfigsFor = cli.GenerateConfigsFor;

            // Handle exit-only CLI modes before anything else
            if (cli.ShowVersion)
            {
                Console.WriteLine($"CodeLogic 3.0.0 | App {_options.AppVersion}");
                return InitializationResult.Exit("--version");
            }

            if (cli.ShowInfo)
            {
                // --info can be answered immediately — no libraries needed
                Console.WriteLine($"CodeLogic 3.0.0");
                Console.WriteLine($"  App version  : {_options.AppVersion}");
                Console.WriteLine($"  Machine      : {Environment.MachineName}");
                Console.WriteLine($"  Framework    : {_options.GetFrameworkPath()}");
                Console.WriteLine($"  Application  : {_options.GetApplicationPath()}");
                Console.WriteLine($"  Libraries    : {_options.GetLibrariesPath()}");
                Console.WriteLine($"  Development  : {IsDevelopmentMode()}");
                return InitializationResult.Exit("--info");
            }

            // Set app version on environment
            CodeLogicEnvironment.AppVersion = _options.AppVersion;

            // First-run scaffolding (no exit — just scaffold and continue)
            var frameworkRoot = _options.GetFrameworkPath();
            var appRoot       = _options.GetApplicationPath();

            if (FirstRunManager.IsFirstRun(frameworkRoot))
            {
                Console.WriteLine("\n  First run detected — scaffolding directory structure...");
                var scaffoldResult = await FirstRunManager.ScaffoldAsync(frameworkRoot, appRoot);
                if (!scaffoldResult.Success)
                    return InitializationResult.Failed($"First-run scaffold failed: {scaffoldResult.Error}");
                Console.WriteLine($"  Created {scaffoldResult.DirectoriesCreated} directories\n");
            }

            // Load CodeLogic.json
            await LoadConfigurationAsync();

            // If debugger is attached, override log levels in memory so
            // all Info/Debug messages write to disk during development.
            ApplyDebugDefaults();

            // Create the framework logger now that we have config
            _frameworkLogger = CreateFrameworkLogger();
            _frameworkLogger.Info("Framework initialized");
            _frameworkLogger.Info($"  Machine  : {CodeLogicEnvironment.MachineName}");
            _frameworkLogger.Info($"  App      : {CodeLogicEnvironment.AppVersion}");
            _frameworkLogger.Info($"  Debug    : {CodeLogicEnvironment.IsDebugging}");
            _frameworkLogger.Info($"  Root     : {GetOptionsOrThrow().GetFrameworkPath()}");
            _frameworkLogger.Info($"  App path : {GetOptionsOrThrow().GetApplicationPath()}");

            // Create LibraryManager eagerly so Libraries.LoadAsync<T>() can be called
            // right after InitializeAsync — before ConfigureAsync. This is the clean
            // pattern: register all libs first, then Configure + Start in one go.
            var cfg = GetConfigOrThrow();
            var opt = GetOptionsOrThrow();
            _libraryManager = new LibraryManager(_eventBus)
            {
                LoggingOptions             = cfg.Logging.ToLoggingOptions(),
                FrameworkRootPath          = opt.GetFrameworkPath(),
                DefaultCulture             = cfg.Localization.DefaultCulture,
                SupportedCultures          = cfg.Localization.SupportedCultures,
                EnableDependencyResolution = cfg.Libraries.EnableDependencyResolution,
                LibraryDiscoveryPattern    = cfg.Libraries.DiscoveryPattern
            };
            _frameworkLogger.Info("Library manager created — ready for Libraries.LoadAsync<T>()");

            _initialized = true;

            // Register shutdown signals
            if (_options.HandleShutdownSignals && !_shutdownRegistered)
            {
                _shutdownRegistered = true;
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    _eventBus.Publish(new ShutdownRequestedEvent("CTRL+C"));
                    _ = StopAsync();
                };
                AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                {
                    _eventBus.Publish(new ShutdownRequestedEvent("ProcessExit"));
                    StopAsync().GetAwaiter().GetResult();
                };
            }

            // --health needs libraries running — signal the caller to handle it after StartAsync
            return InitializationResult.Succeeded(runHealthCheck: cli.ShowHealth);
        }
        catch (Exception ex)
        {
            return InitializationResult.Failed($"Initialization failed: {ex.Message}");
        }
        finally { _lock.Release(); }
    }

    // ── Application registration ─────────────────────────────────────────────

    public void RegisterApplication(IApplication application)
    {
        EnsureInitialized();
        _lock.Wait();
        try
        {
            _application = application;
            _frameworkLogger.Info($"Application registered: {application.Manifest.Name} v{application.Manifest.Version}");
        }
        finally { _lock.Release(); }
    }

    // ── Configure ────────────────────────────────────────────────────────────

    public async Task ConfigureAsync()
    {
        EnsureInitialized();
        await _lock.WaitAsync();
        try
        {
            var opts   = GetOptionsOrThrow();
            var config = GetConfigOrThrow();

            // Validate structure
            var validator = new StartupValidator();
            var validation = validator.Validate(opts.GetFrameworkPath());
            foreach (var w in validator.GetWarnings())
                _frameworkLogger.Warning(w);
            if (!validation.IsSuccess)
                throw new InvalidOperationException($"Startup validation failed: {validation.ErrorMessage}");

            // Discover + load DLL-based libraries (if any).
            // LibraryManager was already created in InitializeAsync so that
            // Libraries.LoadAsync<T>() could be called between Init and Configure.
            var discovered = _libraryManager!.Discover();
            if (discovered.Count > 0)
                await _libraryManager.LoadLibrariesAsync(discovered);

            // Configure application
            if (_application != null)
            {
                _frameworkLogger.Info($"Configuring application: {_application.Manifest.Name}");
                var appCtx = CreateApplicationContext();

                await _application.OnConfigureAsync(appCtx);
                await appCtx.Configuration.GenerateAllDefaultsAsync();
                await appCtx.Configuration.LoadAllAsync();
                await appCtx.Localization.GenerateAllTemplatesAsync(config.Localization.SupportedCultures);
                await appCtx.Localization.LoadAllAsync(config.Localization.SupportedCultures);

                _applicationContext = appCtx; // only assign after full success
                _frameworkLogger.Info($"Application configured: {_application.Manifest.Name}");
            }
        }
        finally { _lock.Release(); }
    }

    // ── Start ────────────────────────────────────────────────────────────────

    public async Task StartAsync()
    {
        EnsureInitialized();
        await _lock.WaitAsync();
        try
        {
            var config = GetConfigOrThrow();

            if (_libraryManager != null)
            {
                await _libraryManager.ConfigureAllAsync();
                await _libraryManager.InitializeAllAsync();
                await _libraryManager.StartAllAsync();

                if (config.HealthChecks.Enabled)
                    _libraryManager.StartHealthCheckTimer(config.HealthChecks.IntervalSeconds);
            }

            if (_application != null && _applicationContext != null)
            {
                _frameworkLogger.Info($"Starting application: {_application.Manifest.Name}");
                await _application.OnInitializeAsync(_applicationContext);
                await _application.OnStartAsync(_applicationContext);
                _frameworkLogger.Info($"Application started: {_application.Manifest.Name}");
            }
        }
        finally { _lock.Release(); }
    }

    // ── Stop ─────────────────────────────────────────────────────────────────

    public async Task StopAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _eventBus.Publish(new ShutdownRequestedEvent("StopAsync called"));

            if (_application != null)
            {
                _frameworkLogger.Info($"Stopping application: {_application.Manifest.Name}");
                try { await _application.OnStopAsync(); }
                catch (Exception ex) { _frameworkLogger.Error($"Application stop error: {ex.Message}", ex); }
            }

            // Unload all plugins before stopping libraries
            if (_pluginManager != null)
            {
                _frameworkLogger.Info("Unloading plugins...");
                try { await _pluginManager.UnloadAllAsync(); }
                catch (Exception ex) { _frameworkLogger.Error($"Plugin unload error: {ex.Message}", ex); }
            }

            if (_libraryManager != null)
                await _libraryManager.StopAllAsync();

            _frameworkLogger.Info("Framework stopped");
        }
        finally { _lock.Release(); }
    }

    // ── Reset ────────────────────────────────────────────────────────────────

    public async Task ResetAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_application != null)
                try { await _application.OnStopAsync(); } catch { }

            if (_libraryManager != null)
            {
                try { await _libraryManager.StopAllAsync(); } catch { }
                _libraryManager.Dispose();
            }

            _options            = null;
            _config             = null;
            _libraryManager     = null;
            _application        = null;
            _applicationContext = null;
            _initialized        = false;
        }
        finally { _lock.Release(); }
    }

    // ── Health ───────────────────────────────────────────────────────────────

    public async Task<HealthReport> GetHealthAsync()
    {
        var libs    = _libraryManager != null ? await _libraryManager.GetHealthAsync() : new();
        var plugins = _pluginManager  != null ? await _pluginManager.GetHealthAsync()  : new();

        HealthStatus? appHealth = null;
        if (_application != null)
        {
            try { appHealth = await _application.HealthCheckAsync(); }
            catch (Exception ex) { appHealth = HealthStatus.FromException(ex); }
        }

        var overall = libs.Values.All(h => h.IsHealthy)
                   && plugins.Values.All(h => h.IsHealthy)
                   && (appHealth?.IsHealthy ?? true);

        return new HealthReport
        {
            IsHealthy   = overall,
            Libraries   = libs,
            Plugins     = plugins,
            Application = appHealth,
        };
    }

    // ── Accessors ────────────────────────────────────────────────────────────

    public LibraryManager?    GetLibraryManager()       => _libraryManager;
    public IApplication?      GetApplication()           => _application;
    public ApplicationContext? GetApplicationContext()   => _applicationContext;
    public IEventBus          GetEventBus()              => _eventBus;

    public void SetPluginManager(PluginManager manager)
    {
        _pluginManager = manager;
        _frameworkLogger.Info($"PluginManager registered");
    }

    public PluginManager? GetPluginManager() => _pluginManager;
    public CodeLogicOptions   GetOptions()               => GetOptionsOrThrow();
    public CodeLogicConfiguration GetConfiguration()    => GetConfigOrThrow();

    // ── Private helpers ──────────────────────────────────────────────────────

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("CodeLogic not initialized. Call InitializeAsync() first.");
    }

    private CodeLogicOptions GetOptionsOrThrow() =>
        _options ?? throw new InvalidOperationException("CodeLogic options not available.");

    private CodeLogicConfiguration GetConfigOrThrow() =>
        _config ?? throw new InvalidOperationException("CodeLogic configuration not loaded.");

    private void ApplyDebugDefaults()
    {
        // No-op — development mode is handled via CodeLogic.Development.json.
        // LoadConfigurationAsync selects the right file via IsDevelopmentMode().
    }

    /// <summary>
    /// Returns true when running in development mode:
    ///   - Debugger is attached at runtime (any build), OR
    ///   - This is a DEBUG build (dotnet run without -c Release)
    ///
    /// Examples:
    ///   dotnet run              → DEBUG build  → Development.json ✓
    ///   dotnet run -c Release   → Release build, no debugger → CodeLogic.json
    ///   dotnet run + attach     → any build, debugger → Development.json ✓
    /// </summary>
    private static bool IsDevelopmentMode()
    {
        if (Debugger.IsAttached) return true;
#if DEBUG
        return true;
#else
        return false;
#endif
    }

    private ILogger CreateFrameworkLogger()
    {
        var opts    = GetOptionsOrThrow();
        var config  = GetConfigOrThrow();
        var logsDir = Path.Combine(opts.GetFrameworkPath(), "Framework", "logs");
        Directory.CreateDirectory(logsDir);

        var loggingOpts = config.Logging.ToLoggingOptions();

        // Framework logger always writes at least Info — startup messages
        // should always be visible and logged regardless of globalLevel.
        // Libraries and the application respect the configured level.
        if (loggingOpts.GlobalLevel > LogLevel.Info)
            loggingOpts.GlobalLevel = LogLevel.Info;

        loggingOpts.EnableConsoleOutput = true;
        loggingOpts.ConsoleMinimumLevel = LogLevel.Info;

        return new Logger("CODELOGIC", logsDir, LogLevel.Info, loggingOpts);
    }

    private async Task LoadConfigurationAsync()
    {
        var opts    = GetOptionsOrThrow();
        var devPath = opts.GetCodeLogicDevelopmentConfigPath();
        var basePath = opts.GetCodeLogicConfigPath();

        // Use CodeLogic.Development.json when:
        //   - Debugger is attached at runtime, OR
        //   - This is a Debug build (#if DEBUG)
        // Otherwise fall back to CodeLogic.json (production config).
        string configPath;
        if (IsDevelopmentMode() && File.Exists(devPath))
        {
            configPath = devPath;
            var reason = Debugger.IsAttached ? "debugger attached" : "DEBUG build";
            Console.WriteLine($"[CodeLogic] Using {Path.GetFileName(devPath)} ({reason})");
        }
        else
        {
            configPath = basePath;
        }

        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Config not found at: {configPath}");

        var json = await File.ReadAllTextAsync(configPath);
        _config = JsonSerializer.Deserialize<CodeLogicConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase
        }) ?? throw new InvalidOperationException($"Failed to deserialize {Path.GetFileName(configPath)}");
    }

    private ApplicationContext CreateApplicationContext()
    {
        var opts   = GetOptionsOrThrow();
        var config = GetConfigOrThrow();

        var appDir  = opts.GetApplicationPath();
        var logDir  = opts.GetApplicationLogsPath();
        var locDir  = opts.GetApplicationLocalizationPath();
        var dataDir = opts.GetApplicationDataPath();

        Directory.CreateDirectory(appDir);
        Directory.CreateDirectory(logDir);
        Directory.CreateDirectory(locDir);
        Directory.CreateDirectory(dataDir);

        var loggingOpts = config.Logging.ToLoggingOptions();
        var logger = new Core.Logging.Logger(
            "APPLICATION", logDir, loggingOpts.GlobalLevel, loggingOpts);

        return new ApplicationContext
        {
            ApplicationId         = _application!.Manifest.Id,
            ApplicationDirectory  = appDir,
            ConfigDirectory       = appDir,
            LocalizationDirectory = locDir,
            LogsDirectory         = logDir,
            DataDirectory         = dataDir,
            Logger                = logger,
            Configuration         = new Core.Configuration.ConfigurationManager(appDir),
            Localization          = new Core.Localization.LocalizationManager(locDir, config.Localization.DefaultCulture),
            Events                = _eventBus
        };
    }
}
