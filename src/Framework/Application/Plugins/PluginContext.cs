using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Core.Logging;

namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// Context provided to plugins at every lifecycle phase.
/// Full parity with LibraryContext — includes Config, Localization, Events, and Logger.
/// </summary>
public sealed class PluginContext
{
    public required string PluginId { get; init; }
    public required string PluginDirectory { get; init; }
    public required string ConfigDirectory { get; init; }
    public required string LocalizationDirectory { get; init; }
    public required string LogsDirectory { get; init; }
    public required string DataDirectory { get; init; }

    public required ILogger Logger { get; init; }
    public required IConfigurationManager Configuration { get; init; }
    public required ILocalizationManager Localization { get; init; }
    public required IEventBus Events { get; init; }
}
