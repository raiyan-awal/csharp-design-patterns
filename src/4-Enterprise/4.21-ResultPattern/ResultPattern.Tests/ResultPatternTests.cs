using ResultPattern.Core;
using ResultPattern.Domain;
using ResultPattern.Services;

namespace ResultPattern.Tests;

// ── Suite 1: Result<T> creation ───────────────────────────────────────────────

public sealed class ResultCreationTests
{
    [Fact]
    public void Success_IsSuccess_IsTrue()
    {
        var result = Result<int>.Success(42);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Success_Value_IsSet()
    {
        var result = Result<string>.Success("hello");
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Failure_IsSuccess_IsFalse()
    {
        var result = Result<int>.Failure("something went wrong");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Failure_Error_IsSet()
    {
        var result = Result<int>.Failure("bad input");
        Assert.Equal("bad input", result.Error);
    }
}

// ── Suite 2: Map ──────────────────────────────────────────────────────────────

public sealed class ResultMapTests
{
    [Fact]
    public void Map_TransformsValue_OnSuccess()
    {
        var result = Result<int>.Success(5).Map(x => x * 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Map_PropagatesFailure_WithoutCallingMapper()
    {
        var called = false;
        var result = Result<int>.Failure("original error").Map(x => { called = true; return x * 2; });

        Assert.False(result.IsSuccess);
        Assert.Equal("original error", result.Error);
        Assert.False(called);
    }

    [Fact]
    public void Map_CanChangeType()
    {
        var result = Result<int>.Success(42).Map(x => x.ToString());

        Assert.IsType<Result<string>>(result);
        Assert.Equal("42", result.Value);
    }
}

// ── Suite 3: Bind ─────────────────────────────────────────────────────────────

public sealed class ResultBindTests
{
    private static Result<int> Double(int x) => Result<int>.Success(x * 2);
    private static Result<int> Fail(int _)   => Result<int>.Failure("step failed");

    [Fact]
    public void Bind_ChainsSuccessfulOperations()
    {
        var result = Result<int>.Success(3).Bind(Double).Bind(Double);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value);
    }

    [Fact]
    public void Bind_StopsAtFirstFailure()
    {
        var secondCalled = false;
        var result = Result<int>.Success(3)
            .Bind(Fail)
            .Bind(x => { secondCalled = true; return Double(x); });

        Assert.False(result.IsSuccess);
        Assert.False(secondCalled);
    }

    [Fact]
    public void Bind_PropagatesFailure_WithoutCallingNext()
    {
        var called = false;
        Result<int>.Failure("early error").Bind(x => { called = true; return Double(x); });

        Assert.False(called);
    }

    [Fact]
    public void Bind_FailureMessage_ComesFromFailingStep()
    {
        var result = Result<int>.Success(1).Bind(Fail);

        Assert.Equal("step failed", result.Error);
    }
}

// ── Suite 4: Match ────────────────────────────────────────────────────────────

public sealed class ResultMatchTests
{
    [Fact]
    public void Match_CallsOnSuccess_WithValue()
    {
        var result = Result<int>.Success(99);

        var output = result.Match(
            onSuccess: v    => $"value:{v}",
            onFailure: err  => $"error:{err}");

        Assert.Equal("value:99", output);
    }

    [Fact]
    public void Match_CallsOnFailure_WithError()
    {
        var result = Result<int>.Failure("oops");

        var output = result.Match(
            onSuccess: _   => "success",
            onFailure: err => $"error:{err}");

        Assert.Equal("error:oops", output);
    }
}

// ── Suite 5: OnSuccess / OnFailure ────────────────────────────────────────────

public sealed class ResultObservabilityTests
{
    [Fact]
    public void OnSuccess_InvokesCallback_OnSuccess()
    {
        var received = -1;
        Result<int>.Success(7).OnSuccess(v => received = v);

        Assert.Equal(7, received);
    }

    [Fact]
    public void OnFailure_InvokesCallback_OnFailure()
    {
        var received = "";
        Result<int>.Failure("bad").OnFailure(err => received = err);

        Assert.Equal("bad", received);
    }

    [Fact]
    public void OnSuccess_ReturnsSameResult_ForChaining()
    {
        var result = Result<int>.Success(3);
        var returned = result.OnSuccess(_ => { });

        Assert.Same(result, returned);
    }
}

// ── Suite 6: LoanApplicationService ──────────────────────────────────────────

public sealed class LoanApplicationServiceTests
{
    private static readonly LoanApplicationService Service = new();

    private static LoanApplication ValidApp(
        decimal income  = 80_000m,
        int     credit  = 720,
        decimal debt    = 500m,
        decimal amount  = 20_000m,
        int     term    = 60) =>
        new("CUST-001", "Alice Tremblay", income, credit, debt, amount, term);

    [Fact]
    public void ValidApplication_ReturnsSuccess()
    {
        var result = Service.Evaluate(ValidApp());
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void LowIncome_ReturnsFailure_WithIncomeMessage()
    {
        var result = Service.Evaluate(ValidApp(income: 20_000m));

        Assert.False(result.IsSuccess);
        Assert.Contains("income", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LowCreditScore_ReturnsFailure_WithCreditMessage()
    {
        var result = Service.Evaluate(ValidApp(credit: 600));

        Assert.False(result.IsSuccess);
        Assert.Contains("credit", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HighDebtRatio_ReturnsFailure_WithDebtMessage()
    {
        // $4,000/month debt on $80,000/year income = 60% DTI
        var result = Service.Evaluate(ValidApp(debt: 4_000m));

        Assert.False(result.IsSuccess);
        Assert.Contains("debt", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Approval_MonthlyPayment_IsPositive()
    {
        var result = Service.Evaluate(ValidApp());

        Assert.True(result.Value!.MonthlyPaymentCAD > 0);
    }

    [Fact]
    public void Approval_ApplicationRef_IsNotEmpty()
    {
        var result = Service.Evaluate(ValidApp());

        Assert.NotEmpty(result.Value!.ApplicationRef);
    }

    [Fact]
    public void HigherCreditScore_GetsLowerInterestRate()
    {
        var excellent = Service.Evaluate(ValidApp(credit: 780)).Value!.AnnualInterestRate;
        var good      = Service.Evaluate(ValidApp(credit: 710)).Value!.AnnualInterestRate;

        Assert.True(excellent < good);
    }

    [Fact]
    public void IncomeFailure_ShortCircuits_BeforeCreditCheck()
    {
        // Both income and credit fail; only the income error should appear.
        var result = Service.Evaluate(ValidApp(income: 10_000m, credit: 400));

        Assert.Contains("income", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
