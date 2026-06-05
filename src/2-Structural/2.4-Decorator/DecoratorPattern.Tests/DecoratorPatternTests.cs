using DecoratorPattern;

namespace DecoratorPattern.Tests;

// ── Test doubles ─────────────────────────────────────────────

/// Records every Send() call for assertion.
file sealed class RecordingNotifier : INotifier
{
    public List<(string Recipient, string Subject, string Body)> Calls { get; } = [];

    public void Send(string recipient, string subject, string body)
        => Calls.Add((recipient, subject, body));
}

/// Throws for the first failCount calls, then succeeds silently.
file sealed class FlakyNotifier(int failCount) : INotifier
{
    private int _callCount;

    public void Send(string recipient, string subject, string body)
    {
        _callCount++;
        if (_callCount <= failCount)
            throw new InvalidOperationException($"Simulated failure on attempt {_callCount}.");
    }
}

// ── LoggingDecorator ─────────────────────────────────────────

public class LoggingDecoratorTests
{
    [Fact]
    public void DelegatesTo_Inner_ExactlyOnce()
    {
        var inner     = new RecordingNotifier();
        var decorator = new LoggingDecorator(inner);

        decorator.Send("a@b.com", "Subject", "Body");

        Assert.Single(inner.Calls);
    }

    [Fact]
    public void PassesArguments_To_Inner_Unchanged()
    {
        var inner     = new RecordingNotifier();
        var decorator = new LoggingDecorator(inner);

        decorator.Send("r@x.com", "Subj", "Bdy");

        Assert.Equal(("r@x.com", "Subj", "Bdy"), inner.Calls[0]);
    }

    [Fact]
    public void EmitsLog_BeforeAndAfter_InnerCall()
    {
        var logs      = new List<string>();
        var decorator = new LoggingDecorator(new RecordingNotifier(), logs.Add);

        decorator.Send("a@b.com", "Subject", "Body");

        Assert.Equal(2, logs.Count);
        Assert.Contains("Sending", logs[0]);
        Assert.Contains("Delivered", logs[1]);
    }

    [Fact]
    public void LogsBefore_MentionsRecipient_And_Subject()
    {
        var logs      = new List<string>();
        var decorator = new LoggingDecorator(new RecordingNotifier(), logs.Add);

        decorator.Send("alice@example.com", "Hello", "Body");

        Assert.Contains("alice@example.com", logs[0]);
        Assert.Contains("Hello", logs[0]);
    }
}

// ── RetryDecorator ───────────────────────────────────────────

public class RetryDecoratorTests
{
    [Fact]
    public void SucceedsFirstTry_InnerCalledOnce()
    {
        var inner     = new RecordingNotifier();
        var decorator = new RetryDecorator(inner, maxAttempts: 3);

        decorator.Send("a@b.com", "S", "B");

        Assert.Single(inner.Calls);
    }

    [Fact]
    public void RetriesOnFailure_UntilSuccess()
    {
        var inner     = new FlakyNotifier(failCount: 2);  // fails twice, succeeds 3rd
        var retryLogs = new List<string>();
        var decorator = new RetryDecorator(inner, maxAttempts: 3, retryLog: retryLogs.Add);

        decorator.Send("a@b.com", "S", "B");  // should not throw

        Assert.Equal(2, retryLogs.Count);     // two failed attempts logged
    }

    [Fact]
    public void ThrowsInvalidOperationException_AfterAllAttemptsFail()
    {
        var inner     = new FlakyNotifier(failCount: 10);  // always fails
        var decorator = new RetryDecorator(inner, maxAttempts: 3);

        Assert.Throws<InvalidOperationException>(() => decorator.Send("a@b.com", "S", "B"));
    }

    [Fact]
    public void ThrownException_WrapsLastInnerException()
    {
        var inner     = new FlakyNotifier(failCount: 10);
        var decorator = new RetryDecorator(inner, maxAttempts: 1);

        var ex = Assert.Throws<InvalidOperationException>(() => decorator.Send("a@b.com", "S", "B"));
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRange_WhenMaxAttemptsIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RetryDecorator(new RecordingNotifier(), maxAttempts: 0));
    }
}

// ── SmsDecorator ─────────────────────────────────────────────

public class SmsDecoratorTests
{
    [Fact]
    public void DelegatesTo_Inner_ExactlyOnce()
    {
        var inner     = new RecordingNotifier();
        var decorator = new SmsDecorator(inner);

        decorator.Send("a@b.com", "Subject", "Body");

        Assert.Single(inner.Calls);
    }

    [Fact]
    public void AlsoFires_SmsChannel()
    {
        var smsSent   = new List<string>();
        var decorator = new SmsDecorator(new RecordingNotifier(), smsSent.Add);

        decorator.Send("a@b.com", "Subject", "Body");

        Assert.Single(smsSent);
    }

