namespace BuilderPattern;

/// <summary>
/// Priority level for an outgoing email.
/// </summary>
public enum EmailPriority
{
    Low,
    Normal,
    High
}

/// <summary>
/// The PRODUCT — a fully-constructed, immutable email message.
///
/// The constructor is internal so only the EmailBuilder (same assembly) can
/// create instances. Consumers work with IEmailBuilder and receive a finished
/// Email from Build() — they never touch the constructor directly.
/// </summary>
public sealed class Email
{
    // ── Required fields ─────────────────────────────────────────────────────
    public string To         { get; }
    public string Subject    { get; }
    public string Body       { get; }

    // ── Optional fields ─────────────────────────────────────────────────────
    public IReadOnlyList<string> CcRecipients  { get; }
    public IReadOnlyList<string> BccRecipients { get; }
    public IReadOnlyList<string> Attachments   { get; }
    public bool          IsHtml    { get; }
    public EmailPriority Priority  { get; }
    public string?       ReplyTo   { get; }

    /// <summary>
    /// Internal constructor — called only by EmailBuilder.Build().
    /// All validation lives in the builder; by the time this runs the data
    /// is known to be valid.
    /// </summary>
    internal Email(
        string               to,
        string               subject,
        string               body,
        IReadOnlyList<string> ccRecipients,
        IReadOnlyList<string> bccRecipients,
        IReadOnlyList<string> attachments,
        bool                 isHtml,
        EmailPriority        priority,
        string?              replyTo)
    {
        To            = to;
        Subject       = subject;
        Body          = body;
        CcRecipients  = ccRecipients;
        BccRecipients = bccRecipients;
        Attachments   = attachments;
        IsHtml        = isHtml;
        Priority      = priority;
        ReplyTo       = replyTo;
    }

    /// <summary>
    /// Human-readable summary for demo output.
    /// </summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"  To      : {To}");
        sb.AppendLine($"  Subject : {Subject}");
        sb.AppendLine($"  Body    : {Body}");
        sb.AppendLine($"  Format  : {(IsHtml ? "HTML" : "Plain text")}");
        sb.AppendLine($"  Priority: {Priority}");

        if (ReplyTo is not null)
            sb.AppendLine($"  Reply-To: {ReplyTo}");

        if (CcRecipients.Count > 0)
            sb.AppendLine($"  CC      : {string.Join(", ", CcRecipients)}");

        if (BccRecipients.Count > 0)
            sb.AppendLine($"  BCC     : {string.Join(", ", BccRecipients)}");

        if (Attachments.Count > 0)
            sb.AppendLine($"  Attachments ({Attachments.Count}): {string.Join(", ", Attachments)}");

        return sb.ToString().TrimEnd();
    }
}
