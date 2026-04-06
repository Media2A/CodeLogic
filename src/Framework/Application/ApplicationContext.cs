using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Core.Logging;

namespace CodeLogic.Framework.Application;

/// <summary>
/// Context provided to the consuming application during its lifecycle phases.
/// Mirrors LibraryContext but scoped to the Application directory.
/// </summary>
public sealed class ApplicationContext
{
    public required string ApplicationId { get; init; }
    public required string ApplicationDirectory { get; init; }
    public required string ConfigDirectory { get; init; }
    public required string LocalizationDirectory { get; init; }
    public required string LogsDirectory { get; init; }
    public required string DataDirectory { get; init; }

    public required ILogger Logger { get; init; }
    public required IConfigurationManager Configuration { get; init; }
    public required ILocalizationManager Localization { get; init; }
    public required IEventBus Events { get; init; }
}
