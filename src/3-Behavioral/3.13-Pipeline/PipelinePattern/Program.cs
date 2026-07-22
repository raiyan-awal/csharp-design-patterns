using PipelinePattern;
using PipelinePattern.Steps;

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}

static void Header(string title)
{
    Console.WriteLine(new string('─', 64));
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('─', 64));
}

static void PrintResult(LoanContext ctx)
{
    Console.WriteLine();
    foreach (var line in ctx.Log) Console.WriteLine(line);
    Console.WriteLine();
    if (ctx.IsApproved)
        Console.WriteLine($"  OUTCOME: APPROVED  — {ctx.OutcomeNote}");
    else if (ctx.IsRejected)
        Console.WriteLine($"  OUTCOME: REJECTED  — {ctx.OutcomeNote}");
    else
        Console.WriteLine("  OUTCOME: INCOMPLETE (pipeline did not reach the decision step)");
}

// Standard full pipeline — all 6 steps in order
static Pipeline<LoanContext> BuildFullPipeline() =>
    new Pipeline<LoanContext>()
        .AddStep(new ApplicationValidationStep())
        .AddStep(new CreditScoreStep())
        .AddStep(new DebtToIncomeStep())
        .AddStep(new AmlScreeningStep())
        .AddStep(new RiskClassificationStep())
        .AddStep(new LoanDecisionStep());

Console.WriteLine("=== Pipeline Pattern — Loan Application Processing ===\n");

// ─── THE PROBLEM ──────────────────────────────────────────────────────────────
Header("THE PROBLEM — procedural approach without a pipeline");
Console.WriteLine("""

  Without a pipeline, processing logic grows into a single method:

    public LoanResult ProcessLoan(LoanApplication app)
    {
        // Validate
        if (string.IsNullOrEmpty(app.Name)) return Reject("Name required");
        if (app.Sin.Length != 9)            return Reject("Invalid SIN");
        if (app.Amount < 1000)              return Reject("Amount too low");

        // Credit check
        if (app.CreditScore < 600) return Reject("Low credit score");

        // DTI
        var dti = CalculateDti(app);
        if (dti > 0.44m)  return Reject("DTI too high");

        // AML
        if (IsWatchlisted(app)) return Reject("AML flag");

        // Classify + decide (all inlined here)
        var risk = ClassifyRisk(app, dti);
        return risk == "High" ? ApproveReduced(...) : ApproveFullAmount(...);
    }

  Adding a new step (fraud score, income verification, insurance check)
  means editing this method every time. Steps cannot be reordered or
  composed differently per product. Pipeline separates each step into
  its own class and assembles them at the call site.

""");
Pause();

// ─── DEMO 1: Low-risk — full approval ────────────────────────────────────────
Header("DEMO 1 — Low-risk applicant: all steps pass, full approval");
Console.WriteLine("  Applicant: Sophie Tremblay | Score: 810 | Income: $145,000 | Loan: $320,000\n");
{
    var app = new LoanApplication
    {
        ApplicantName   = "Sophie Tremblay",
        Sin             = "123456789",
        RequestedAmount = 320_000m,
        AnnualIncome    = 145_000m,
        MonthlyDebt     = 400m,
        CreditScore     = 810,
        OnWatchlist     = false
    };
    var ctx = BuildFullPipeline().Run(new LoanContext(app));
    PrintResult(ctx);
}
Pause();

// ─── DEMO 2: Rejected at validation ──────────────────────────────────────────
Header("DEMO 2 — Rejected at Step 1: invalid SIN");
Console.WriteLine("  Applicant: Marcus Reid | SIN: '12345' (only 5 digits)\n");
{
    var app = new LoanApplication
    {
        ApplicantName   = "Marcus Reid",
        Sin             = "12345",
        RequestedAmount = 200_000m,
        AnnualIncome    = 90_000m,
        CreditScore     = 700,
        OnWatchlist     = false
    };
    var ctx = BuildFullPipeline().Run(new LoanContext(app));
    PrintResult(ctx);
}
Pause();

