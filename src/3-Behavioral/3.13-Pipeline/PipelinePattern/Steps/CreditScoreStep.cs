namespace PipelinePattern.Steps;

// Step 2 — verify the credit score meets the minimum threshold.
// Canadian credit scores range from 300 (poor) to 900 (excellent).
// Most lenders require a minimum of 600 for mortgage or personal loan approval.
public sealed class CreditScoreStep : IPipelineStep<LoanContext>
{
    private const int MinimumScore = 600;

    public string Name => "Credit Score Check";

    public bool Execute(LoanContext ctx)
    {
        var score = ctx.Application.CreditScore;

        if (score < MinimumScore)
        {
            ctx.Reject($"Credit score {score} is below the minimum required score of {MinimumScore}");
            return false;
        }

        ctx.Log.Add($"      ✓ Credit score {score} — passes (minimum: {MinimumScore})");
        return true;
    }
}
