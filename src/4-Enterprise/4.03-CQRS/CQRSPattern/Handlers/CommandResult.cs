namespace CQRSPattern;

public sealed record CommandResult(bool IsSuccess, string? Error = null)
{
    public static CommandResult Ok()               => new(true);
    public static CommandResult Fail(string error) => new(false, error);
}
