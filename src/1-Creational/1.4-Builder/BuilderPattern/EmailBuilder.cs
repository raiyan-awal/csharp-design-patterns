namespace BuilderPattern;

/// <summary>
/// CONCRETE BUILDER — accumulates email state step by step, then
/// produces a validated, immutable Email in Build().
///
/// Design decisions:
///   • Returns <c>this</c> cast to IEmailBuilder for fluent chaining
///   • CC, BCC, Attachments are List&lt;T&gt; internally; exposed as IReadOnlyList&lt;T&gt;
///   • Validation is centralised in Build() — no half-built objects escape
///   • Reset() lets the same builder instance be reused for multiple emails
/// </summary>
public sealed class EmailBuilder : IEmailBuilder
{
    // ── Accumulated state ────────────────────────────────────────────────────
    private string?       _to;
    private string?       _subject;
    private string?       _body;
    private readonly List<string> _cc          = [];
    private readonly List<string> _bcc         = [];
    private readonly List<string> _attachments = [];
    private bool          _isHtml   = false;
    private EmailPriority _priority = EmailPriority.Normal;
    private string?       _replyTo;

    // ── Required ─────────────────────────────────────────────────────────────

    public IEmailBuilder To(string recipient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient, nameof(recipient));
        _to = recipient;
        return this;
    }

    public IEmailBuilder WithSubject(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject, nameof(subject));
        _subject = subject;
        return this;
    }

    public IEmailBuilder WithBody(string body)
    {
        ArgumentNullException.ThrowIfNull(body, nameof(body));
        _body = body;
        return this;
    }

    // ── Optional ─────────────────────────────────────────────────────────────

    public IEmailBuilder Cc(string recipient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient, nameof(recipient));
        _cc.Add(recipient);
        return this;
    }

    public IEmailBuilder Bcc(string recipient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient, nameof(recipient));
        _bcc.Add(recipient);
        return this;
    }

    public IEmailBuilder Attach(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        _attachments.Add(filePath);
        return this;
    }

    public IEmailBuilder AsHtml()
    {
        _isHtml = true;
        return this;
    }

    public IEmailBuilder WithPriority(EmailPriority priority)
    {
        _priority = priority;
        return this;
    }

    public IEmailBuilder WithReplyTo(string replyTo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replyTo, nameof(replyTo));
        _replyTo = replyTo;
        return this;
    }

    // ── Terminal step ─────────────────────────────────────────────────────────

    /// <summary>
    /// Validates required fields, then constructs and returns the finished Email.
    /// The builder's state is NOT reset — call Reset() explicitly if you want to
    /// reuse the same builder for another email.
    /// </summary>
    public Email Build()
    {
        // Guard: all three required fields must be set
        if (string.IsNullOrWhiteSpace(_to))
            throw new InvalidOperationException(
                "Cannot build email: recipient (To) is required. Call To(address) first.");

        if (string.IsNullOrWhiteSpace(_subject))
            throw new InvalidOperationException(
                "Cannot build email: subject is required. Call WithSubject(subject) first.");

        if (_body is null)
            throw new InvalidOperationException(
                "Cannot build email: body is required. Call WithBody(body) first.");

        return new Email(
            to:            _to,
            subject:       _subject,
            body:          _body,
            ccRecipients:  _cc.AsReadOnly(),
            bccRecipients: _bcc.AsReadOnly(),
            attachments:   _attachments.AsReadOnly(),
            isHtml:        _isHtml,
            priority:      _priority,
            replyTo:       _replyTo
        );
    }

    /// <summary>
    /// Clears all accumulated state so this builder instance can be reused.
    /// </summary>
    public IEmailBuilder Reset()
    {
        _to       = null;
        _subject  = null;
        _body     = null;
        _replyTo  = null;
        _isHtml   = false;
        _priority = EmailPriority.Normal;
        _cc.Clear();
        _bcc.Clear();
        _attachments.Clear();
        return this;
    }
}
