using System.Diagnostics;
using System.Text.Json;

namespace CodeLogic.Core.Utilities;

/// <summary>
/// Manages first-run detection and directory/config scaffolding.
/// Does NOT exit after first run — scaffolds and returns, caller decides what to do.
/// </summary>
public static class FirstRunManager
{
    private const string MarkerFileName = ".codelogic";

    public static bool IsFirstRun(string frameworkRootPath)
    {
        return !File.Exists(GetMarkerPath(frameworkRootPath));
    }

    /// <summary>
    /// Performs first-run scaffolding:
    /// - Creates required directory structure
    /// - Generates CodeLogic.json with debug-aware defaults
    /// - Creates .codelogic marker
    /// Returns how many directories were created.
    /// </summary>
    public static async Task<FirstRunResult> ScaffoldAsync(string frameworkRootPath, string? applicationRootPath = null)
    {
        var result = new FirstRunResult();
        applicationRootPath ??= Path.Combine(frameworkRootPath, "Application");

        try
        {
            CreateDirectories(frameworkRootPath, applicationRootPath, result);
            await GenerateCodeLogicJsonAsync(frameworkRootPath);
            await CreateMarkerAsync(frameworkRootPath);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }

    public static async Task CompleteAsync(string frameworkRootPath)
    {
        await CreateMarkerAsync(frameworkRootPath);
    }

    public static void Reset(string frameworkRootPath)
    {
        var marker = GetMarkerPath(frameworkRootPath);
        if (File.Exists(marker)) File.Delete(marker);
    }

    private static void CreateDirectories(string frameworkRoot, string appRoot, FirstRunResult result)
    {
        var dirs = new[]
        {
            frameworkRoot,
            Path.Combine(frameworkRoot, "Framework"),
            Path.Combine(frameworkRoot, "Framework", "logs"),
            Path.Combine(frameworkRoot, "Libraries"),
            appRoot,
            Path.Combine(appRoot, "localization"),
            Path.Combine(appRoot, "logs"),
            Path.Combine(appRoot, "data"),
        };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                result.DirectoriesCreated++;
            }
        }
    }

    private static async Task GenerateCodeLogicJsonAsync(string frameworkRoot)
    {
        var configPath = Path.Combine(frameworkRoot, "Framework", "CodeLogic.json");
        if (File.Exists(configPath)) return;

        bool isDebugging = Debugger.IsAttached;

        // Build config as anonymous object — no dependency on CodeLogicConfiguration yet
        var config = new
        {
            framework = new { name = "CodeLogic", version = "3.0.0" },
            logging = new
            {
                mode = "singleFile",
                maxFileSizeMb = 10,
                maxRolledFiles = 5,
                globalLevel = isDebugging ? "Debug" : "Warning",
                enableConsoleOutput = isDebugging,
                consoleMinimumLevel = "Debug",
                enableDebugMode = isDebugging,
                centralizedDebugLog = false,
                includeMachineName = true,
                timestampFormat = "yyyy-MM-dd HH:mm:ss.fff"
            },
            localization = new
            {
                defaultCulture = "en-US",
                supportedCultures = new[] { "en-US" },
                autoGenerateTemplates = true
            },
            libraries = new
            {
                discoveryPattern = "CL.*",
                enableDependencyResolution = true
            },
            healthChecks = new
            {
                enabled = true,
                intervalSeconds = 30
            }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(configPath, json);
    }

    private static async Task CreateMarkerAsync(string frameworkRoot)
    {
        var marker = new
        {
            initializedAt = DateTime.UtcNow,
            version = "3.0.0",
            machine = Environment.MachineName
        };
        var json = JsonSerializer.Serialize(marker, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(GetMarkerPath(frameworkRoot), json);
    }

    private static string GetMarkerPath(string frameworkRoot) =>
        Path.Combine(frameworkRoot, MarkerFileName);
}

/// <summary>
/// Represents the result of a first-run scaffolding operation.
/// </summary>
public sealed class FirstRunResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int DirectoriesCreated { get; set; }
}
