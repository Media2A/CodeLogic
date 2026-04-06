using CL = CodeLogic.CodeLogic;
using CodeLogic.Demo.Web.Plugins;

namespace CodeLogic.Demo.Web.Endpoints;

// ─────────────────────────────────────────────────────────────────────────────
// Plugin endpoints — exposes data collected by the demo plugins.
//
//   GET /plugins               → list all loaded plugins and their state
//   GET /plugins/request-stats → stats from RequestLoggerPlugin
//   GET /plugins/notifications → recent notifications from NotificationPlugin
// ─────────────────────────────────────────────────────────────────────────────

public static class PluginEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // GET /plugins — list all loaded plugins
        app.MapGet("/plugins", () =>
        {
            var pm = CL.GetPluginManager();
            if (pm == null) return Results.Ok(new { message = "No plugin manager registered." });

            var plugins = pm.GetLoadedPlugins().Select(p => new
            {
                id          = p.Manifest.Id,
                name        = p.Manifest.Name,
                version     = p.Manifest.Version,
                description = p.Manifest.Description,
                state       = p.State.ToString(),
                loadedAt    = p.LoadedAt
            });

            return Results.Ok(new { count = pm.GetLoadedPlugins().Count(), plugins });
        })
        .WithName("ListPlugins");

        // GET /plugins/request-stats — data from RequestLoggerPlugin
        app.MapGet("/plugins/request-stats", () =>
        {
            var pm = CL.GetPluginManager();
            if (pm == null) return Results.Problem("No plugin manager registered.");

            var plugin = pm.GetPlugin<RequestLoggerPlugin>("demo.request-logger");
            if (plugin == null) return Results.NotFound(new { error = "RequestLoggerPlugin not loaded." });

            var (total, byMethod, byPath) = plugin.GetStats();
            var topPaths = byPath
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            return Results.Ok(new
            {
                total,
                byMethod,
                topPaths
            });
        })
        .WithName("RequestStats");

        // GET /plugins/notifications — rolling log from NotificationPlugin
        app.MapGet("/plugins/notifications", () =>
        {
            var pm = CL.GetPluginManager();
            if (pm == null) return Results.Problem("No plugin manager registered.");

            var plugin = pm.GetPlugin<NotificationPlugin>("demo.notifications");
            if (plugin == null) return Results.NotFound(new { error = "NotificationPlugin not loaded." });

            var log = plugin.GetLog();
            return Results.Ok(new
            {
                count = log.Count,
                notifications = log.Select(n => new
                {
                    timestamp = n.Timestamp,
                    severity  = n.Severity,
                    title     = n.Title,
                    message   = n.Message
                })
            });
        })
        .WithName("Notifications");
    }
}
