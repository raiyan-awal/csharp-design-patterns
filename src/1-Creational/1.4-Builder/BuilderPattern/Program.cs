namespace BuilderPattern;

/// <summary>
/// BUILDER PATTERN DEMONSTRATION
///
/// This program demonstrates the Builder pattern using an email message as the
/// product. It shows:
/// 1. Basic fluent builder — step-by-step construction with method chaining
/// 2. Complex email — all optional fields combined
/// 3. Director — pre-built templates for common email types
/// 4. Why Builder? — comparing with a constructor that takes many arguments
/// 5. Builder reuse and validation — Reset(), and Build() guard clauses
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== BUILDER PATTERN DEMO ===");
        Console.WriteLine("Press any key after each section to continue.\n");

        // ── DEMONSTRATION 1: Basic fluent builder ────────────────────────────
        Console.WriteLine("--- Demonstration 1: Basic Fluent Builder ---");
        Console.WriteLine("Building a simple notification email step by step.\n");

        IEmailBuilder builder = new EmailBuilder();

        Email simpleEmail = builder
            .To("alice@example.com")
            .WithSubject("Your order has shipped!")
            .WithBody("Hi Alice,\n\nYour order #4521 has been dispatched and is on its way.")
            .Build();

        Console.WriteLine("Result:");
        Console.WriteLine(simpleEmail);

        Console.WriteLine("\n→ Three required fields set, everything else uses defaults.");
        Console.WriteLine("→ Method chaining reads like a sentence — no positional argument confusion.");
        Console.WriteLine("→ The Email object is immutable — once built, it cannot be altered.");

        Pause();

        // ── DEMONSTRATION 2: Complex email with all optional fields ──────────
        Console.WriteLine("--- Demonstration 2: Complex Email (All Optional Fields) ---");
        Console.WriteLine("Building a full-featured email with CC, BCC, attachments, HTML, and priority.\n");

        Email complexEmail = builder
            .Reset()
            .To("bob@example.com")
            .WithSubject("Q3 Financial Report — Confidential")
            .WithBody("<h1>Q3 Financial Report</h1><p>Please review the attached document.</p>")
            .AsHtml()
            .WithPriority(EmailPriority.High)
            .Cc("cfo@example.com")
            .Cc("ceo@example.com")
            .Bcc("audit@example.com")
            .Attach("/reports/Q3-financial-report.pdf")
            .Attach("/reports/Q3-appendix.xlsx")
            .WithReplyTo("finance-team@example.com")
            .Build();

        Console.WriteLine("Result:");
        Console.WriteLine(complexEmail);

        Console.WriteLine("\n→ CC and Attach() can be called multiple times — they accumulate.");
        Console.WriteLine("→ The same builder was reused after Reset() — no new object allocation needed.");
        Console.WriteLine("→ Only one Build() call at the end — no partial/broken Email objects can exist.");

        Pause();

        // ── DEMONSTRATION 3: Director ────────────────────────────────────────
        Console.WriteLine("--- Demonstration 3: Director — Pre-Built Templates ---");
        Console.WriteLine("The Director encapsulates construction recipes for common email types.\n");

        var director = new EmailDirector(new EmailBuilder());

        Console.WriteLine("  [Welcome Email]");
        var welcome = director.BuildWelcomeEmail("carol@example.com", "Carol");
        Console.WriteLine(welcome);

        Console.WriteLine("  [Password Reset Email]");
        var passwordReset = director.BuildPasswordResetEmail(
            "dave@example.com",
            "https://example.com/reset?token=abc123xyz");
        Console.WriteLine(passwordReset);

        Console.WriteLine("  [Newsletter Email]");
        var newsletter = director.BuildNewsletterEmail(
            "eve@example.com",
            "<h1>Weekly Digest</h1><p>Here's what happened this week...</p>");
        Console.WriteLine(newsletter);

        Console.WriteLine("  [Invoice Email]");
        var invoice = director.BuildInvoiceEmail(
            "frank@example.com",
            "Frank",
            "/invoices/INV-2026-0542.pdf");
        Console.WriteLine(invoice);

        Console.WriteLine("→ Four different email shapes from four method calls.");
        Console.WriteLine("→ The Director only knows IEmailBuilder — not EmailBuilder or Email's constructor.");
        Console.WriteLine("→ Callers don't memorise which steps go with which template.");

        Pause();

        // ── DEMONSTRATION 4: Why Builder? ────────────────────────────────────
        Console.WriteLine("--- Demonstration 4: Why Builder? ---");
        Console.WriteLine("Comparing the builder approach to the constructor-with-many-parameters anti-pattern.\n");

        Console.WriteLine("WITHOUT builder (constructor with 9 parameters):");
        Console.WriteLine("""
  var email = new Email(
      "to@example.com",
      "Subject",
      "Body",
      new[] { "cc@example.com" },
      Array.Empty<string>(),
      new[] { "file.pdf" },
      isHtml: true,
      EmailPriority.High,
      "reply@example.com"
  );
""");
        Console.WriteLine("  Problems:");
        Console.WriteLine("  ✗ What does the 4th parameter mean? You have to look at the constructor signature.");
        Console.WriteLine("  ✗ What if you only need To, Subject, and Body? Pass 6 nulls/defaults anyway.");
        Console.WriteLine("  ✗ Adding a 10th field requires updating every call site in the codebase.");
        Console.WriteLine("  ✗ Nothing prevents you passing BCC list where CC list was expected (same type).");

        Console.WriteLine("\nWITH builder:");
        Console.WriteLine("""
  var email = new EmailBuilder()
      .To("to@example.com")
      .WithSubject("Subject")
      .WithBody("Body")
      .Attach("file.pdf")
      .AsHtml()
      .WithPriority(EmailPriority.High)
      .WithReplyTo("reply@example.com")
      .Build();
""");
        Console.WriteLine("  Benefits:");
        Console.WriteLine("  ✓ Each method call is self-documenting — .Cc() can never be mistaken for .Bcc().");
        Console.WriteLine("  ✓ Skip optional fields entirely — only set what you need.");
        Console.WriteLine("  ✓ Adding a new optional field adds one method to the builder — zero call-site changes.");
        Console.WriteLine("  ✓ Validation in Build() ensures required fields are present before the object exists.");

        Pause();

        // ── DEMONSTRATION 5: Validation and builder reuse ────────────────────
        Console.WriteLine("--- Demonstration 5: Build() Validation and Builder Reuse ---");
        Console.WriteLine("Showing that Build() guards against missing required fields.\n");

        Console.WriteLine("Attempting to build without setting 'To':");
        try
        {
            var badEmail = new EmailBuilder()
                .WithSubject("Forgot something")
                .WithBody("This email has no recipient.")
                .Build();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  ✓ InvalidOperationException caught: {ex.Message}");
        }

        Console.WriteLine("\nAttempting to build without setting 'Subject':");
        try
        {
            var badEmail = new EmailBuilder()
                .To("someone@example.com")
                .WithBody("No subject set.")
                .Build();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  ✓ InvalidOperationException caught: {ex.Message}");
        }

        Console.WriteLine("\nReusing a builder for two different emails:");

        IEmailBuilder reusable = new EmailBuilder();

        Email first = reusable
            .To("first@example.com")
            .WithSubject("First email")
            .WithBody("Hello, First!")
            .Build();

        Email second = reusable
            .Reset()
            .To("second@example.com")
            .WithSubject("Second email")
            .WithBody("Hello, Second!")
            .Build();

        Console.WriteLine($"\n  Email 1 → To: {first.To}, Subject: {first.Subject}");
        Console.WriteLine($"  Email 2 → To: {second.To}, Subject: {second.Subject}");
        Console.WriteLine("\n→ Reset() cleared all state — Email 2 has no leftover fields from Email 1.");
        Console.WriteLine("→ Both are independent, immutable objects.");

        Pause();

        // ── SUMMARY ──────────────────────────────────────────────────────────
        Console.WriteLine("=== KEY TAKEAWAYS ===");
        Console.WriteLine("✓ Builder separates CONSTRUCTION logic from the PRODUCT itself");
        Console.WriteLine("✓ Fluent interface makes optional parameters readable and explicit");
        Console.WriteLine("✓ Required fields are enforced in Build() — invalid objects cannot be created");
        Console.WriteLine("✓ Director encapsulates named construction recipes without owning the product");
        Console.WriteLine("✓ Reset() allows builder reuse — one instance can produce many independent products");

        Console.WriteLine("\n=== REAL-WORLD USAGE IN .NET ===");
        Console.WriteLine("• WebApplicationBuilder / IHostBuilder — ASP.NET Core app setup");
        Console.WriteLine("• DbContextOptionsBuilder — configuring EF Core");
        Console.WriteLine("• SqlConnectionStringBuilder — composing connection strings");
        Console.WriteLine("• HttpClient / RestSharp request builders");
        Console.WriteLine("• StringBuilder — efficient string assembly");
        Console.WriteLine("• MailMessage (System.Net.Mail) — structurally identical to this example");

        Console.WriteLine("\nDemo complete.");
    }

    static void Pause()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}
