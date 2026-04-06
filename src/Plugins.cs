using CodeLogic.Framework.Application.Plugins;

namespace CodeLogic;

/// <summary>
/// Convenience helpers for plugin management.
/// Apps typically create and manage their own PluginManager instance.
/// Pass IEventBus from CodeLogic.GetEventBus() when constructing.
/// </summary>
public static class Plugins
{
    /// <summary>
    /// Creates a new PluginManager wired to the shared CodeLogic event bus.
    /// </summary>
    public static PluginManager CreateManager(PluginOptions? options = null) =>
        new(CodeLogic.GetEventBus(), options);
}
