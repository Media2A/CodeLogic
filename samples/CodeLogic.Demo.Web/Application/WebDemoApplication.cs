using CL.WebLogic;
using CL.WebLogic.Runtime;
using CodeLogic;
using CodeLogic.Demo.Web.Config;
using CodeLogic.Demo.Web.Events;
using CodeLogic.Demo.Web.Localization;
using CodeLogic.Framework.Application;

namespace CodeLogic.Demo.Web.Application;

public class WebDemoApplication : IApplication
{
    public ApplicationManifest Manifest { get; } = new()
    {
        Id = "demo.web",
        Name = "CodeLogic Web Demo",
        Version = "1.0.0",
        Description = "Reference implementation showing CodeLogic + CL.WebLogic",
        Author = "CodeLogic"
    };

    private ApplicationContext? _context;
    private WebConfig _config = new();

    public Task OnConfigureAsync(ApplicationContext context)
    {
        context.Configuration.Register<WebConfig>();
        context.Localization.Register<WebStrings>();
        context.Logger.Info($"{Manifest.Name} configured");
        return Task.CompletedTask;
    }

    public Task OnInitializeAsync(ApplicationContext context)
    {
        _context = context;
        _config = context.Configuration.Get<WebConfig>();
        context.Logger.Info($"{Manifest.Name} initialized for site '{_config.SiteTitle}'");
        return Task.CompletedTask;
    }

    public Task OnStartAsync(ApplicationContext context)
    {
        var web = Libraries.Get<WebLogicLibrary>()
            ?? throw new InvalidOperationException("CL.WebLogic is required for the demo web app.");

        context.Events.Subscribe<WebRequestHandledEvent>(e =>
            context.Logger.Trace($"[{e.StatusCode}] {e.Method} {e.Path} in {e.DurationMs}ms"));

        context.Events.Subscribe<AppNotificationEvent>(e =>
        {
            var msg = $"Notification [{e.Severity}] {e.Title}: {e.Message}";
            switch (e.Severity.ToLowerInvariant())
            {
                case "error":
                    context.Logger.Error(msg);
                    break;
                case "warn":
                case "warning":
                    context.Logger.Warning(msg);
                    break;
                default:
                    context.Logger.Info(msg);
                    break;
            }
        });

        web.RegisterPage("/", _ => Task.FromResult(WebResult.Template(
            "templates/home.html",
            new Dictionary<string, object?>
            {
                ["title"] = _config.SiteTitle,
                ["subtitle"] = "CL.WebLogic is owning the request pipeline now.",
                ["description"] = "The page is rendered from the theme folder, while the app and plugins register routes through CodeLogic3."
            })), "GET");

        web.RegisterApi("/api/health", _ => Task.FromResult(WebResult.Json(new
        {
            ok = true,
            site = _config.SiteTitle,
            runtime = "CL.WebLogic"
        })), "GET");

        web.RegisterApi("/api/events/trigger", async request =>
        {
            var form = await request.ReadFormAsync().ConfigureAwait(false);
            var severity = form.GetValueOrDefault("severity", "info");
            var title = form.GetValueOrDefault("title", "Manual event");
            var message = form.GetValueOrDefault("message", "Triggered through CL.WebLogic");

            context.Events.Publish(new AppNotificationEvent(title, message, severity));
            return WebResult.Json(new
            {
                accepted = true,
                severity,
                title,
                message
            });
        }, "POST");

        web.RegisterFallback(_ => Task.FromResult(WebResult.Template(
            "templates/not-found.html",
            new Dictionary<string, object?>
            {
                ["title"] = _config.SiteTitle,
                ["path"] = "The requested route was not registered."
            },
            statusCode: 404)), "GET", "POST");

        context.Events.Publish(new AppNotificationEvent(
            "Startup",
            $"{Manifest.Name} is ready to handle requests through CL.WebLogic",
            "info"));

        context.Logger.Info($"{Manifest.Name} started");
        return Task.CompletedTask;
    }

    public Task OnStopAsync()
    {
        _context?.Logger.Info($"{Manifest.Name} stopping");
        return Task.CompletedTask;
    }
}
