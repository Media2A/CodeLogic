namespace CodeLogic.Core.Utilities;

/// <summary>
/// The parsed result of command-line argument processing.
/// </summary>
public sealed class ParsedCliArgs
{
    public bool GenerateConfigs { get; init; }
    public bool GenerateConfigsForce { get; init; }
    public string[]? GenerateConfigsFor { get; init; }  // null = all
    public bool ExitAfterGenerate { get; init; }
    public bool DryRun { get; init; }
    public bool ShowVersion { get; init; }
    public bool ShowInfo { get; init; }
    public bool ShowHealth { get; init; }
}

/// <summary>
/// Parses command-line arguments into a <see cref="ParsedCliArgs"/> instance.
/// </summary>
public static class CliArgParser
{
    /// <summary>
    /// Parses Environment.GetCommandLineArgs() (skips argv[0] which is the exe path).
    /// </summary>
    public static ParsedCliArgs Parse()
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray(); // skip exe path
        return Parse(args);
    }

    /// <summary>Parses an explicit args array (useful for testing).</summary>
    public static ParsedCliArgs Parse(string[] args)
    {
        bool generateConfigs = false;
        bool generateConfigsForce = false;
        bool dryRun = false;
        bool showVersion = false;
        bool showInfo = false;
        bool showHealth = false;
        var scopedLibs = new List<string>();
        bool collectingLibs = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();

            if (arg == "--generate-configs-force")
            {
                generateConfigs = true;
                generateConfigsForce = true;
                collectingLibs = true;
                continue;
            }
            if (arg == "--generate-configs")
            {
                generateConfigs = true;
                collectingLibs = true;
                continue;
            }
            if (arg == "--dry-run")  { dryRun = true;       collectingLibs = false; continue; }
            if (arg == "--version")  { showVersion = true;   collectingLibs = false; continue; }
            if (arg == "--info")     { showInfo = true;       collectingLibs = false; continue; }
            if (arg == "--health")   { showHealth = true;     collectingLibs = false; continue; }

            if (arg.StartsWith("--"))
            {
                collectingLibs = false;
                continue;
            }

            // Non-flag arg while collecting lib IDs
            if (collectingLibs)
                scopedLibs.Add(args[i]); // preserve original casing for lib IDs
        }

        return new ParsedCliArgs
        {
            GenerateConfigs      = generateConfigs,
            GenerateConfigsForce = generateConfigsForce,
            GenerateConfigsFor   = scopedLibs.Count > 0 ? scopedLibs.ToArray() : null,
            DryRun               = dryRun,
            ShowVersion          = showVersion,
            ShowInfo             = showInfo,
            ShowHealth           = showHealth
        };
    }
}
