using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Core.Logging;

namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Context provided to each library at every lifecycle phase.
/// All paths and services are scoped to this specific library.
/// </summary>
public sealed class LibraryContext
{
    public required string LibraryId { get; init; }
    public required string LibraryDirectory { get; init; }
    public required string ConfigDirectory { get; init; }
    public required string LocalizationDirectory { get; init; }
    public required string LogsDirectory { get; init; }
    public required string DataDirectory { get; init; }

    public required ILogger Logger { get; init; }
    public required IConfigurationManager Configuration { get; init; }
    public required ILocalizationManager Localization { get; init; }
    public required IEventBus Events { get; init; }  // shared instance
}
