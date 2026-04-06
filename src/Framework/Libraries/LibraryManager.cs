using System.Reflection;
using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Core.Logging;
using CodeLogic.Core.Utilities;

namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Manages the full lifecycle of all registered CL.* libraries.
/// Thread-safe: lifecycle methods are serialized via SemaphoreSlim.
/// </summary>
public sealed class LibraryManager : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly List<LoadedLibrary> _libraries = [];
    private readonly Dictionary<string, ILibrary> _librariesById = new();
    private readonly IEventBus _eventBus;
    private System.Threading.Timer? _healthCheckTimer;

    // Configuration — set by CodeLogicRuntime after loading CodeLogic.json
    public LoggingOptions LoggingOptions { get; set; } = new();
    public string FrameworkRootPath { get; set; } = "CodeLogic";
    public string DefaultCulture { get; set; } = "en-US";
    public IReadOnlyList<string> SupportedCultures { get; set; } = ["en-US"];
    public bool EnableDependencyResolution { get; set; } = true;
    public string LibraryDiscoveryPattern { get; set; } = "CL.*";

    public LibraryManager(IEventBus eventBus)
    {
        _eventBus = eventBus;
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    // ── Discovery ────────────────────────────────────────────────────────────

    public List<string> Discover()
    {
        var librariesRoot = Path.Combine(FrameworkRootPath, "Libraries");
        var paths = new List<string>();

        if (!Directory.Exists(librariesRoot)) return paths;

        var dirs = Directory.GetDirectories(librariesRoot, LibraryDiscoveryPattern, SearchOption.TopDirectoryOnly);
        foreach (var dir in dirs)
        {
            var name = Path.GetFileName(dir);
            var dll  = Path.Combine(dir, $"{name}.dll");
            if (File.Exists(dll)) paths.Add(dll);
        }
        return paths;
    }

    // ── Loading ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Manually registers a library by type.
    /// Dependency validation is deferred to ConfigureAllAsync which runs
    /// ValidateDependencies after all libraries are registered.
    /// </summary>
    public async Task<bool> LoadLibraryAsync<T>() where T : class, ILibrary, new()
    {
        await _lock.WaitAsync();
        try
        {
            var library  = new T();
            var manifest = library.Manifest;

            if (_librariesById.ContainsKey(manifest.Id))
            {
                Console.WriteLine($"  ⚠ Library '{manifest.Name}' already loaded");
                return false;
            }

            _libraries.Add(new LoadedLibrary
            {
                Instance     = library,
                Manifest     = manifest,
                AssemblyPath = typeof(T).Assembly.Location
            });
            _librariesById[manifest.Id] = library;

            Console.WriteLine($"  ✓ Manually loaded: {manifest.Name} v{manifest.Version}");
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task LoadLibrariesAsync(IReadOnlyList<string> libraryPaths)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var path in libraryPaths)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(path);
                    var type     = FindLibraryType(assembly);
                    if (type == null)
                    {
                        Console.WriteLine($"  ⚠ No ILibrary found in {Path.GetFileName(path)}");
                        continue;
                    }

                    var library  = (ILibrary)Activator.CreateInstance(type)!;
                    var manifest = library.Manifest;

                    if (_librariesById.ContainsKey(manifest.Id))
                        throw new InvalidOperationException($"Duplicate library ID '{manifest.Id}'");

                    _libraries.Add(new LoadedLibrary
                    {
                        Instance = library, Manifest = manifest, AssemblyPath = path
                    });
                    _librariesById[manifest.Id] = library;
                    Console.WriteLine($"  ✓ Discovered: {manifest.Name} v{manifest.Version}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Failed to load {Path.GetFileName(path)}: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
        finally { _lock.Release(); }
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public async Task ConfigureAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            ValidateDependencies(GetOrderedLibraries());

            foreach (var loaded in GetOrderedLibraries())
            {
                if (loaded.State != LibraryState.Loaded) continue;

                try
                {
                    var ctx = CreateContext(loaded.Manifest.Id);
                    await loaded.Instance.OnConfigureAsync(ctx);
                    await ctx.Configuration.GenerateAllDefaultsAsync();
                    await ctx.Configuration.LoadAllAsync();
                    await ctx.Localization.GenerateAllTemplatesAsync(SupportedCultures);
                    await ctx.Localization.LoadAllAsync(SupportedCultures);

                    loaded.Context = ctx;
                    loaded.State   = LibraryState.Configured;
                    Console.WriteLine($"  ✓ Configured: {loaded.Manifest.Name}");
                }
                catch (Exception ex)
                {
                    loaded.State            = LibraryState.Failed;
                    loaded.FailureException = ex;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Failed to configure {loaded.Manifest.Name}: {ex.Message}");
                    Console.ResetColor();
                    throw;
                }
            }
        }
        finally { _lock.Release(); }
    }

    public async Task InitializeAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var loaded in GetOrderedLibraries())
            {
                if (loaded.State == LibraryState.Failed) continue;
                if (loaded.State != LibraryState.Configured)
                    throw new InvalidOperationException(
                        $"Library '{loaded.Manifest.Name}' must be Configured before Initialize. State: {loaded.State}");

                try
                {
                    await loaded.Instance.OnInitializeAsync(loaded.Context!);
                    loaded.State = LibraryState.Initialized;
                    Console.WriteLine($"  ✓ Initialized: {loaded.Manifest.Name}");
                }
                catch (Exception ex)
                {
                    loaded.State = LibraryState.Failed;
                    loaded.FailureException = ex;
                    _eventBus.Publish(new LibraryFailedEvent(loaded.Manifest.Id, loaded.Manifest.Name, ex));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Failed to initialize {loaded.Manifest.Name}: {ex.Message}");
                    Console.ResetColor();
                    throw;
                }
            }
        }
        finally { _lock.Release(); }
    }

    public async Task StartAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var loaded in GetOrderedLibraries())
            {
                if (loaded.State == LibraryState.Failed) continue;
                if (loaded.State != LibraryState.Initialized)
                    throw new InvalidOperationException(
                        $"Library '{loaded.Manifest.Name}' must be Initialized before Start. State: {loaded.State}");

                try
                {
                    await loaded.Instance.OnStartAsync(loaded.Context!);
                    loaded.State = LibraryState.Started;
                    _eventBus.Publish(new LibraryStartedEvent(loaded.Manifest.Id, loaded.Manifest.Name));
                    Console.WriteLine($"  ✓ Started: {loaded.Manifest.Name}");
                }
                catch (Exception ex)
                {
                    loaded.State = LibraryState.Failed;
                    loaded.FailureException = ex;
                    _eventBus.Publish(new LibraryFailedEvent(loaded.Manifest.Id, loaded.Manifest.Name, ex));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Failed to start {loaded.Manifest.Name}: {ex.Message}");
                    Console.ResetColor();
                    throw;
                }
            }
        }
        finally { _lock.Release(); }
    }

    public async Task StopAllAsync()
    {
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;

        await _lock.WaitAsync();
        try
        {
            foreach (var loaded in GetOrderedLibraries(reverse: true))
            {
                if (loaded.State != LibraryState.Started) continue;
                try
                {
                    await loaded.Instance.OnStopAsync();
                    loaded.State = LibraryState.Stopped;
                    _eventBus.Publish(new LibraryStoppedEvent(loaded.Manifest.Id, loaded.Manifest.Name));
                    Console.WriteLine($"  ✓ Stopped: {loaded.Manifest.Name}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Error stopping {loaded.Manifest.Name}: {ex.Message}");
                    Console.ResetColor();
                    // Don't rethrow on stop — try to stop everything
                }
            }
        }
        finally { _lock.Release(); }
    }

    // ── Health checks ────────────────────────────────────────────────────────

    public void StartHealthCheckTimer(int intervalSeconds)
    {
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = new System.Threading.Timer(
            _ => _ = RunHealthChecksAsync(),
            null,
            TimeSpan.FromSeconds(intervalSeconds),
            TimeSpan.FromSeconds(intervalSeconds));
    }

    public async Task<Dictionary<string, HealthStatus>> GetHealthAsync()
    {
        var results = new Dictionary<string, HealthStatus>();
        foreach (var loaded in _libraries.Where(l => l.State == LibraryState.Started))
        {
            try
            {
                results[loaded.Manifest.Id] = await loaded.Instance.HealthCheckAsync();
            }
            catch (Exception ex)
            {
                results[loaded.Manifest.Id] = HealthStatus.FromException(ex);
            }
        }
        return results;
    }

    private async Task RunHealthChecksAsync()
    {
        try
        {
            var results = await GetHealthAsync();
            foreach (var (id, status) in results)
            {
                _eventBus.Publish(new HealthCheckCompletedEvent(id, status.IsHealthy, status.Message));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[LibraryManager] Health check error: {ex.Message}");
        }
    }

    // ── Accessors ────────────────────────────────────────────────────────────

    public T? GetLibrary<T>() where T : class, ILibrary =>
        _libraries.Select(l => l.Instance as T).FirstOrDefault(l => l != null);

    public ILibrary? GetLibrary(string id) =>
        _librariesById.GetValueOrDefault(id);

    public IEnumerable<ILibrary> GetAllLibraries() =>
        _libraries.Select(l => l.Instance);

    public IEnumerable<LoadedLibrary> GetLoadedLibraries() => _libraries.AsReadOnly();

    // ── Private helpers ──────────────────────────────────────────────────────

    private LibraryContext CreateContext(string libraryId)
    {
        var root    = Path.Combine(FrameworkRootPath, "Libraries");
        var libDir  = Path.Combine(root, NormalizeId(libraryId));
        var logsDir = Path.Combine(libDir, "logs");
        var dataDir = Path.Combine(libDir, "data");
        var locDir  = Path.Combine(libDir, "localization");

        Directory.CreateDirectory(libDir);
        Directory.CreateDirectory(logsDir);
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(locDir);

        var logger = new Logger(
            libraryId.ToUpper(), logsDir, LoggingOptions.GlobalLevel, LoggingOptions);

        return new LibraryContext
        {
            LibraryId             = libraryId,
            LibraryDirectory      = libDir,
            ConfigDirectory       = libDir,
            LocalizationDirectory = locDir,
            LogsDirectory         = logsDir,
            DataDirectory         = dataDir,
            Logger                = logger,
            Configuration         = new ConfigurationManager(libDir),
            Localization          = new LocalizationManager(locDir, DefaultCulture),
            Events                = _eventBus
        };
    }

    private List<LoadedLibrary> GetOrderedLibraries(bool reverse = false)
    {
        var ordered = EnableDependencyResolution
            ? TopologicalSort()
            : _libraries.ToList();
        if (reverse) ordered.Reverse();
        return ordered;
    }

    private List<LoadedLibrary> TopologicalSort()
    {
        var sorted   = new List<LoadedLibrary>();
        var visited  = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var lib in _libraries)
            Visit(lib, sorted, visited, visiting);

        return sorted;
    }

    private void Visit(LoadedLibrary lib, List<LoadedLibrary> sorted,
        HashSet<string> visited, HashSet<string> visiting)
    {
        if (visited.Contains(lib.Manifest.Id)) return;
        if (visiting.Contains(lib.Manifest.Id))
            throw new InvalidOperationException($"Circular dependency: {lib.Manifest.Id}");

        visiting.Add(lib.Manifest.Id);

        foreach (var dep in lib.Manifest.Dependencies)
        {
            var depLib = _libraries.FirstOrDefault(l => l.Manifest.Id == dep.Id);
            if (depLib == null && dep.IsOptional) continue;
            if (depLib != null) Visit(depLib, sorted, visited, visiting);
        }

        visiting.Remove(lib.Manifest.Id);
        visited.Add(lib.Manifest.Id);
        sorted.Add(lib);
    }

    private void ValidateDependencies(IEnumerable<LoadedLibrary> ordered)
    {
        var errors = new List<string>();
        foreach (var lib in ordered)
        {
            foreach (var dep in lib.Manifest.Dependencies)
            {
                var found = _libraries.FirstOrDefault(l => l.Manifest.Id == dep.Id);
                if (found == null)
                {
                    if (!dep.IsOptional)
                        errors.Add($"'{lib.Manifest.Name}' requires missing dependency '{dep.Id}'" +
                            (dep.MinVersion != null ? $" >= {dep.MinVersion}" : ""));
                    continue;
                }

                if (dep.MinVersion != null &&
                    SemanticVersion.TryParse(found.Manifest.Version, out var installed) &&
                    SemanticVersion.TryParse(dep.MinVersion, out var required) &&
                    installed != null && required != null &&
                    installed < required)
                {
                    errors.Add($"'{lib.Manifest.Name}' requires '{dep.Id}' >= {dep.MinVersion}, " +
                        $"but {installed} is loaded");
                }
            }
        }
        if (errors.Count > 0)
            throw new InvalidOperationException("Dependency validation failed:\n  " + string.Join("\n  ", errors));
    }

    private static string NormalizeId(string id) =>
        id.StartsWith("CL.", StringComparison.OrdinalIgnoreCase) ? $"CL.{id[3..]}" : $"CL.{id}";

    private static Type? FindLibraryType(Assembly assembly) =>
        assembly.GetTypes().FirstOrDefault(t =>
            typeof(ILibrary).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

    private Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name);
        foreach (var lib in _libraries)
        {
            var dir  = Path.GetDirectoryName(lib.AssemblyPath);
            if (dir == null) continue;
            var path = Path.Combine(dir, name.Name + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
    }
}
