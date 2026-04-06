using System.ComponentModel.DataAnnotations;
using CodeLogic.Core.Configuration;

namespace CodeLogic.Demo.Console.Config;

// ─────────────────────────────────────────────────────────────────────────────
// Application configuration model.
//
// Pattern:
//   1. Inherit from ConfigModelBase
//   2. Add DataAnnotations for validation ([Required], [Range], etc.)
//   3. Set sensible defaults on all properties
//   4. Register in DemoApplication.OnConfigureAsync:
//        context.Configuration.Register<DemoConfig>();
//   5. Access after startup:
//        var config = context.Configuration.Get<DemoConfig>();
//
// The framework generates data/app/config.json on first run.
// Edit that file to change values — no code changes needed.
// ─────────────────────────────────────────────────────────────────────────────

public class DemoConfig : ConfigModelBase
{
    /// <summary>Display name shown in the console header.</summary>
    [Required]
    public string AppTitle { get; set; } = "CodeLogic Console Demo";

    /// <summary>Maximum number of items to process per batch.</summary>
    [Range(1, 10_000)]
    public int MaxItemsPerBatch { get; set; } = 100;

    /// <summary>How long to wait between simulated work cycles (ms).</summary>
    [Range(0, 60_000)]
    public int WorkIntervalMs { get; set; } = 500;

    /// <summary>Enable verbose output for debugging.</summary>
    public bool VerboseOutput { get; set; } = false;
}
