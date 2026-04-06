namespace CodeLogic.Core.Logging;

public interface ILogger
{
    void Trace(string message);
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
    void Critical(string message, Exception? exception = null);

    // Convenience overloads with structured data
    void Debug(string message, params object?[] args) => Debug(string.Format(message, args));
    void Info(string message, params object?[] args) => Info(string.Format(message, args));
    void Warning(string message, params object?[] args) => Warning(string.Format(message, args));
    void Error(string message, params object?[] args) => Error(string.Format(message, args));
}
