namespace CodeLogic.Core.Results;

/// <summary>
/// Well-known error code constants. Format: "category.specific_issue"
/// Libraries may define their own codes following the same convention.
/// </summary>
public static class ErrorCode
{
    // General
    public const string NotFound        = "not_found";
    public const string AlreadyExists   = "already_exists";
    public const string InvalidArgument = "invalid_argument";
    public const string InvalidState    = "invalid_state";
    public const string NotSupported    = "not_supported";
    public const string Cancelled       = "cancelled";
    public const string Timeout         = "timeout";
    public const string Internal        = "internal";
    public const string Unauthorized    = "unauthorized";
    public const string Forbidden       = "forbidden";

    // Validation
    public const string ValidationFailed  = "validation.failed";
    public const string RequiredMissing   = "validation.required_missing";
    public const string InvalidFormat     = "validation.invalid_format";
    public const string OutOfRange        = "validation.out_of_range";

    // Configuration
    public const string ConfigNotFound    = "config.not_found";
    public const string ConfigInvalid     = "config.invalid";
    public const string ConfigLoadFailed  = "config.load_failed";

    // IO
    public const string FileNotFound      = "io.file_not_found";
    public const string FileReadFailed    = "io.read_failed";
    public const string FileWriteFailed   = "io.write_failed";

    // Network / Connection
    public const string ConnectionFailed  = "connection.failed";
    public const string ConnectionLost    = "connection.lost";
    public const string ConnectionTimeout = "connection.timeout";

    // Framework
    public const string NotInitialized   = "framework.not_initialized";
    public const string AlreadyStarted   = "framework.already_started";
    public const string LibraryNotFound  = "framework.library_not_found";
    public const string PluginNotFound   = "framework.plugin_not_found";
}
