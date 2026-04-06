using CodeLogic.Core.Localization;

namespace CodeLogic.Demo.Web.Localization;

// ─────────────────────────────────────────────────────────────────────────────
// Localization strings for the web demo.
//
// Generated to:
//   data/app/localization/web.en-US.json  ← auto-generated, do not edit
//   data/app/localization/web.da-DK.json  ← translate this file
//
// Usage in an endpoint:
//   var ctx = CodeLogic.GetApplicationContext()!;
//   var s = ctx.Localization.Get<WebStrings>(requestedCulture);
//   return Results.Ok(new { message = s.Welcome });
// ─────────────────────────────────────────────────────────────────────────────

[LocalizationSection("web")]
public class WebStrings : LocalizationModelBase
{
    // ── API responses ─────────────────────────────────────────────────────
    public string Welcome       { get; set; } = "Welcome to {0}!";
    public string NotFound      { get; set; } = "The requested resource was not found.";
    public string ServerError   { get; set; } = "An internal error occurred. Please try again.";
    public string Unauthorized  { get; set; } = "Authentication is required.";

    // ── Health ────────────────────────────────────────────────────────────
    public string Healthy       { get; set; } = "All systems operational";
    public string Degraded      { get; set; } = "Degraded performance detected";
    public string Unhealthy     { get; set; } = "Service unavailable";

    // ── Events ────────────────────────────────────────────────────────────
    public string EventPublished { get; set; } = "Event published successfully";
    public string EventLog       { get; set; } = "Showing last {0} events";
}
