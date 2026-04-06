namespace CodeLogic.Core.Results;

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);

    // Implicit conversions — makes return statements cleaner
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    // Map value if success
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess ? Result<TOut>.Success(mapper(Value!)) : Result<TOut>.Failure(Error!);

    // Map to non-generic result
    public Result ToResult() =>
        IsSuccess ? Result.Success() : Result.Failure(Error!);

    // Execute action only on success
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess) action(Value!);
        return this;
    }

    // Execute action only on failure
    public Result<T> OnFailure(Action<Error> action)
    {
        if (IsFailure) action(Error!);
        return this;
    }

    // Unwrap with fallback
    public T ValueOrDefault(T defaultValue) => IsSuccess ? Value! : defaultValue;
    public T ValueOrThrow() =>
        IsSuccess ? Value! : throw new InvalidOperationException($"Result is failure: {Error}");

    public override string ToString() =>
        IsSuccess ? $"Success({Value})" : $"Failure({Error})";
}
