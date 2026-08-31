using System.ComponentModel.DataAnnotations;

namespace OptionsPattern.Options;

public sealed class RetryOptions
{
    public const string Section = "Retry";

    [Range(1, 5)]
    public int MaxAttempts { get; set; } = 3;

    [Range(100, 10_000)]
    public int BaseDelayMs { get; set; } = 500;
}
