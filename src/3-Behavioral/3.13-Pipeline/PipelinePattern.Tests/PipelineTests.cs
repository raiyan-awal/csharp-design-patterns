using PipelinePattern;
using PipelinePattern.Steps;

namespace PipelinePattern.Tests;

public class PipelineTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LoanApplication GoodApplication(
        decimal amount      = 300_000m,
        decimal income      = 120_000m,
        decimal monthlyDebt = 400m,
        int     creditScore = 720,
        bool    onWatchlist = false) =>
        new()
        {
            ApplicantName   = "Test Applicant",
            Sin             = "123456789",
            RequestedAmount = amount,
            AnnualIncome    = income,
            MonthlyDebt     = monthlyDebt,
            CreditScore     = creditScore,
            OnWatchlist     = onWatchlist
        };

    private static Pipeline<LoanContext> FullPipeline() =>
        new Pipeline<LoanContext>()
            .AddStep(new ApplicationValidationStep())
            .AddStep(new CreditScoreStep())
            .AddStep(new DebtToIncomeStep())
            .AddStep(new AmlScreeningStep())
            .AddStep(new RiskClassificationStep())
            .AddStep(new LoanDecisionStep());

    // ── Full pipeline — happy path ────────────────────────────────────────────

    [Fact]
    public void FullPipeline_GoodApplication_IsApproved()
    {
        var ctx = FullPipeline().Run(new LoanContext(GoodApplication()));

        Assert.True(ctx.IsApproved);
        Assert.False(ctx.IsRejected);
    }

    [Fact]
    public void FullPipeline_LowRiskProfile_ClassifiedCorrectly()
    {
        var app = GoodApplication(amount: 200_000m, income: 200_000m, monthlyDebt: 100m, creditScore: 800);
        var ctx = FullPipeline().Run(new LoanContext(app));

        Assert.Equal("Low", ctx.RiskLevel);
    }

    [Fact]
    public void FullPipeline_HighRiskProfile_ApprovedAtReducedAmount()
    {
        // Score 620 = below 680 threshold → High risk
        var app = GoodApplication(amount: 200_000m, income: 90_000m, monthlyDebt: 500m, creditScore: 620);
        var ctx = FullPipeline().Run(new LoanContext(app));

        Assert.True(ctx.IsApproved);
        Assert.Equal("High", ctx.RiskLevel);
        Assert.Contains("conditionally approved", ctx.OutcomeNote);
    }

    // ── Step 1: Application Validation ───────────────────────────────────────

    [Fact]
    public void ValidationStep_EmptyName_RejectsAndStops()
    {
        var loan = new LoanApplication
        {
            ApplicantName   = "",
            Sin             = "123456789",
            RequestedAmount = 200_000m,
            AnnualIncome    = 80_000m,
            CreditScore     = 700,
        };
        var step = new ApplicationValidationStep();
        var ctx  = new LoanContext(loan);

        var result = step.Execute(ctx);

        Assert.False(result);
        Assert.True(ctx.IsRejected);
    }

    [Fact]
    public void ValidationStep_ShortSin_RejectsAndStops()
    {
        var loan = new LoanApplication
        {
            ApplicantName   = "Test",
            Sin             = "12345",   // only 5 digits
            RequestedAmount = 200_000m,
            AnnualIncome    = 80_000m,
            CreditScore     = 700,
        };
        var step = new ApplicationValidationStep();
        var ctx  = new LoanContext(loan);

        Assert.False(step.Execute(ctx));
        Assert.True(ctx.IsRejected);
    }

    [Fact]
    public void ValidationStep_AmountBelowMinimum_RejectsAndStops()
    {
        var loan = new LoanApplication
        {
            ApplicantName   = "Test",
            Sin             = "123456789",
            RequestedAmount = 500m,   // below $1,000 minimum
            AnnualIncome    = 80_000m,
            CreditScore     = 700,
        };
        var ctx = new LoanContext(loan);
        Assert.False(new ApplicationValidationStep().Execute(ctx));
        Assert.True(ctx.IsRejected);
    }

    // ── Step 2: Credit Score ──────────────────────────────────────────────────

    [Fact]
    public void CreditScoreStep_ScoreBelowMinimum_RejectsAndStops()
    {
        var ctx = new LoanContext(GoodApplication(creditScore: 550));
        Assert.False(new CreditScoreStep().Execute(ctx));
        Assert.True(ctx.IsRejected);
    }

    [Fact]
    public void CreditScoreStep_ScoreAtMinimum_Continues()
    {
        var ctx = new LoanContext(GoodApplication(creditScore: 600));
        Assert.True(new CreditScoreStep().Execute(ctx));
        Assert.False(ctx.IsRejected);
    }

    // ── Step 3: Debt-to-Income ────────────────────────────────────────────────

    [Fact]
    public void DtiStep_HighDebtLowIncome_RejectsAndStops()
    {
        // $450,000 loan + $2,200/mo existing debt on $60,000 income → TDS > 44%
        var app = GoodApplication(amount: 450_000m, income: 60_000m, monthlyDebt: 2_200m);
        var ctx = new LoanContext(app);

        Assert.False(new DebtToIncomeStep().Execute(ctx));
        Assert.True(ctx.IsRejected);
    }

    [Fact]
    public void DtiStep_AcceptableRatio_SetsDtiAndContinues()
    {
        // $300,000 loan on $120,000 income with $400/mo debt → should pass
        var app = GoodApplication(amount: 300_000m, income: 120_000m, monthlyDebt: 400m);
        var ctx = new LoanContext(app);

        var result = new DebtToIncomeStep().Execute(ctx);

        Assert.True(result);
        Assert.True(ctx.DebtToIncomeRatio > 0);        // ratio was calculated and stored
        Assert.True(ctx.DebtToIncomeRatio <= 0.44m);   // within limit
    }

    // ── Step 4: AML Screening ─────────────────────────────────────────────────

    [Fact]
    public void AmlStep_OnWatchlist_RejectsAndStops()
    {
        var ctx = new LoanContext(GoodApplication(onWatchlist: true));
        Assert.False(new AmlScreeningStep().Execute(ctx));
        Assert.True(ctx.IsRejected);
    }

    [Fact]
    public void AmlStep_NotOnWatchlist_Continues()
    {
        var ctx = new LoanContext(GoodApplication(onWatchlist: false));
        Assert.True(new AmlScreeningStep().Execute(ctx));
        Assert.False(ctx.IsRejected);
    }

    // ── Step 5: Risk Classification ───────────────────────────────────────────

    [Fact]
    public void RiskStep_HighScoreLowDti_ClassifiesLow()
    {
        var ctx = new LoanContext(GoodApplication(creditScore: 800));
        ctx.DebtToIncomeRatio = 0.25m;
        new RiskClassificationStep().Execute(ctx);
        Assert.Equal("Low", ctx.RiskLevel);
    }

    [Fact]
    public void RiskStep_MidScoreMidDti_ClassifiesMedium()
    {
        var ctx = new LoanContext(GoodApplication(creditScore: 700));
        ctx.DebtToIncomeRatio = 0.35m;
        new RiskClassificationStep().Execute(ctx);
        Assert.Equal("Medium", ctx.RiskLevel);
    }

    [Fact]
    public void RiskStep_LowScore_ClassifiesHigh()
    {
        var ctx = new LoanContext(GoodApplication(creditScore: 620));
        ctx.DebtToIncomeRatio = 0.40m;
        new RiskClassificationStep().Execute(ctx);
        Assert.Equal("High", ctx.RiskLevel);
    }

    // ── Pipeline short-circuit ────────────────────────────────────────────────

    [Fact]
    public void Pipeline_RejectsAtStep1_RemainingStepsDoNotRun()
    {
        var loan = new LoanApplication
        {
            ApplicantName   = "",   // will fail validation
            Sin             = "123456789",
            RequestedAmount = 200_000m,
            AnnualIncome    = 80_000m,
            CreditScore     = 700,
        };
        var ctx = FullPipeline().Run(new LoanContext(loan));

        Assert.True(ctx.IsRejected);
        Assert.False(ctx.IsApproved);
        Assert.Equal("Unknown", ctx.RiskLevel);   // risk step never ran
        Assert.Equal(0m, ctx.DebtToIncomeRatio);  // DTI step never ran
    }

    // ── Custom pipeline ───────────────────────────────────────────────────────

    [Fact]
    public void CustomPipeline_SubsetOfSteps_RunsOnlyConfiguredSteps()
    {
        var preQual = new Pipeline<LoanContext>()
            .AddStep(new ApplicationValidationStep())
            .AddStep(new CreditScoreStep());

        // AML flag set — but AML step not in this pipeline, so should not reject
        var ctx = preQual.Run(new LoanContext(GoodApplication(onWatchlist: true)));

        Assert.False(ctx.IsRejected);
        Assert.False(ctx.IsApproved);   // decision step not included either
    }
}
