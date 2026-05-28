namespace BuilderPattern;

/// <summary>
/// DIRECTOR — knows HOW to build specific email types using IEmailBuilder.
///
/// The Director encapsulates construction recipes for common email templates.
/// It only uses the IEmailBuilder interface — it has zero knowledge of
/// EmailBuilder's internals or the Email class's constructor.
///
/// Benefits of using a Director:
///   • Common email shapes are named, reusable, and tested in one place
///   • Callers don't have to remember which steps go with which template
///   • The Director can be swapped out (e.g., an A/B-test variant)
///   • Not mandatory — callers can always drive the builder directly
/// </summary>
public sealed class EmailDirector(IEmailBuilder builder)
{
    private readonly IEmailBuilder _builder = builder;

    /// <summary>
    /// Builds a standard welcome email for new user registrations.
    ///
    /// Produces: plain-text, Normal priority, no CC/BCC.
    /// </summary>
    public Email BuildWelcomeEmail(string to, string firstName)
    {
        return _builder
            .Reset()
            .To(to)
            .WithSubject($"Welcome to our platform, {firstName}!")
            .WithBody(
                $"Hi {firstName},\n\n" +
                "Your account has been created successfully. " +
                "We're excited to have you on board!\n\n" +
                "If you have any questions, just reply to this email.\n\n" +
                "The Team")
            .WithPriority(EmailPriority.Normal)
            .Build();
    }

    /// <summary>
    /// Builds a password-reset email with a High priority flag.
    ///
    /// Produces: plain-text, High priority, reply-to set to no-reply address.
    /// </summary>
    public Email BuildPasswordResetEmail(string to, string resetLink)
    {
        return _builder
            .Reset()
            .To(to)
            .WithSubject("Password reset request")
            .WithBody(
                $"We received a request to reset your password.\n\n" +
                $"Click the link below within 30 minutes:\n{resetLink}\n\n" +
                "If you didn't request this, you can safely ignore this email.")
            .WithPriority(EmailPriority.High)
            .WithReplyTo("no-reply@example.com")
            .Build();
    }

    /// <summary>
    /// Builds an HTML newsletter with Low priority and an unsubscribe BCC.
    ///
    /// Produces: HTML, Low priority, BCC to unsubscribe handler.
    /// </summary>
    public Email BuildNewsletterEmail(string to, string htmlContent)
    {
        return _builder
            .Reset()
            .To(to)
            .WithSubject("This week's updates — don't miss out!")
            .WithBody(htmlContent)
            .AsHtml()
            .WithPriority(EmailPriority.Low)
            .Bcc("unsubscribe-tracker@example.com")
            .Build();
    }

    /// <summary>
    /// Builds an invoice email with an attached PDF, CC to accounting.
    ///
    /// Produces: HTML, Normal priority, one attachment, one CC.
    /// </summary>
    public Email BuildInvoiceEmail(string to, string customerName, string invoicePdfPath)
    {
        return _builder
            .Reset()
            .To(to)
            .WithSubject($"Your invoice from Example Corp — {DateTime.UtcNow:MMMM yyyy}")
            .WithBody(
                $"<p>Dear {customerName},</p>" +
                "<p>Please find your invoice attached.</p>" +
                "<p>Thank you for your business!</p>")
            .AsHtml()
            .Attach(invoicePdfPath)
            .Cc("accounting@example.com")
            .Build();
    }
}
