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

    /// <summary>Generates the config file with defaults if it doesn't exist.</summary>
    Task GenerateDefaultAsync<T>() where T : ConfigModelBase, new();

    /// <summary>Loads a config from disk. Generates defaults if missing. Validates after load.</summary>
    Task LoadAsync<T>() where T : ConfigModelBase, new();

    /// <summary>
    /// Saves config to disk. Validates before writing — throws if invalid.
    /// </summary>
    Task SaveAsync<T>(T config) where T : ConfigModelBase, new();

    /// <summary>Generates defaults for all registered types that don't have files yet.</summary>
    Task GenerateAllDefaultsAsync();

    /// <summary>Loads all registered configs.</summary>
    Task LoadAllAsync();

    /// <summary>
    /// Reloads a specific config from disk and updates the in-memory instance.
    /// Safe for: log levels, intervals, pool sizes.
    /// NOT safe for: connection strings, paths, core settings (requires restart).
    /// </summary>
    Task ReloadAsync<T>() where T : ConfigModelBase, new();

    /// <summary>Reloads all registered configs from disk.</summary>
    Task ReloadAllAsync();
}
