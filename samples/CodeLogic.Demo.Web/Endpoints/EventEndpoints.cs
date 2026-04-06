using CodeLogic.Core.Events;
using CodeLogic.Demo.Web.Events;

namespace CodeLogic.Demo.Web.Endpoints;

// ─────────────────────────────────────────────────────────────────────────────
// Event endpoints — demonstrates the event bus from HTTP requests.
//
// Shows how endpoints can publish events that the application layer
// reacts to without tight coupling between endpoint and handler code.
// ─────────────────────────────────────────────────────────────────────────────

public static class EventEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // POST /events/notify — publish an app notification event
        // Body: { "title": "...", "message": "...", "severity": "info|warn|error" }
        app.MapPost("/events/notify", (NotifyRequest req, IEventBus bus) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Message))
                return Results.BadRequest(new { error = "Title and message are required." });

            bus.Publish(new AppNotificationEvent(
                req.Title,
                req.Message,
                req.Severity ?? "info"));

            return Results.Ok(new
            {
                published = true,
                eventType = nameof(AppNotificationEvent),
                title     = req.Title
            });
        })
        .WithName("PublishNotification")
        .WithDescription("Publishes an AppNotificationEvent to the shared event bus.");

        // POST /events/request — simulate a request event (for demo/testing)
        app.MapPost("/events/request", (RequestSimulation req, IEventBus bus) =>
        {
            bus.Publish(new RequestReceivedEvent(
                req.Method ?? "GET",
                req.Path   ?? "/simulated",
                req.UserId));

            return Results.Ok(new { published = true, eventType = nameof(RequestReceivedEvent) });
        })
        .WithName("SimulateRequest")
        .WithDescription("Simulates a request event via the event bus.");
    }
}

// ── Request models ────────────────────────────────────────────────────────────

record NotifyRequest(string Title, string Message, string? Severity);
record RequestSimulation(string? Method, string? Path, string? UserId);
