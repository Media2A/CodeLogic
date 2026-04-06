namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Internal record of a library registered with LibraryManager.
/// </summary>
public sealed class LoadedLibrary
{
    public required ILibrary Instance { get; init; }
    public required LibraryManifest Manifest { get; init; }
    public required string AssemblyPath { get; init; }
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Context assigned after OnConfigureAsync completes.</summary>
    public LibraryContext? Context { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public LibraryState State { get; set; } = LibraryState.Loaded;

    /// <summary>Exception if State == Failed.</summary>
    public Exception? FailureException { get; set; }
}
