using ResultPattern.Core;
using ResultPattern.Domain;

namespace ResultPattern.Services;

public sealed class LoanApplicationService
{
    private const decimal MinAnnualIncomeCAD = 35_000m;
    private const int     MinCreditScore     = 650;
    private const decimal MaxDebtToIncome    = 0.43m;

    // Railway-oriented pipeline: each step passes the application forward on success
    // or short-circuits with a failure message.
    public Result<LoanApproval> Evaluate(LoanApplication app) =>
        ValidateIncome(app)
            .Bind(ValidateCreditScore)
            .Bind(ValidateDebtRatio)
            .Bind(CalculateApproval);

    private static Result<LoanApplication> ValidateIncome(LoanApplication app) =>
        app.AnnualIncomeCAD >= MinAnnualIncomeCAD
            ? Result<LoanApplication>.Success(app)
            : Result<LoanApplication>.Failure(
                $"Annual income ${app.AnnualIncomeCAD:N0} CAD is below the ${MinAnnualIncomeCAD:N0} CAD minimum.");

    private static Result<LoanApplication> ValidateCreditScore(LoanApplication app) =>
        app.CreditScore >= MinCreditScore
            ? Result<LoanApplication>.Success(app)
            : Result<LoanApplication>.Failure(
                $"Credit score {app.CreditScore} is below the minimum score of {MinCreditScore} required for approval.");

    private static Result<LoanApplication> ValidateDebtRatio(LoanApplication app)
    {
        var monthlyIncome = app.AnnualIncomeCAD / 12m;
        var ratio         = app.MonthlyDebtPaymentsCAD / monthlyIncome;
        return ratio <= MaxDebtToIncome
            ? Result<LoanApplication>.Success(app)
            : Result<LoanApplication>.Failure(
                $"Debt-to-income ratio of {ratio:P0} exceeds the maximum {MaxDebtToIncome:P0} allowed.");
    }

    private static Result<LoanApproval> CalculateApproval(LoanApplication app)
    {
        var rate = app.CreditScore switch
        {
            >= 750 => 0.0549m,
            >= 700 => 0.0649m,
            _      => 0.0749m,
        };

        var monthlyRate    = rate / 12m;
        var n              = (double)app.TermMonths;
        var r              = (double)monthlyRate;
        var monthlyPayment = (decimal)(
            (double)app.RequestedAmountCAD * r * Math.Pow(1 + r, n) / (Math.Pow(1 + r, n) - 1));

        return Result<LoanApproval>.Success(new LoanApproval(
            ApplicationRef:    Guid.NewGuid().ToString()[..8].ToUpper(),
            ApplicantName:     app.ApplicantName,
            ApprovedAmountCAD: app.RequestedAmountCAD,
            MonthlyPaymentCAD: Math.Round(monthlyPayment, 2),
            AnnualInterestRate: rate,
            TermMonths:        app.TermMonths));
    }
}
