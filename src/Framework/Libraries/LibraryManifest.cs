using System.Reflection;

namespace CodeLogic.Framework.Libraries;

public sealed class LibraryManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public LibraryDependency[] Dependencies { get; init; } = [];
    public string[] Tags { get; init; } = [];
}

public sealed record LibraryDependency
{
    public required string Id { get; init; }
    public string? MinVersion { get; init; }
    public bool IsOptional { get; init; } = false;

    public static LibraryDependency Required(string id) => new() { Id = id };
    public static LibraryDependency Required(string id, string minVersion) =>
        new() { Id = id, MinVersion = minVersion };
    public static LibraryDependency Optional(string id) => new() { Id = id, IsOptional = true };
    public static LibraryDependency Optional(string id, string minVersion) =>
        new() { Id = id, MinVersion = minVersion, IsOptional = true };
}

/// <summary>Applied to library classes to declare dependencies on other libraries.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class LibraryDependencyAttribute : Attribute
{
    public required string Id { get; set; }
    public string? MinVersion { get; set; }
    public bool IsOptional { get; set; } = false;

    public LibraryDependency ToDependency() => new()
    {
        Id = Id, MinVersion = MinVersion, IsOptional = IsOptional
    };
}