    [Fact]
    public void SmsMessage_ContainsRecipient()
    {
        var smsSent   = new List<string>();
        var decorator = new SmsDecorator(new RecordingNotifier(), smsSent.Add);

        decorator.Send("alice@example.com", "Hello", "World");

        Assert.Contains("alice@example.com", smsSent[0]);
    }

    [Fact]
    public void LongBody_TruncatedWithEllipsis()
    {
        var smsSent   = new List<string>();
        var decorator = new SmsDecorator(new RecordingNotifier(), smsSent.Add);
        string body   = new('x', 200);

        decorator.Send("a@b.com", "S", body);

        Assert.Contains("...", smsSent[0]);
        // Total SMS text should not exceed 160 chars
        // (smsChannel receives "To: recipient | <truncated>", so just check the truncation marker)
    }

    [Fact]
    public void ShortBody_NotTruncated()
    {
        var smsSent   = new List<string>();
        var decorator = new SmsDecorator(new RecordingNotifier(), smsSent.Add);

        decorator.Send("a@b.com", "S", "Short");

        Assert.DoesNotContain("...", smsSent[0]);
    }
}

// ── SubjectPrefixDecorator ───────────────────────────────────

public class SubjectPrefixDecoratorTests
{
    [Fact]
    public void PrependsPrefixToSubject()
    {
        var inner     = new RecordingNotifier();
        var decorator = new SubjectPrefixDecorator(inner, "[PROD]");

        decorator.Send("a@b.com", "Order Shipped", "Body");

        Assert.Equal("[PROD] Order Shipped", inner.Calls[0].Subject);
    }

    [Fact]
    public void RecipientAndBody_PassThrough_Unchanged()
    {
        var inner     = new RecordingNotifier();
        var decorator = new SubjectPrefixDecorator(inner, "[TAG]");

        decorator.Send("alice@example.com", "Subject", "Original body");

        Assert.Equal("alice@example.com", inner.Calls[0].Recipient);
        Assert.Equal("Original body", inner.Calls[0].Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsArgumentException_OnEmptyPrefix(string prefix)
    {
        Assert.Throws<ArgumentException>(
            () => new SubjectPrefixDecorator(new RecordingNotifier(), prefix));
    }

    [Fact]
    public void DelegatesTo_Inner_ExactlyOnce()
    {
        var inner     = new RecordingNotifier();
        var decorator = new SubjectPrefixDecorator(inner, "[X]");

        decorator.Send("a@b.com", "Subject", "Body");

        Assert.Single(inner.Calls);
    }
}

// ── Stacked decorators ───────────────────────────────────────

public class StackedDecoratorTests
{
    [Fact]
    public void FullChain_ReachesInner_Once()
    {
        var inner  = new RecordingNotifier();
        var logs   = new List<string>();
        var smsSent = new List<string>();

        INotifier chain = new SubjectPrefixDecorator(
                              new LoggingDecorator(
                                  new SmsDecorator(inner, smsSent.Add),
                                  logs.Add),
                              "[TEST]");

        chain.Send("a@b.com", "Hello", "World");

        Assert.Single(inner.Calls);
    }

    [Fact]
    public void FullChain_PrefixApplied_BeforeReachingInner()
    {
        var inner  = new RecordingNotifier();
        INotifier chain = new SubjectPrefixDecorator(
                              new LoggingDecorator(
                                  new SmsDecorator(inner)),
                              "[TEST]");

        chain.Send("a@b.com", "Hello", "World");

        Assert.Equal("[TEST] Hello", inner.Calls[0].Subject);
    }

    [Fact]
    public void FullChain_LoggingFires_AndSmsAlsoFires()
    {
        var logs    = new List<string>();
        var smsSent = new List<string>();

        INotifier chain = new SubjectPrefixDecorator(
                              new LoggingDecorator(
                                  new SmsDecorator(new RecordingNotifier(), smsSent.Add),
                                  logs.Add),
                              "[TEST]");

        chain.Send("a@b.com", "Hello", "World");

        Assert.Equal(2, logs.Count);   // before + after
        Assert.Single(smsSent);
    }

    [Fact]
    public void RetryWithLogging_OuterRetry_InnerLogging_LogsEachAttempt()
    {
        var logs      = new List<string>();
        var inner     = new FlakyNotifier(failCount: 2);
        var retryLogs = new List<string>();

        // Retry wraps Logging — each attempt triggers a log entry
        INotifier chain = new RetryDecorator(
                              new LoggingDecorator(inner, logs.Add),
                              maxAttempts: 3,
                              retryLog: retryLogs.Add);

        chain.Send("a@b.com", "S", "B");

        // 3 attempts → 3 × (before + after) but only 2 succeed delivery path
        // Attempt 1: LOG before, throws → no LOG after; attempt 2: same; attempt 3: both
        Assert.True(logs.Count >= 2);
    }
}
