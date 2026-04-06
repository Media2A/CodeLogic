namespace CodeLogic.Core.Configuration;

public sealed class ConfigValidationResult
{
    public bool IsValid { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; } = [];

    private ConfigValidationResult() { }

    public static ConfigValidationResult Valid() =>
        new() { IsValid = true };

    public static ConfigValidationResult Invalid(IEnumerable<string> errors) =>
        new() { IsValid = false, Errors = errors.ToList() };

    public static ConfigValidationResult Invalid(string error) =>
        new() { IsValid = false, Errors = [error] };

    public override string ToString() =>
        IsValid ? "Valid" : $"Invalid: {string.Join(", ", Errors)}";
}
