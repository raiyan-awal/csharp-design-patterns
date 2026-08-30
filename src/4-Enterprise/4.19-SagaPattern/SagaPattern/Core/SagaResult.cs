namespace SagaPattern.Core;

public sealed class SagaResult
{
    public bool       IsSuccess  { get; private init; }
    public string?    FailedStep { get; private init; }
    public Exception? Error      { get; private init; }

    private SagaResult() { }

    public static SagaResult Success()
        => new() { IsSuccess = true };

    public static SagaResult Failure(string failedStep, Exception error)
        => new() { IsSuccess = false, FailedStep = failedStep, Error = error };
}
