namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// Tracks the lifecycle state of a loaded plugin.
/// Mirrors LibraryState but for plugins.
/// </summary>
public enum PluginState
{
    Discovered,   // Found on disk but not yet loaded
    Loaded,       // Assembly loaded, instance created
    Configured,   // OnConfigureAsync completed
    Initialized,  // OnInitializeAsync completed
    Started,      // OnStartAsync completed — fully operational
    Stopped,      // OnUnloadAsync completed
    Failed        // Exception during any lifecycle phase
}
