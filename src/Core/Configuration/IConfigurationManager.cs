namespace CodeLogic.Core.Configuration;

/// <summary>
/// Manages configuration files for a single component (library, application, or plugin).
/// Supports multiple config files per component:
///   Register&lt;MainConfig&gt;()           → config.json
///   Register&lt;DatabaseConfig&gt;("db")   → config.db.json
/// </summary>
public interface IConfigurationManager
{
    /// <summary>Registers a config type for this component.</summary>
    void Register<T>(string? subConfigName = null) where T : ConfigModelBase, new();

    /// <summary>Gets a loaded config instance. Throws if not loaded.</summary>
    T Get<T>() where T : ConfigModelBase, new();

    /// <summary>Generates the config file with defaults if it doesn't exist, or overwrites it when <paramref name="force"/> is true.</summary>
    Task GenerateDefaultAsync<T>(bool force = false) where T : ConfigModelBase, new();

    /// <summary>Loads a config from disk. Generates defaults if missing when <paramref name="generateIfMissing"/> is true. Validates after load.</summary>
    Task LoadAsync<T>(bool generateIfMissing = true) where T : ConfigModelBase, new();

    /// <summary>
    /// Saves config to disk. Validates before writing — throws if invalid.
    /// </summary>
    Task SaveAsync<T>(T config) where T : ConfigModelBase, new();

    /// <summary>Generates defaults for all registered types that don't have files yet, or overwrites them when <paramref name="force"/> is true.</summary>
    Task GenerateAllDefaultsAsync(bool force = false);

    /// <summary>Loads all registered configs. Generates missing files only when <paramref name="generateIfMissing"/> is true.</summary>
    Task LoadAllAsync(bool generateIfMissing = true);

    /// <summary>Validates all registered config files that already exist on disk. Missing files are allowed only when <paramref name="allowMissingFiles"/> is true.</summary>
    Task ValidateAllAsync(bool allowMissingFiles = false);

    /// <summary>
    /// Reloads a specific config from disk and updates the in-memory instance.
    /// Safe for: log levels, intervals, pool sizes.
    /// NOT safe for: connection strings, paths, core settings (requires restart).
    /// </summary>
    Task ReloadAsync<T>() where T : ConfigModelBase, new();

    /// <summary>Reloads all registered configs from disk.</summary>
    Task ReloadAllAsync();

    /// <summary>Returns the on-disk paths for all registered config files.</summary>
    IReadOnlyList<string> GetRegisteredFilePaths();
}
