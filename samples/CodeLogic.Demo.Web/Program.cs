using CL.Common;
using CL.GitHelper;
using CL.MySQL2;
using CL.NetUtils;
using CL.StorageS3;
using CL.WebLogic;
using CodeLogic;
using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Demo.Web.Application;
using CodeLogic.Demo.Web.Plugins;
using CodeLogic.Framework.Application.Plugins;

var clResult = await CodeLogic.CodeLogic.InitializeAsync(opts =>
{
    opts.FrameworkRootPath = "data/codelogic";
    opts.ApplicationRootPath = "data/app";
    opts.AppVersion = "1.0.0";
    opts.HandleShutdownSignals = false;
});

if (!clResult.Success || clResult.ShouldExit)
{
    Console.Error.WriteLine($"CodeLogic init failed: {clResult.Message}");
    return;
}

await Libraries.LoadAsync<CommonLibrary>();
await Libraries.LoadAsync<GitHelperLibrary>();
await Libraries.LoadAsync<StorageS3Library>();
await Libraries.LoadAsync<MySQL2Library>();
await Libraries.LoadAsync<NetUtilsLibrary>();
await Libraries.LoadAsync<WebLogicLibrary>();

CodeLogic.CodeLogic.RegisterApplication(new WebDemoApplication());

await CodeLogic.CodeLogic.ConfigureAsync();
await CodeLogic.CodeLogic.StartAsync();

var pluginMgr = new PluginManager(
    CodeLogic.CodeLogic.GetEventBus(),
    new PluginOptions
    {
        PluginsDirectory = "data/plugins",
        EnableHotReload = false
    });

await LoadInProcessPluginAsync(pluginMgr, new RequestLoggerPlugin());
await LoadInProcessPluginAsync(pluginMgr, new NotificationPlugin());

CodeLogic.CodeLogic.SetPluginManager(pluginMgr);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IEventBus>(CodeLogic.CodeLogic.GetEventBus());
builder.Services.AddCodeLogicWebLogic();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<ICodeLogicRuntime>(_ =>
    throw new InvalidOperationException(
        "Use CodeLogic.GetApplicationContext() directly for now, " +
        "or wire ICodeLogicRuntime from your own CodeLogicRuntime instance."));

var app = builder.Build();
app.UseSession();
app.UseCodeLogicWebLogic();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
    CodeLogic.CodeLogic.StopAsync().GetAwaiter().GetResult());

app.Run();

static async Task LoadInProcessPluginAsync(PluginManager manager, IPlugin plugin)
{
    var ctx = CodeLogic.CodeLogic.GetApplicationContext()
        ?? throw new InvalidOperationException("Application context not available.");

    var pluginDir = Path.Combine("data/plugins", plugin.Manifest.Id);
    Directory.CreateDirectory(Path.Combine(pluginDir, "logs"));

    var pluginCtx = new PluginContext
    {
        PluginId = plugin.Manifest.Id,
        PluginDirectory = pluginDir,
        ConfigDirectory = pluginDir,
        LocalizationDirectory = Path.Combine(pluginDir, "localization"),
        LogsDirectory = Path.Combine(pluginDir, "logs"),
        DataDirectory = Path.Combine(pluginDir, "data"),
        Logger = ctx.Logger,
        Configuration = new ConfigurationManager(pluginDir),
        Localization = ctx.Localization,
        Events = ctx.Events
    };

    await plugin.OnConfigureAsync(pluginCtx);
    await pluginCtx.Configuration.GenerateAllDefaultsAsync();
    await pluginCtx.Configuration.LoadAllAsync();
    await plugin.OnInitializeAsync(pluginCtx);
    await plugin.OnStartAsync(pluginCtx);
    await manager.RegisterInProcessPluginAsync(plugin, pluginCtx);
}
