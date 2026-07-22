namespace PipelinePattern.Steps;

// Step 3 — calculate the Total Debt Service (TDS) ratio and compare against OSFI guidelines.
// TDS = (existing monthly debt + estimated loan payment) / gross monthly income.
// OSFI's maximum TDS for federally regulated lenders in Canada: 44%.
public sealed class DebtToIncomeStep : IPipelineStep<LoanContext>
{
    private const decimal MaxTdsRatio       = 0.44m;
    private const int     AmortizationYears = 25;
    private const decimal AnnualRate        = 0.065m;   // approximate 5-year fixed rate

    public string Name => "Debt-to-Income (TDS) Check";

    public bool Execute(LoanContext ctx)
    {
        var app = ctx.Application;

        // Standard amortization formula for equal monthly payments
        var monthlyRate    = AnnualRate / 12;
        var n              = (double)(AmortizationYears * 12);
        var factor         = Math.Pow(1.0 + (double)monthlyRate, n);
        var monthlyPayment = (decimal)(factor * (double)monthlyRate / (factor - 1.0))
                           * app.RequestedAmount;

        var grossMonthlyIncome = app.AnnualIncome / 12m;
        var totalMonthlyDebt   = app.MonthlyDebt + monthlyPayment;
        var tds                = totalMonthlyDebt / grossMonthlyIncome;

        ctx.DebtToIncomeRatio = Math.Round(tds, 4);

        if (tds > MaxTdsRatio)
        {
            ctx.Reject($"TDS ratio {tds:P1} exceeds OSFI maximum of {MaxTdsRatio:P0}");
            return false;
        }

        ctx.Log.Add($"      ✓ TDS ratio {tds:P1} — within limit ({MaxTdsRatio:P0} max)");
        return true;
    }
}
