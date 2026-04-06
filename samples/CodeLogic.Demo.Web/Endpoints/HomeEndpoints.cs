using CodeLogic;
using CodeLogic.Core.Events;
using CodeLogic.Demo.Web.Config;
using CodeLogic.Demo.Web.Events;
using CodeLogic.Demo.Web.Localization;
using CL = CodeLogic.CodeLogic; // alias to avoid CodeLogic.CodeLogic ambiguity

namespace CodeLogic.Demo.Web.Endpoints;

// ─────────────────────────────────────────────────────────────────────────────
// Home endpoints — demonstrates reading config and localization per request.
// ─────────────────────────────────────────────────────────────────────────────

public static class HomeEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // GET / — returns site info, config values, localized strings
        app.MapGet("/", (IEventBus bus) =>
        {
            // Publish a request event so the application layer can react
            bus.Publish(new RequestReceivedEvent("GET", "/"));

            // Read config + localization from the application context
            var ctx     = CL.GetApplicationContext();
            if (ctx == null) return Results.Problem("CodeLogic application context not available.");

            var config  = ctx.Configuration.Get<WebConfig>();
            var strings = ctx.Localization.Get<WebStrings>();

            return Results.Ok(new
            {
                title       = config.SiteTitle,
                welcome     = string.Format(strings.Welcome, config.SiteTitle),
                language    = config.DefaultLanguage,
                machine     = CodeLogicEnvironment.MachineName,
                appVersion  = CodeLogicEnvironment.AppVersion,
                isDebugging = CodeLogicEnvironment.IsDebugging,
            });
        })
        .WithName("Home")
        .WithDescription("Returns site info, config values, and localized strings.");
    }
}