// ─── DEMO 3: Rejected at credit score ────────────────────────────────────────
Header("DEMO 3 — Rejected at Step 2: credit score too low");
Console.WriteLine("  Applicant: Devon Park | Score: 540 (minimum: 600)\n");
{
    var app = new LoanApplication
    {
        ApplicantName   = "Devon Park",
        Sin             = "987654321",
        RequestedAmount = 180_000m,
        AnnualIncome    = 75_000m,
        MonthlyDebt     = 300m,
        CreditScore     = 540,
        OnWatchlist     = false
    };
    var ctx = BuildFullPipeline().Run(new LoanContext(app));
    PrintResult(ctx);
}
Pause();

// ─── DEMO 4: Rejected at TDS ratio ───────────────────────────────────────────
Header("DEMO 4 — Rejected at Step 3: TDS ratio exceeds 44%");
Console.WriteLine("  Applicant: Aisha Kowalski | Income: $60,000 | Existing debt: $2,200/mo | Loan: $450,000\n");
{
    var app = new LoanApplication
    {
        ApplicantName   = "Aisha Kowalski",
        Sin             = "456789012",
        RequestedAmount = 450_000m,
        AnnualIncome    =  60_000m,
        MonthlyDebt     =   2_200m,
        CreditScore     =     690,
        OnWatchlist     = false
    };
    var ctx = BuildFullPipeline().Run(new LoanContext(app));
    PrintResult(ctx);
}
Pause();

// ─── DEMO 5: Rejected at AML screening ───────────────────────────────────────
Header("DEMO 5 — Rejected at Step 4: flagged by AML screening");
Console.WriteLine("  Applicant: Raj Mehta | All financial metrics fine — but on FINTRAC watchlist\n");
{
    var app = new LoanApplication
    {
        ApplicantName   = "Raj Mehta",
        Sin             = "321654987",
        RequestedAmount = 250_000m,
        AnnualIncome    = 110_000m,
        MonthlyDebt     =     500m,
        CreditScore     =     730,
        OnWatchlist     = true     // ← triggers AML rejection
    };
    var ctx = BuildFullPipeline().Run(new LoanContext(app));
    PrintResult(ctx);
}
Pause();

// ─── DEMO 6: High-risk — conditional approval ────────────────────────────────
Header("DEMO 6 — High-risk applicant: conditional approval at reduced amount");
Console.WriteLine("  Applicant: Lena Osei | Score: 625 — passes all screens but classified high-risk\n");
{
    var app = new LoanApplication
    {
        ApplicantName   = "Lena Osei",
        Sin             = "654321098",
        RequestedAmount = 300_000m,
        AnnualIncome    =  90_000m,
        MonthlyDebt     =     800m,
        CreditScore     =     625,
        OnWatchlist     = false
    };
    var ctx = BuildFullPipeline().Run(new LoanContext(app));
    PrintResult(ctx);
}
Pause();

// ─── DEMO 7: Custom pipeline — pre-qualification (no AML, no final decision) ─
Header("DEMO 7 — Custom pipeline: pre-qualification check (3 steps only)");
Console.WriteLine("  The same steps assembled differently for a lightweight pre-qual flow.");
Console.WriteLine("  No AML screening, no final decision — just a quick eligibility check.\n");
{
    var preQualPipeline = new Pipeline<LoanContext>()
        .AddStep(new ApplicationValidationStep())
        .AddStep(new CreditScoreStep())
        .AddStep(new DebtToIncomeStep());

    var app = new LoanApplication
    {
        ApplicantName   = "Tom Nguyen",
        Sin             = "789012345",
        RequestedAmount = 400_000m,
        AnnualIncome    = 130_000m,
        MonthlyDebt     =     600m,
        CreditScore     =     720,
        OnWatchlist     = false
    };

    var ctx = preQualPipeline.Run(new LoanContext(app));

    Console.WriteLine();
    foreach (var line in ctx.Log) Console.WriteLine(line);
    Console.WriteLine();
    Console.WriteLine("  Pre-qualification: ELIGIBLE — proceed to full AML and credit review.");
}

Pause();
Console.WriteLine("  Done.");
