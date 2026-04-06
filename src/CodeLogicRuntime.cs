using System.Diagnostics;
using System.Text.Json;
using CodeLogic.Core.Events;
using CodeLogic.Core.Utilities;
using CodeLogic.Framework.Application;
using CodeLogic.Framework.Libraries;

namespace CodeLogic;

public sealed class CodeLogicRuntime : ICodeLogicRuntime
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly EventBus _eventBus = new();

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

            // Set app version on environment
            CodeLogicEnvironment.AppVersion = _options.AppVersion;

            // Apply debug-aware logging defaults
            ApplyDebugDefaults();

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

            return InitializationResult.Succeeded();
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
            Console.WriteLine($"  Registered application: {application.Manifest.Name} v{application.Manifest.Version}");
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
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ! {w}");
                Console.ResetColor();
            }
            if (!validation.IsSuccess)
                throw new InvalidOperationException($"Startup validation failed: {validation.ErrorMessage}");

            // Create library manager
            _libraryManager = new LibraryManager(_eventBus)
            {
                LoggingOptions             = config.Logging.ToLoggingOptions(),
                FrameworkRootPath          = opts.GetFrameworkPath(),
                DefaultCulture             = config.Localization.DefaultCulture,
                SupportedCultures          = config.Localization.SupportedCultures,
                EnableDependencyResolution = config.Libraries.EnableDependencyResolution,
                LibraryDiscoveryPattern    = config.Libraries.DiscoveryPattern
            };

            // Discover + load DLL-based libraries (if any)
            var discovered = _libraryManager.Discover();
            if (discovered.Count > 0)
                await _libraryManager.LoadLibrariesAsync(discovered);

            // Configure application
            if (_application != null)
            {
                Console.WriteLine($"\n  Configuring application: {_application.Manifest.Name}...");
                var appCtx = CreateApplicationContext();

                await _application.OnConfigureAsync(appCtx);
                await appCtx.Configuration.GenerateAllDefaultsAsync();
                await appCtx.Configuration.LoadAllAsync();
                await appCtx.Localization.GenerateAllTemplatesAsync(config.Localization.SupportedCultures);
                await appCtx.Localization.LoadAllAsync(config.Localization.SupportedCultures);

                _applicationContext = appCtx; // only assign after full success
                Console.WriteLine($"  Configured: {_application.Manifest.Name}");
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
                await _application.OnInitializeAsync(_applicationContext);
                await _application.OnStartAsync(_applicationContext);
                Console.WriteLine($"  Application started: {_application.Manifest.Name}");
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
                Console.WriteLine($"\n  Stopping application: {_application.Manifest.Name}...");
                try { await _application.OnStopAsync(); }
                catch (Exception ex) { Console.Error.WriteLine($"  ! Application stop error: {ex.Message}"); }
            }

            if (_libraryManager != null)
                await _libraryManager.StopAllAsync();
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
        var overall = libs.Values.All(h => h.IsHealthy);
        return new HealthReport
        {
            IsHealthy = overall,
            Libraries = libs,
        };
    }

    // ── Accessors ────────────────────────────────────────────────────────────

    public LibraryManager?    GetLibraryManager()       => _libraryManager;
    public IApplication?      GetApplication()           => _application;
    public ApplicationContext? GetApplicationContext()   => _applicationContext;
    public IEventBus          GetEventBus()              => _eventBus;
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
        // Debug defaults are applied when generating CodeLogic.json (FirstRunManager).
        // At runtime, the loaded config always wins.
        // We only need to note the debug state for diagnostics.
        if (Debugger.IsAttached)
            Console.WriteLine("  [Debug] Debugger attached — verbose defaults applied to new configs");
    }

    private async Task LoadConfigurationAsync()
    {
        var configPath = GetOptionsOrThrow().GetCodeLogicConfigPath();
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"CodeLogic.json not found at: {configPath}");

        var json = await File.ReadAllTextAsync(configPath);
        _config = JsonSerializer.Deserialize<CodeLogicConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? throw new InvalidOperationException("Failed to deserialize CodeLogic.json");
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
