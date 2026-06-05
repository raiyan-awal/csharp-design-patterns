namespace DecoratorPattern;

// Prepends a fixed tag to every notification subject.
// Common uses: environment labels ("[PROD]", "[STAGING]"),
// severity markers ("[URGENT]"), or team routing tags ("[OPS]").
// Recipient and body pass through unchanged.
public sealed class SubjectPrefixDecorator : NotifierDecorator
{
    private readonly string _prefix;

    public SubjectPrefixDecorator(INotifier inner, string prefix) : base(inner)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));
        _prefix = prefix;
    }

    public override void Send(string recipient, string subject, string body)
        => base.Send(recipient, $"{_prefix} {subject}", body);
}
