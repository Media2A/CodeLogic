namespace CodeLogic.Core.Configuration;

/// <summary>Represents the result of validating a configuration model.</summary>
public sealed class ConfigValidationResult
{
    /// <summary>Gets whether the configuration passed validation.</summary>
    public bool IsValid { get; private set; }

    /// <summary>Gets the list of validation error messages.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = [];

    private ConfigValidationResult() { }

    /// <summary>Creates a successful validation result.</summary>
    public static ConfigValidationResult Valid() =>
        new() { IsValid = true };

    /// <summary>Creates a failed validation result with the specified errors.</summary>
    public static ConfigValidationResult Invalid(IEnumerable<string> errors) =>
        new() { IsValid = false, Errors = errors.ToList() };

    /// <summary>Creates a failed validation result with a single error.</summary>
    public static ConfigValidationResult Invalid(string error) =>
        new() { IsValid = false, Errors = [error] };

    /// <inheritdoc />
    public override string ToString() =>
        IsValid ? "Valid" : $"Invalid: {string.Join(", ", Errors)}";
}
