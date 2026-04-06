namespace CodeLogic;

public sealed class InitializationResult
{
    public bool Success { get; init; }
    public bool IsFirstRun { get; init; }
    public bool ShouldExit { get; init; }
    public string Message { get; init; } = string.Empty;

    public static InitializationResult Succeeded(bool isFirstRun = false) => new()
    {
        Success = true, IsFirstRun = isFirstRun, Message = "Framework initialized successfully"
    };

    public static InitializationResult Failed(string message) => new()
    {
        Success = false, ShouldExit = true, Message = message
    };

    public static InitializationResult Exit(string message) => new()
    {
        Success = true, ShouldExit = true, Message = message
    };
}
