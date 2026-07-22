namespace PipelinePattern.Steps;

// Step 1 — validate required fields and acceptable loan range.
// Stops the pipeline immediately on any validation failure.
public sealed class ApplicationValidationStep : IPipelineStep<LoanContext>
{
    private const decimal MinAmount =    1_000m;
    private const decimal MaxAmount = 1_500_000m;

    public string Name => "Application Validation";

    public bool Execute(LoanContext ctx)
    {
        var app = ctx.Application;

        if (string.IsNullOrWhiteSpace(app.ApplicantName))
        {
            ctx.Reject("Applicant name is required");
            return false;
        }
        if (app.Sin.Length != 9 || !app.Sin.All(char.IsDigit))
        {
            ctx.Reject("Valid 9-digit SIN is required");
            return false;
        }
        if (app.RequestedAmount < MinAmount || app.RequestedAmount > MaxAmount)
        {
            ctx.Reject($"Loan amount must be between ${MinAmount:N0} and ${MaxAmount:N0}");
            return false;
        }
        if (app.AnnualIncome <= 0)
        {
            ctx.Reject("Annual income must be greater than zero");
            return false;
        }

        ctx.Log.Add($"      ✓ {app.ApplicantName} — ${app.RequestedAmount:N0} requested");
        return true;
    }
}
