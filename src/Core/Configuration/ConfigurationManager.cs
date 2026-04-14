using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeLogic.Core.Configuration;

/// <summary>
/// File-based JSON configuration manager for a single component.
/// Thread-safe for concurrent reads; writes use file system atomicity.
/// </summary>
public sealed class ConfigurationManager : IConfigurationManager
{
    private readonly string _baseDirectory;
    private readonly Dictionary<Type, object> _loaded = new();
    private readonly Dictionary<Type, string> _registered = new(); // Type → subConfigName

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>Initializes the configuration manager with the specified base directory.</summary>
    public ConfigurationManager(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
        Directory.CreateDirectory(baseDirectory);
    }

    /// <inheritdoc />
    public void Register<T>(string? subConfigName = null) where T : ConfigModelBase, new()
    {
        _registered[typeof(T)] = subConfigName ?? string.Empty;
    }

    /// <inheritdoc />
    public T Get<T>() where T : ConfigModelBase, new()
    {
        if (_loaded.TryGetValue(typeof(T), out var config))
            return (T)config;

        throw new InvalidOperationException(
            $"Configuration '{typeof(T).Name}' is not loaded. " +
            $"Call LoadAsync<{typeof(T).Name}>() or LoadAllAsync() first.");
    }

    /// <inheritdoc />
    public async Task GenerateDefaultAsync<T>(bool force = false) where T : ConfigModelBase, new()
    {
        var path = GetFilePath<T>();
        if (!force && File.Exists(path)) return;

        var defaultConfig = new T();
        await WriteToFileAsync(path, defaultConfig);
    }

    /// <inheritdoc />
    public async Task LoadAsync<T>(bool generateIfMissing = true) where T : ConfigModelBase, new()
    {
        var path = GetFilePath<T>();

        if (!File.Exists(path))
        {
            if (!generateIfMissing)
                throw new FileNotFoundException(
                    $"Config file for '{typeof(T).Name}' not found at: {path}");

            await GenerateDefaultAsync<T>();
        }

        await LoadExistingAsync<T>();
    }

    /// <inheritdoc />
    public async Task SaveAsync<T>(T config) where T : ConfigModelBase, new()
    {
        // Validate BEFORE writing — never persist invalid config
        var validation = config.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"Cannot save invalid configuration '{typeof(T).Name}': {validation}");

        var path = GetFilePath<T>();
        await WriteToFileAsync(path, config);

        // Update in-memory cache
        _loaded[typeof(T)] = config;
    }

    /// <inheritdoc />
    public async Task GenerateAllDefaultsAsync(bool force = false)
    {
        foreach (var type in _registered.Keys)
        {
            var method = typeof(ConfigurationManager)
                .GetMethod(nameof(GenerateDefaultAsync), BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException(
                    $"Reflection failed: method '{nameof(GenerateDefaultAsync)}' not found.");

            var generic = method.MakeGenericMethod(type);
            var task = generic.Invoke(this, [force]) as Task
                ?? throw new InvalidOperationException(
                    $"Reflection failed: '{nameof(GenerateDefaultAsync)}<{type.Name}>' returned null.");

            await task;
        }
    }

    /// <inheritdoc />
    public async Task LoadAllAsync(bool generateIfMissing = true)
    {
        foreach (var type in _registered.Keys)
        {
            var method = typeof(ConfigurationManager)
                .GetMethod(nameof(LoadAsync), BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException(
                    $"Reflection failed: method '{nameof(LoadAsync)}' not found.");

            var generic = method.MakeGenericMethod(type);
            var task = generic.Invoke(this, [generateIfMissing]) as Task
                ?? throw new InvalidOperationException(
                    $"Reflection failed: '{nameof(LoadAsync)}<{type.Name}>' returned null.");

            await task;
        }
    }

    /// <inheritdoc />
    public async Task ValidateAllAsync(bool allowMissingFiles = false)
    {
        foreach (var type in _registered.Keys)
        {
            var path = GetFilePath(type);
            if (!File.Exists(path))
            {
                if (allowMissingFiles)
                    continue;

                throw new FileNotFoundException(
                    $"Config file for '{type.Name}' not found at: {path}");
            }

            await LoadExistingAsync(type);
        }
    }

    /// <inheritdoc />
    public async Task ReloadAsync<T>() where T : ConfigModelBase, new()
    {
        // Same as LoadAsync but always re-reads from disk even if already loaded
        var path = GetFilePath<T>();

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Config file for '{typeof(T).Name}' not found at: {path}. " +
                $"Call GenerateDefaultAsync<{typeof(T).Name}>() first.");

        var json = await File.ReadAllTextAsync(path);
        var config = JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize '{typeof(T).Name}' from {path}");

        var validation = config.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"Reloaded configuration '{typeof(T).Name}' is invalid: {validation}");

        _loaded[typeof(T)] = config;
    }

    /// <inheritdoc />
    public async Task ReloadAllAsync()
    {
        foreach (var type in _registered.Keys)
        {
            var method = typeof(ConfigurationManager)
                .GetMethod(nameof(ReloadAsync), BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException(
                    $"Reflection failed: method '{nameof(ReloadAsync)}' not found.");

            var generic = method.MakeGenericMethod(type);
            var task = generic.Invoke(this, null) as Task
                ?? throw new InvalidOperationException(
                    $"Reflection failed: '{nameof(ReloadAsync)}<{type.Name}>' returned null.");

            await task;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRegisteredFilePaths() =>
        _registered.Keys.Select(GetFilePath).ToList();

    private string GetFilePath<T>() where T : ConfigModelBase, new()
    {
        return GetFilePath(typeof(T));
    }

    private async Task WriteToFileAsync<T>(string path, T config) where T : ConfigModelBase, new()
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private string GetFilePath(Type type)
    {
        if (!_registered.TryGetValue(type, out var subName))
            throw new InvalidOperationException(
                $"Configuration type '{type.Name}' is not registered. " +
                $"Call Register<{type.Name}>() in OnConfigureAsync first.");

        var fileName = string.IsNullOrEmpty(subName) ? "config.json" : $"config.{subName}.json";
        return Path.Combine(_baseDirectory, fileName);
    }

    private async Task LoadExistingAsync(Type type)
    {
        var method = typeof(ConfigurationManager)
            .GetMethod(nameof(LoadExistingAsync), BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
            ?? throw new InvalidOperationException(
                $"Reflection failed: method '{nameof(LoadExistingAsync)}' not found.");

        var generic = method.MakeGenericMethod(type);
        var task = generic.Invoke(this, null) as Task
            ?? throw new InvalidOperationException(
                $"Reflection failed: '{nameof(LoadExistingAsync)}<{type.Name}>' returned null.");

        await task;
    }

    private async Task LoadExistingAsync<T>() where T : ConfigModelBase, new()
    {
        var path = GetFilePath<T>();
        var json = await File.ReadAllTextAsync(path);
        var config = JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize '{typeof(T).Name}' from {path}");

        var validation = config.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"Configuration '{typeof(T).Name}' is invalid: {validation}");

        _loaded[typeof(T)] = config;
    }
}
