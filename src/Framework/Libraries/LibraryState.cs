namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Tracks the lifecycle state of a loaded library.
/// States advance sequentially. Failed can be set from any phase.
/// </summary>
public enum LibraryState
{
    Loaded,       // Added to LibraryManager but not yet configured
    Configured,   // OnConfigureAsync completed, config/localization loaded
    Initialized,  // OnInitializeAsync completed
    Started,      // OnStartAsync completed — fully operational
    Stopped,      // OnStopAsync completed
    Failed        // An exception occurred during any lifecycle phase
}
