namespace CodeLogic.Core.Results;

public readonly struct Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    // Implicit conversions
    public static implicit operator Result(Error error) => Failure(error);

    // Execute action only on success
    public Result OnSuccess(Action action)
    {
        if (IsSuccess) action();
        return this;
    }

    // Execute action only on failure
    public Result OnFailure(Action<Error> action)
    {
        if (IsFailure) action(Error!);
        return this;
    }

    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure({Error})";
}
