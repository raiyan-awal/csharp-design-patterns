namespace BuilderPattern;

/// <summary>
/// ABSTRACT BUILDER — defines the steps for constructing an Email.
///
/// Each method returns IEmailBuilder so calls can be chained (fluent API).
/// Build() is the terminal step; it validates the accumulated state and
/// returns the finished product.
///
/// Returning the interface (not the concrete type) from each step means:
///   • Callers depend only on this contract
///   • The Director only knows IEmailBuilder
///   • Concrete implementations are swappable (e.g. a logging builder in tests)
/// </summary>
public interface IEmailBuilder
{
    // ── Required fields ─────────────────────────────────────────────────────

    /// <summary>Sets the primary recipient. Required before calling Build().</summary>
    IEmailBuilder To(string recipient);

    /// <summary>Sets the email subject line. Required before calling Build().</summary>
    IEmailBuilder WithSubject(string subject);

    /// <summary>Sets the message body. Required before calling Build().</summary>
    IEmailBuilder WithBody(string body);

    // ── Optional fields ─────────────────────────────────────────────────────

    /// <summary>Adds a CC recipient. Can be called multiple times.</summary>
    IEmailBuilder Cc(string recipient);

    /// <summary>Adds a BCC recipient. Can be called multiple times.</summary>
    IEmailBuilder Bcc(string recipient);

    /// <summary>Attaches a file by path. Can be called multiple times.</summary>
    IEmailBuilder Attach(string filePath);

    /// <summary>Marks the body as HTML (defaults to plain text).</summary>
    IEmailBuilder AsHtml();

    /// <summary>Sets the email priority (defaults to Normal).</summary>
    IEmailBuilder WithPriority(EmailPriority priority);

    /// <summary>Sets a reply-to address different from the sender.</summary>
    IEmailBuilder WithReplyTo(string replyTo);

    // ── Terminal step ────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the accumulated state and returns the completed Email.
    /// Throws <see cref="InvalidOperationException"/> if required fields are missing.
    /// </summary>
    Email Build();

    /// <summary>Resets all fields so the builder can be reused for a new email.</summary>
    IEmailBuilder Reset();
}
