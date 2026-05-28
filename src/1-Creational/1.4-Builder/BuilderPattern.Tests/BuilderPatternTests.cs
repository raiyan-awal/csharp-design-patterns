using BuilderPattern;

namespace BuilderPattern.Tests;

/// <summary>
/// Unit tests for the Builder pattern implementation.
///
/// These tests verify that:
/// 1. Required fields must be set before Build() succeeds
/// 2. Default values are applied correctly for optional fields
/// 3. Each builder method sets the correct field on the product
/// 4. Additive methods (Cc, Bcc, Attach) accumulate values correctly
/// 5. The builder resets cleanly and can be reused for independent products
/// 6. Method chaining returns the same builder instance (fluent API)
/// 7. The Director produces correctly shaped emails for each template
/// </summary>
public class BuilderPatternTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // BUILD VALIDATION — required fields
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithoutTo_ThrowsInvalidOperationException()
    {
        var builder = new EmailBuilder()
            .WithSubject("Subject")
            .WithBody("Body");

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithoutSubject_ThrowsInvalidOperationException()
    {
        var builder = new EmailBuilder()
            .To("a@example.com")
            .WithBody("Body");

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithoutBody_ThrowsInvalidOperationException()
    {
        var builder = new EmailBuilder()
            .To("a@example.com")
            .WithSubject("Subject");

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithAllRequiredFields_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            new EmailBuilder()
                .To("a@example.com")
                .WithSubject("Subject")
                .WithBody("Body")
                .Build());

        Assert.Null(exception);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REQUIRED FIELDS — values are transferred to the product
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_SetsTo_Correctly()
    {
        var email = new EmailBuilder()
            .To("alice@example.com")
            .WithSubject("Hi")
            .WithBody("Hello")
            .Build();

        Assert.Equal("alice@example.com", email.To);
    }

    [Fact]
    public void Build_SetsSubject_Correctly()
    {
        var email = new EmailBuilder()
            .To("a@example.com")
            .WithSubject("Test Subject")
            .WithBody("Body")
            .Build();

        Assert.Equal("Test Subject", email.Subject);
    }

    [Fact]
    public void Build_SetsBody_Correctly()
    {
        var email = new EmailBuilder()
            .To("a@example.com")
            .WithSubject("Subject")
            .WithBody("Hello, World!")
            .Build();

        Assert.Equal("Hello, World!", email.Body);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DEFAULT VALUES — optional fields start at sensible defaults
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_DefaultPriority_IsNormal()
    {
        var email = BuildMinimal();

        Assert.Equal(EmailPriority.Normal, email.Priority);
    }

    [Fact]
    public void Build_DefaultIsHtml_IsFalse()
    {
        var email = BuildMinimal();

        Assert.False(email.IsHtml);
    }

    [Fact]
    public void Build_DefaultReplyTo_IsNull()
    {
        var email = BuildMinimal();

        Assert.Null(email.ReplyTo);
    }

    [Fact]
    public void Build_DefaultCcList_IsEmpty()
    {
        var email = BuildMinimal();

        Assert.Empty(email.CcRecipients);
    }

    [Fact]
    public void Build_DefaultBccList_IsEmpty()
    {
        var email = BuildMinimal();

        Assert.Empty(email.BccRecipients);
    }

    [Fact]
    public void Build_DefaultAttachments_IsEmpty()
    {
        var email = BuildMinimal();

        Assert.Empty(email.Attachments);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OPTIONAL FIELDS — each setter applies correctly
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WithPriority_High_SetsCorrectly()
    {
        var email = new EmailBuilder()
            .To("a@example.com").WithSubject("S").WithBody("B")
            .WithPriority(EmailPriority.High)
            .Build();

        Assert.Equal(EmailPriority.High, email.Priority);
    }

    [Fact]
    public void AsHtml_SetsIsHtml_True()
    {
        var email = new EmailBuilder()
            .To("a@example.com").WithSubject("S").WithBody("<p>B</p>")
            .AsHtml()
            .Build();

        Assert.True(email.IsHtml);
    }

    [Fact]
    public void WithReplyTo_SetsReplyTo_Correctly()
    {
        var email = new EmailBuilder()
            .To("a@example.com").WithSubject("S").WithBody("B")
            .WithReplyTo("noreply@example.com")
            .Build();

        Assert.Equal("noreply@example.com", email.ReplyTo);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ADDITIVE METHODS — Cc, Bcc, Attach accumulate values
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Cc_SingleRecipient_AddedToList()
    {
        var email = new EmailBuilder()
            .To("a@example.com").WithSubject("S").WithBody("B")
            .Cc("cc1@example.com")
            .Build();

        Assert.Single(email.CcRecipients);
        Assert.Contains("cc1@example.com", email.CcRecipients);
    }

    [Fact]
    public void Cc_MultipleRecipients_AllAddedToList()
    {
        var email = new EmailBuilder()
            .To("a@example.com").WithSubject("S").WithBody("B")
            .Cc("cc1@example.com")
            .Cc("cc2@example.com")
            .Cc("cc3@example.com")
            .Build();

        Assert.Equal(3, email.CcRecipients.Count);
        Assert.Contains("cc2@example.com", email.CcRecipients);
    }

    [Fact]
    public void Bcc_MultipleRecipients_AllAddedToList()
    {
        var email = new EmailBuilder()
            .To("a@example.com").WithSubject("S").WithBody("B")
            .Bcc("bcc1@example.com")
            .Bcc("bcc2@example.com")
            .Build();

        Assert.Equal(2, email.BccRecipients.Count);
    }

    [Fact]
    public void Attach_MultipleFiles_AllAddedToList()
    {
        var email = new EmailBuilder()
            .To("a@example.com").WithSubject("S").WithBody("B")
            .Attach("/files/doc1.pdf")
            .Attach("/files/doc2.xlsx")
            .Build();

        Assert.Equal(2, email.Attachments.Count);
        Assert.Contains("/files/doc1.pdf", email.Attachments);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FLUENT API — each method returns IEmailBuilder for chaining
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MethodChaining_ReturnsTheSameBuilderInstance()
    {
        // Arrange
        IEmailBuilder builder = new EmailBuilder();

        // Act — each call should return the same builder (this cast to interface)
        var returned = builder.To("a@example.com");

        // Assert — not a new object
        Assert.Same(builder, returned);
    }

    [Fact]
    public void Reset_ReturnsTheSameBuilderInstance()
    {
        IEmailBuilder builder = new EmailBuilder();
        var returned = builder.Reset();

        Assert.Same(builder, returned);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BUILDER REUSE — Reset() produces independent products
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsTo_ForNextBuild()
    {
        IEmailBuilder builder = new EmailBuilder();

        // Build first email
        builder.To("first@example.com").WithSubject("S").WithBody("B").Build();

        // Reset and build second — To is different
        var second = builder
            .Reset()
            .To("second@example.com")
            .WithSubject("S2")
            .WithBody("B2")
            .Build();

        Assert.Equal("second@example.com", second.To);
    }

    [Fact]
    public void Reset_ClearsCcList_ForNextBuild()
    {
        IEmailBuilder builder = new EmailBuilder();

        // First email has CC
        builder.To("a@example.com").WithSubject("S").WithBody("B")
               .Cc("cc@example.com").Build();

        // After reset, no CC
        var second = builder.Reset()
            .To("b@example.com").WithSubject("S2").WithBody("B2")
            .Build();

        Assert.Empty(second.CcRecipients);
    }

    [Fact]
    public void Build_ProducesIndependentInstances()
    {
        // Two separate builders produce two separate Email objects
        var email1 = new EmailBuilder()
            .To("x@example.com").WithSubject("X").WithBody("Body X")
            .Build();

        var email2 = new EmailBuilder()
            .To("y@example.com").WithSubject("Y").WithBody("Body Y")
            .Build();

        Assert.NotSame(email1, email2);
        Assert.NotEqual(email1.To, email2.To);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DIRECTOR — template methods produce correct shapes
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Director_BuildWelcomeEmail_SetsToAndSubjectCorrectly()
    {
        var director = new EmailDirector(new EmailBuilder());
        var email = director.BuildWelcomeEmail("carol@example.com", "Carol");

        Assert.Equal("carol@example.com", email.To);
        Assert.Contains("Carol", email.Subject);
    }

    [Fact]
    public void Director_BuildWelcomeEmail_IsPlainText_NormalPriority()
    {
        var director = new EmailDirector(new EmailBuilder());
        var email = director.BuildWelcomeEmail("a@example.com", "Alice");

        Assert.False(email.IsHtml);
        Assert.Equal(EmailPriority.Normal, email.Priority);
    }

    [Fact]
    public void Director_BuildPasswordResetEmail_IsHighPriority()
    {
        var director = new EmailDirector(new EmailBuilder());
        var email = director.BuildPasswordResetEmail("a@example.com", "https://example.com/reset?t=xyz");

        Assert.Equal(EmailPriority.High, email.Priority);
    }

    [Fact]
    public void Director_BuildPasswordResetEmail_HasReplyTo()
    {
        var director = new EmailDirector(new EmailBuilder());
        var email = director.BuildPasswordResetEmail("a@example.com", "https://example.com/reset?t=xyz");

        Assert.NotNull(email.ReplyTo);
    }

    [Fact]
    public void Director_BuildNewsletterEmail_IsHtml_LowPriority()
    {
        var director = new EmailDirector(new EmailBuilder());
        var email = director.BuildNewsletterEmail("a@example.com", "<p>Content</p>");

        Assert.True(email.IsHtml);
        Assert.Equal(EmailPriority.Low, email.Priority);
    }

    [Fact]
    public void Director_BuildNewsletterEmail_HasBccRecipient()
    {
        var director = new EmailDirector(new EmailBuilder());
        var email = director.BuildNewsletterEmail("a@example.com", "<p>Content</p>");

        Assert.NotEmpty(email.BccRecipients);
    }

    [Fact]
    public void Director_BuildInvoiceEmail_HasAttachment()
    {
        var director = new EmailDirector(new EmailBuilder());
        var email = director.BuildInvoiceEmail("a@example.com", "Alice", "/invoices/INV-001.pdf");

        Assert.Single(email.Attachments);
        Assert.Contains("/invoices/INV-001.pdf", email.Attachments);
    }

    [Fact]
    public void Director_BuildInvoiceEmail_HasCcRecipient()
    {
        var director = new EmailDirector(new EmailBuilder());
        var email = director.BuildInvoiceEmail("a@example.com", "Alice", "/invoices/INV-001.pdf");

        Assert.NotEmpty(email.CcRecipients);
    }

    [Fact]
    public void Director_CanBeUsedWithAnyIEmailBuilderImplementation()
    {
        // Director depends only on the interface — any concrete builder works
        IEmailBuilder builder = new EmailBuilder();
        var director = new EmailDirector(builder);

        var exception = Record.Exception(() =>
            director.BuildWelcomeEmail("a@example.com", "Alice"));

        Assert.Null(exception);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPER
    // ─────────────────────────────────────────────────────────────────────────

    private static Email BuildMinimal() =>
        new EmailBuilder()
            .To("a@example.com")
            .WithSubject("Subject")
            .WithBody("Body")
            .Build();
}
