namespace PipelinePattern.Steps;

// Step 5 — classify risk level based on credit score and TDS ratio.
// This step enriches the context and always continues — it never rejects.
// The classification is consumed by the final decision step.
public sealed class RiskClassificationStep : IPipelineStep<LoanContext>
{
    public string Name => "Risk Classification";

    public bool Execute(LoanContext ctx)
    {
        ctx.RiskLevel = (ctx.Application.CreditScore, ctx.DebtToIncomeRatio) switch
        {
            (>= 760, <= 0.30m) => "Low",
            (>= 680, <= 0.38m) => "Medium",
            _                  => "High"
        };

        ctx.Log.Add($"      ✓ Risk level: {ctx.RiskLevel} (score: {ctx.Application.CreditScore}, TDS: {ctx.DebtToIncomeRatio:P1})");
        return true;
    }
}
