using CodeLogic.Core.Logging;

namespace CodeLogic;

public sealed class CodeLogicConfiguration
{
    public FrameworkConfig Framework { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public LocalizationConfig Localization { get; set; } = new();
    public LibrariesConfig Libraries { get; set; } = new();
    public HealthChecksConfig HealthChecks { get; set; } = new();
}

public sealed class FrameworkConfig
{
    public string Name { get; set; } = "CodeLogic";
    public string Version { get; set; } = "3.0.0";
}

public sealed class LoggingConfig
{
    public string Mode { get; set; } = "singleFile";
    public int MaxFileSizeMb { get; set; } = 10;
    public int MaxRolledFiles { get; set; } = 5;
    public string FileNamePattern { get; set; } = "{date:yyyy}/{date:MM}/{date:dd}/{level}.log";
    public string GlobalLevel { get; set; } = "Warning";
    public bool EnableConsoleOutput { get; set; } = false;
    public string ConsoleMinimumLevel { get; set; } = "Debug";
    public bool EnableDebugMode { get; set; } = false;
    public bool CentralizedDebugLog { get; set; } = false;
    public string? CentralizedLogsPath { get; set; }
    public bool IncludeMachineName { get; set; } = true;
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

    public LogLevel GetGlobalLogLevel() => ParseLevel(GlobalLevel, LogLevel.Warning);
    public LogLevel GetConsoleLogLevel() => ParseLevel(ConsoleMinimumLevel, LogLevel.Debug);

    private static LogLevel ParseLevel(string value, LogLevel fallback) =>
        Enum.TryParse<LogLevel>(value, ignoreCase: true, out var level) ? level : fallback;

    public LoggingOptions ToLoggingOptions() => new()
    {
        Mode                = Mode == "dateFolder" ? Core.Logging.LoggingMode.DateFolder : Core.Logging.LoggingMode.SingleFile,
        MaxFileSizeMb       = MaxFileSizeMb,
        MaxRolledFiles      = MaxRolledFiles,
        FileNamePattern     = FileNamePattern,
        GlobalLevel         = GetGlobalLogLevel(),
        EnableDebugMode     = EnableDebugMode,
        CentralizedDebugLog = CentralizedDebugLog,
        CentralizedLogsPath = CentralizedLogsPath,
        EnableConsoleOutput = EnableConsoleOutput,
        ConsoleMinimumLevel = GetConsoleLogLevel(),
        TimestampFormat     = TimestampFormat,
        IncludeMachineName  = IncludeMachineName
    };
}

public sealed class LocalizationConfig
{
    public string DefaultCulture { get; set; } = "en-US";
    public List<string> SupportedCultures { get; set; } = ["en-US"];
    public bool AutoGenerateTemplates { get; set; } = true;
}

public sealed class LibrariesConfig
{
    public string DiscoveryPattern { get; set; } = "CL.*";
    public bool EnableDependencyResolution { get; set; } = true;
}

public sealed class HealthChecksConfig
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 30;
}
