namespace CodeLogic;

public sealed class CodeLogicOptions
{
    // === Paths ===
    /// <summary>Root for framework files: CodeLogic.json, Libraries/, etc.</summary>
    public string FrameworkRootPath { get; set; } = "CodeLogic";

    /// <summary>Root for application files: config, localization, logs, data.
    /// Defaults to {FrameworkRootPath}/Application if null.</summary>
    public string? ApplicationRootPath { get; set; } = null;

    // === App identity ===
    public string AppVersion { get; set; } = "0.0.0";

    // === Config generation ===
    public bool GenerateConfigs { get; set; } = true;
    public bool GenerateConfigsForce { get; set; } = false;
    public string[]? GenerateConfigsFor { get; set; } = null; // null = all
    public bool ExitAfterGenerate { get; set; } = false;

    // === Shutdown ===
    public bool HandleShutdownSignals { get; set; } = true;

    // === Path helpers ===
    public string GetFrameworkPath() =>
        Path.Combine(AppContext.BaseDirectory, FrameworkRootPath);

    public string GetCodeLogicConfigPath() =>
        Path.Combine(GetFrameworkPath(), "Framework", "CodeLogic.json");

    /// <summary>
    /// Path to the development config overlay.
    /// Loaded instead of CodeLogic.json when Debugger.IsAttached.
    /// Add this file to .gitignore — it is per-machine, never committed.
    /// </summary>
    public string GetCodeLogicDevelopmentConfigPath() =>
        Path.Combine(GetFrameworkPath(), "Framework", "CodeLogic.Development.json");

    public string GetLibrariesPath() =>
        Path.Combine(GetFrameworkPath(), "Libraries");

    public string GetLibraryPath(string libraryId) =>
        Path.Combine(GetLibrariesPath(), NormalizeLibraryId(libraryId));

    public string GetApplicationPath() =>
        ApplicationRootPath != null
            ? Path.Combine(AppContext.BaseDirectory, ApplicationRootPath)
            : Path.Combine(GetFrameworkPath(), "Application");

    public string GetApplicationConfigPath()       => GetApplicationPath();
    public string GetApplicationLocalizationPath() => Path.Combine(GetApplicationPath(), "localization");
    public string GetApplicationLogsPath()         => Path.Combine(GetApplicationPath(), "logs");
    public string GetApplicationDataPath()         => Path.Combine(GetApplicationPath(), "data");

    public string GetPluginsPath() =>
        Path.Combine(GetFrameworkPath(), "Plugins");

    private static string NormalizeLibraryId(string id) =>
        id.StartsWith("CL.", StringComparison.OrdinalIgnoreCase) ? $"CL.{id[3..]}" : $"CL.{id}";
}
