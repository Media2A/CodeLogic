namespace CodeLogic.Framework.Libraries;

/// <summary>Controls how one runtime configuration override handles a failure.</summary>
public enum ConfigOverrideFailureMode
{
    /// <summary>Abort configuration when the override cannot be applied.</summary>
    Strict,

    /// <summary>Log the failure and continue with the unmodified in-memory configuration.</summary>
    Ignore
}

/// <summary>Options for a runtime-only library configuration override.</summary>
public sealed class ConfigOverrideOptions
{
    /// <summary>How to handle an unknown target, type mismatch, callback error, or invalid result.</summary>
    public ConfigOverrideFailureMode FailureMode { get; init; } = ConfigOverrideFailureMode.Strict;
}
