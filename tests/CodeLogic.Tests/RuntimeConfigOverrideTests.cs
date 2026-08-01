using System.ComponentModel.DataAnnotations;
using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Framework.Libraries;
using CodeLogic;
using Xunit;

namespace CodeLogic.Tests;

public sealed class RuntimeConfigOverrideTests
{
    [Fact]
    public async Task Override_makes_incomplete_json_valid_is_visible_on_initialize_and_is_not_persisted()
    {
        await using var fixture = new LibraryFixture("{}");
        var host = StartupParameterStore.From([], new Dictionary<string, string>
        {
            ["MYSQL_HOST"] = "db.internal"
        }).GetRequired<string>("mysql-host");
        fixture.Manager.OverrideConfig<FakeConfig>("CL.Fake", "main", config => config.Host = host);
        await fixture.Manager.LoadLibraryAsync<FakeLibrary>();

        await fixture.Manager.ConfigureAllAsync();
        await fixture.Manager.InitializeAllAsync();

        var library = Assert.IsType<FakeLibrary>(fixture.Manager.GetLibrary("CL.Fake"));
        Assert.Equal("db.internal", library.HostSeenDuringInitialize);
        Assert.Equal("{}", await File.ReadAllTextAsync(fixture.ConfigPath));
    }

    [Fact]
    public async Task Overrides_are_ordered_and_an_ignored_failed_override_is_atomic()
    {
        await using var fixture = new LibraryFixture("{\"host\":\"json\"}");
        fixture.Manager.OverrideConfig<FakeConfig>("CL.Fake", "main", config => config.Host += "-first");
        fixture.Manager.OverrideConfig<FakeConfig>("CL.Fake", "main", config =>
        {
            config.Host = "must-not-leak";
            throw new InvalidOperationException("expected test failure");
        }, new ConfigOverrideOptions { FailureMode = ConfigOverrideFailureMode.Ignore });
        fixture.Manager.OverrideConfig<FakeConfig>("CL.Fake", "main", config => config.Host += "-last");
        await fixture.Manager.LoadLibraryAsync<FakeLibrary>();

        await fixture.Manager.ConfigureAllAsync();
        await fixture.Manager.InitializeAllAsync();

        var library = Assert.IsType<FakeLibrary>(fixture.Manager.GetLibrary("CL.Fake"));
        Assert.Equal("json-first-last", library.HostSeenDuringInitialize);
        Assert.Equal("{\"host\":\"json\"}", await File.ReadAllTextAsync(fixture.ConfigPath));
    }

    [Fact]
    public async Task Strict_unknown_library_unknown_section_and_type_mismatch_fail_configuration()
    {
        var root = Path.Combine(Path.GetTempPath(), $"CodeLogic.Tests.{Guid.NewGuid():N}");
        try
        {
            using var missing = new LibraryManager(new EventBus()) { FrameworkRootPath = root };
            missing.OverrideConfig<FakeConfig>("CL.Missing", "main", _ => { });
            await Assert.ThrowsAsync<InvalidOperationException>(() => missing.ConfigureAllAsync());

            await using var mismatch = new LibraryFixture("{\"host\":\"json\"}");
            mismatch.Manager.OverrideConfig<OtherConfig>("CL.Fake", "main", _ => { });
            await mismatch.Manager.LoadLibraryAsync<FakeLibrary>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => mismatch.Manager.ConfigureAllAsync());

            await using var section = new LibraryFixture("{\"host\":\"json\"}");
            section.Manager.OverrideConfig<FakeConfig>("CL.Fake", "missing", _ => { });
            await section.Manager.LoadLibraryAsync<FakeLibrary>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => section.Manager.ConfigureAllAsync());
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private sealed class LibraryFixture : IAsyncDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), $"CodeLogic.Tests.{Guid.NewGuid():N}");

        public LibraryFixture(string configJson)
        {
            Manager = new LibraryManager(new EventBus()) { FrameworkRootPath = _root };
            var directory = Path.Combine(_root, "Libraries", "CL.Fake");
            Directory.CreateDirectory(directory);
            ConfigPath = Path.Combine(directory, "config.main.json");
            File.WriteAllText(ConfigPath, configJson);
        }

        public LibraryManager Manager { get; }
        public string ConfigPath { get; }

        public ValueTask DisposeAsync()
        {
            Manager.Dispose();
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeConfig : ConfigModelBase
    {
        [Required]
        public string Host { get; set; } = string.Empty;
    }

    private sealed class OtherConfig : ConfigModelBase { }

    private sealed class FakeLibrary : ILibrary
    {
        public LibraryManifest Manifest { get; } = new()
        {
            Id = "CL.Fake",
            Name = "Fake",
            Version = "1.0.0"
        };

        public string? HostSeenDuringInitialize { get; private set; }

        public Task OnConfigureAsync(LibraryContext context)
        {
            context.Configuration.Register<FakeConfig>("main");
            return Task.CompletedTask;
        }

        public Task OnInitializeAsync(LibraryContext context)
        {
            HostSeenDuringInitialize = context.Configuration.Get<FakeConfig>().Host;
            return Task.CompletedTask;
        }

        public Task OnStartAsync(LibraryContext context) => Task.CompletedTask;
        public Task OnStopAsync() => Task.CompletedTask;
        public Task<HealthStatus> HealthCheckAsync() => Task.FromResult(HealthStatus.Healthy());
        public void Dispose() { }
    }
}
