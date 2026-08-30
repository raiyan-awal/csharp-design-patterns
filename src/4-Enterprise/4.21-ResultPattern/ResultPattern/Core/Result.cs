namespace ResultPattern.Core;

public sealed class Result<T>
{
    public bool   IsSuccess { get; }
    public T?     Value     { get; }
    public string Error     { get; }

    private Result(T value)      { IsSuccess = true;  Value = value; Error = "";    }
    private Result(string error) { IsSuccess = false; Value = default; Error = error; }

    public static Result<T> Success(T value)     => new(value);
    public static Result<T> Failure(string error) => new(error);

    // Transform the success value; propagate failure unchanged.
    public Result<TNext> Map<TNext>(Func<T, TNext> map)
        => IsSuccess ? Result<TNext>.Success(map(Value!)) : Result<TNext>.Failure(Error);

    // Chain a fallible operation; short-circuit on the first failure.
    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> bind)
        => IsSuccess ? bind(Value!) : Result<TNext>.Failure(Error);

    // Collapse both branches into a single value — use this at the call site to handle the result.
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error);

    // Side-effect callbacks — useful for logging; return this so calls can be chained.
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess) action(Value!);
        return this;
    }

    public Result<T> OnFailure(Action<string> action)
    {
        if (!IsSuccess) action(Error);
        return this;
    }

    public override string ToString()
        => IsSuccess ? $"Success({Value})" : $"Failure({Error})";
}
