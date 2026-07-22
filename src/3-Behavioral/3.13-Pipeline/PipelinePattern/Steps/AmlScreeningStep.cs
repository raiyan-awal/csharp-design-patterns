namespace PipelinePattern.Steps;

// Step 4 — Anti-Money Laundering (AML) screening via FINTRAC watchlist.
// In production this calls a third-party identity screening service.
// The OnWatchlist flag on the application simulates that external check.
public sealed class AmlScreeningStep : IPipelineStep<LoanContext>
{
    public string Name => "AML Screening";

    public bool Execute(LoanContext ctx)
    {
        if (ctx.Application.OnWatchlist)
        {
            ctx.Reject("Applicant matched FINTRAC watchlist — referred to compliance team");
            return false;
        }

        ctx.Log.Add("      ✓ AML screening passed — no watchlist matches found");
        return true;
    }
}
