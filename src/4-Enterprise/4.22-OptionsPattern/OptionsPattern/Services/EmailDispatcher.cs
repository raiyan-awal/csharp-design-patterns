using Microsoft.Extensions.Options;
using OptionsPattern.Options;

namespace OptionsPattern.Services;

public sealed class EmailDispatcher(IOptions<SmtpOptions> smtpOptions, IOptions<RetryOptions> retryOptions)
{
    public SmtpOptions  SmtpConfig  { get; } = smtpOptions.Value;
    public RetryOptions RetryConfig { get; } = retryOptions.Value;

    public void Send(string to, string subject, string body)
    {
        Console.WriteLine($"  Sending via {SmtpConfig.Host}:{SmtpConfig.Port}");
        Console.WriteLine($"  From    : {SmtpConfig.FromDisplayName} <{SmtpConfig.FromAddress}>");
        Console.WriteLine($"  To      : {to}");
        Console.WriteLine($"  Subject : {subject}");
        Console.WriteLine($"  Timeout : {SmtpConfig.TimeoutSeconds}s  |  " +
                          $"Retry: up to {RetryConfig.MaxAttempts} attempts (base {RetryConfig.BaseDelayMs}ms delay)");
    }
}
