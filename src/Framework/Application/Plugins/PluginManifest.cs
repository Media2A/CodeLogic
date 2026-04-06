namespace CodeLogic.Framework.Application.Plugins;

public sealed class PluginManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }

    /// <summary>Minimum CodeLogic framework version required.</summary>
    public string? MinFrameworkVersion { get; init; }
}
