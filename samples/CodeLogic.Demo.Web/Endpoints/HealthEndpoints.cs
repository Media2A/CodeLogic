using CL = CodeLogic.CodeLogic; // alias avoids CodeLogic.CodeLogic ambiguity

namespace CodeLogic.Demo.Web.Endpoints;

// ─────────────────────────────────────────────────────────────────────────────
// Health endpoints — aggregated health report from all libraries and the app.
//
// Returns HTTP 200 when all components are healthy.
// Returns HTTP 503 when any component is degraded or unhealthy.
// ─────────────────────────────────────────────────────────────────────────────

public static class HealthEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // GET /health — JSON health report
        app.MapGet("/health", async () =>
        {
            var report = await CL.GetHealthAsync();
            var json   = report.ToJson();
            var status = report.IsHealthy ? 200 : 503;

            return Results.Content(json, "application/json", statusCode: status);
        })
        .WithName("Health")
        .WithDescription("Returns aggregated health status for all components.");

        // GET /health/text — plain text summary (useful for monitoring tools)
        app.MapGet("/health/text", async () =>
        {
            var report = await CL.GetHealthAsync();
            return Results.Text(report.ToConsoleString());
        })
        .WithName("HealthText")
        .WithDescription("Returns a plain-text health summary.");
    }
}
