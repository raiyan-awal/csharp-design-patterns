using ResultPattern.Core;
using ResultPattern.Domain;
using ResultPattern.Services;

Console.WriteLine("=== Maple Bank — Result Pattern Demo ===\n");

var service = new LoanApplicationService();

static void PrintResult(Result<LoanApproval> result, string applicant)
{
    result.Match(
        onSuccess: approval =>
        {
            Console.WriteLine($"  ✓ Approved — {applicant}");
            Console.WriteLine($"    Ref        : {approval.ApplicationRef}");
            Console.WriteLine($"    Amount     : ${approval.ApprovedAmountCAD:N2} CAD");
            Console.WriteLine($"    Rate       : {approval.AnnualInterestRate:P2}");
            Console.WriteLine($"    Term       : {approval.TermMonths} months");
            Console.WriteLine($"    Monthly    : ${approval.MonthlyPaymentCAD:N2} CAD");
            return true;
        },
        onFailure: error =>
        {
            Console.WriteLine($"  ✗ Declined — {applicant}");
            Console.WriteLine($"    Reason: {error}");
            return false;
        });
}

// ── Section 1: Approved Application ──────────────────────────────────────────
Console.WriteLine("--- Section 1: Approved Application ---");

var app1 = new LoanApplication(
    ApplicantId:           "CUST-001",
    ApplicantName:         "Alice Tremblay",
    AnnualIncomeCAD:       82_000m,
    CreditScore:           762,
    MonthlyDebtPaymentsCAD: 620m,
    RequestedAmountCAD:    25_000m,
    TermMonths:            60);

PrintResult(service.Evaluate(app1), app1.ApplicantName);

Pause();

// ── Section 2: Declined — Income Too Low ──────────────────────────────────────
Console.WriteLine("--- Section 2: Declined — Income Below Minimum ---");

var app2 = new LoanApplication(
    ApplicantId:           "CUST-002",
    ApplicantName:         "Ben Kowalczyk",
    AnnualIncomeCAD:       28_500m,
    CreditScore:           710,
    MonthlyDebtPaymentsCAD: 300m,
    RequestedAmountCAD:    15_000m,
    TermMonths:            48);

PrintResult(service.Evaluate(app2), app2.ApplicantName);

Pause();

// ── Section 3: Declined — Credit Score Too Low ────────────────────────────────
Console.WriteLine("--- Section 3: Declined — Credit Score Below Minimum ---");

var app3 = new LoanApplication(
    ApplicantId:           "CUST-003",
    ApplicantName:         "Sophie Bouchard",
    AnnualIncomeCAD:       65_000m,
    CreditScore:           603,
    MonthlyDebtPaymentsCAD: 400m,
    RequestedAmountCAD:    20_000m,
    TermMonths:            36);

PrintResult(service.Evaluate(app3), app3.ApplicantName);

Pause();

// ── Section 4: Declined — Debt Ratio Too High ────────────────────────────────
Console.WriteLine("--- Section 4: Declined — Debt-to-Income Ratio Exceeded ---");

var app4 = new LoanApplication(
    ApplicantId:           "CUST-004",
    ApplicantName:         "Marcus Osei",
    AnnualIncomeCAD:       55_000m,
    CreditScore:           690,
    MonthlyDebtPaymentsCAD: 2_200m,   // ~48% DTI
    RequestedAmountCAD:    30_000m,
    TermMonths:            60);

PrintResult(service.Evaluate(app4), app4.ApplicantName);

Pause();

// ── Section 5: Map and OnSuccess / OnFailure Callbacks ───────────────────────
Console.WriteLine("--- Section 5: Map, OnSuccess, and OnFailure ---");

// Map transforms an approved loan into a summary string without leaving the Result railway.
service.Evaluate(app1)
    .Map(approval => $"Approval {approval.ApplicationRef}: ${approval.ApprovedAmountCAD:N0} over {approval.TermMonths}mo at {approval.AnnualInterestRate:P2}")
    .OnSuccess(summary => Console.WriteLine($"  Summary  : {summary}"))
    .OnFailure(error   => Console.WriteLine($"  Error    : {error}"));

// OnFailure fires only on the declined result.
service.Evaluate(app3)
    .OnSuccess(_ => Console.WriteLine("  Approved (unexpected)"))
    .OnFailure(error => Console.WriteLine($"  Logged   : {error}"));

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
