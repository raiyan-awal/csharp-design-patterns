namespace PipelinePattern.Steps;

// Step 6 — issue the final approval decision using the context accumulated by prior steps.
// Low/Medium risk: full amount approved.
// High risk: amount reduced by 25% to limit lender exposure.
public sealed class LoanDecisionStep : IPipelineStep<LoanContext>
{
    private const decimal HighRiskReduction = 0.75m;

    public string Name => "Loan Decision";

    public bool Execute(LoanContext ctx)
    {
        var requested = ctx.Application.RequestedAmount;

        if (ctx.RiskLevel == "High")
        {
            // Round to nearest $1,000 for a cleaner offer amount
            var reduced = Math.Round(requested * HighRiskReduction / 1_000m) * 1_000m;
            ctx.Approve($"${reduced:N0} conditionally approved (reduced from ${requested:N0} — high-risk profile)");
        }
        else
        {
            ctx.Approve($"${requested:N0} approved at standard rate ({ctx.RiskLevel.ToLower()}-risk profile)");
        }

        return true;
    }
}
