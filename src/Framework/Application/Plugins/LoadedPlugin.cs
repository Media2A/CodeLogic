namespace CodeLogic.Framework.Application.Plugins;

public sealed class LoadedPlugin
{
    public required IPlugin Instance { get; init; }
    public required PluginManifest Manifest { get; init; }
    public required string AssemblyPath { get; init; }
    public required PluginLoadContext LoadContext { get; init; }
    public WeakReference? WeakReference { get; init; }
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;

    public PluginContext? Context { get; set; }
    public PluginState State { get; set; } = PluginState.Loaded;
    public Exception? FailureException { get; set; }
}
