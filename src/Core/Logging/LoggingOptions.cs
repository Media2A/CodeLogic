namespace CodeLogic.Core.Logging;

public class LoggingOptions
{
    // Mode
    public LoggingMode Mode { get; set; } = LoggingMode.SingleFile;

    // SingleFile settings
    public int MaxFileSizeMb { get; set; } = 10;
    public int MaxRolledFiles { get; set; } = 5;

    // DateFolder settings
    public string FileNamePattern { get; set; } = "{date:yyyy}/{date:MM}/{date:dd}/{level}.log";

    // Levels
    public LogLevel GlobalLevel { get; set; } = LogLevel.Warning;
    public bool EnableDebugMode { get; set; } = false;
    public bool CentralizedDebugLog { get; set; } = false;
    public string? CentralizedLogsPath { get; set; }

    // Console
    public bool EnableConsoleOutput { get; set; } = false;
    public LogLevel ConsoleMinimumLevel { get; set; } = LogLevel.Debug;

    // Format
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
    public bool IncludeMachineName { get; set; } = true;

    /// <summary>
    /// Creates LoggingOptions with debug-aware defaults.
    /// If a debugger is attached: verbose + console on.
    /// If not: quiet + file only.
    /// Individual properties can still be overridden after calling this.
    /// </summary>
    public static LoggingOptions CreateWithDebugDefaults()
    {
        var opts = new LoggingOptions();
        if (System.Diagnostics.Debugger.IsAttached)
        {
            opts.GlobalLevel = LogLevel.Debug;
            opts.EnableConsoleOutput = true;
            opts.ConsoleMinimumLevel = LogLevel.Debug;
            opts.EnableDebugMode = true;
        }
        return opts;
    }
}
