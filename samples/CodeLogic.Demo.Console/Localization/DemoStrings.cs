using CodeLogic.Core.Localization;

namespace CodeLogic.Demo.Console.Localization;

// ─────────────────────────────────────────────────────────────────────────────
// Application localization strings.
//
// Pattern:
//   1. Inherit from LocalizationModelBase
//   2. Apply [LocalizationSection("key")] to control the file name
//   3. Set English defaults on all string properties
//   4. Register in DemoApplication.OnConfigureAsync:
//        context.Localization.Register<DemoStrings>();
//   5. Access after startup:
//        var s = context.Localization.Get<DemoStrings>();
//        var s = context.Localization.Get<DemoStrings>("da-DK");
//
// The framework generates:
//   data/app/localization/demo.en-US.json  ← English (auto, never edit)
//   data/app/localization/demo.da-DK.json  ← Danish  (translate this)
//
// Non-default cultures fall back to English for any missing key.
// ─────────────────────────────────────────────────────────────────────────────

[LocalizationSection("demo")]
public class DemoStrings : LocalizationModelBase
{
    // ── Startup / shutdown ────────────────────────────────────────────────
    public string Welcome     { get; set; } = "Welcome to {0}!";
    public string Goodbye     { get; set; } = "Goodbye!";
    public string Starting    { get; set; } = "Starting up...";
    public string Stopping    { get; set; } = "Shutting down...";

    // ── Status messages ───────────────────────────────────────────────────
    public string Ready       { get; set; } = "Ready. Press Q to quit.";
    public string Processing  { get; set; } = "Processing {0} items...";
    public string Completed   { get; set; } = "Completed in {0}ms";
    public string Failed      { get; set; } = "Failed: {0}";

    // ── Health ────────────────────────────────────────────────────────────
    public string Healthy     { get; set; } = "All systems healthy";
    public string Unhealthy   { get; set; } = "Degraded: {0}";
}
