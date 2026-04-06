namespace CodeLogic.Core.Results;

public sealed class Error
{
    public string Code { get; }       // e.g. "user.not_found", "db.connection_failed"
    public string Message { get; }    // human-readable description
    public string? Details { get; }   // optional extra context
    public Error? InnerError { get; } // error chaining

    private Error(string code, string message, string? details = null, Error? innerError = null)
    {
        Code = code;
        Message = message;
        Details = details;
        InnerError = innerError;
    }

    // Factory methods
    public static Error NotFound(string code, string message, string? details = null)
        => new(code, message, details);

    public static Error Validation(string code, string message, string? details = null)
        => new(code, message, details);

    public static Error Internal(string code, string message, string? details = null, Error? innerError = null)
        => new(code, message, details, innerError);

    public static Error Unauthorized(string code, string message, string? details = null)
        => new(code, message, details);

    public static Error Conflict(string code, string message, string? details = null)
        => new(code, message, details);

    public static Error Timeout(string code, string message, string? details = null)
        => new(code, message, details);

    public static Error Unavailable(string code, string message, string? details = null)
        => new(code, message, details);

    // Wrap an exception as an internal error
    public static Error FromException(Exception ex, string code = "internal.exception")
        => new(code, ex.Message, ex.GetType().Name, null);

    // Chain errors
    public Error WithInner(Error inner) => new(Code, Message, Details, inner);
    public Error WithDetails(string details) => new(Code, Message, details, InnerError);

    public override string ToString() =>
        InnerError is null
            ? $"[{Code}] {Message}{(Details is null ? "" : $" ({Details})")}"
            : $"[{Code}] {Message}{(Details is null ? "" : $" ({Details})")} \u2192 {InnerError}";
}
