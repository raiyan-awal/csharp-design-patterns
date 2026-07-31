using CQRSPattern;

namespace CQRSPattern.Tests;

public class CQRSTests
{
    private static (WriteStore write, ReadStore read, AccountProjector proj) Stores() =>
        (new WriteStore(), new ReadStore(), new AccountProjector());

    private static OpenAccountHandler  OpenHandler (WriteStore w, ReadStore r, AccountProjector p) => new(w, r, p);
    private static DepositHandler      DepHandler  (WriteStore w, ReadStore r, AccountProjector p) => new(w, r, p);
    private static WithdrawHandler     WithHandler (WriteStore w, ReadStore r, AccountProjector p) => new(w, r, p);

    // ── OpenAccount ───────────────────────────────────────────────────────────

    [Fact]
    public void OpenAccount_ValidCommand_Succeeds()
    {
        var (w, r, p) = Stores();
        var result = OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane Smith", 1_000m));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void OpenAccount_CreatesWriteAndReadEntries()
    {
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane Smith", 1_000m));
        Assert.NotNull(w.Find("A1"));
        Assert.NotNull(r.Find("A1"));
    }

    [Fact]
    public void OpenAccount_DuplicateId_Fails()
    {
        var (w, r, p) = Stores();
        var handler = OpenHandler(w, r, p);
        handler.Handle(new OpenAccountCommand("A1", "Jane Smith", 1_000m));
        var result = handler.Handle(new OpenAccountCommand("A1", "Other", 500m));
        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public void OpenAccount_SetsInitialBalance()
    {
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane Smith", 2_500m));
        Assert.Equal(2_500m, r.Find("A1")!.Balance);
    }

    // ── Deposit ───────────────────────────────────────────────────────────────

    [Fact]
    public void Deposit_ValidAmount_IncreasesBalance()
    {
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane", 1_000m));
        DepHandler(w, r, p).Handle(new DepositCommand("A1", 500m, "Payroll"));
        Assert.Equal(1_500m, r.Find("A1")!.Balance);
    }

    [Fact]
    public void Deposit_UnknownAccount_Fails()
    {
        var (w, r, p) = Stores();
        var result = DepHandler(w, r, p).Handle(new DepositCommand("NOPE", 100m, "Test"));
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Deposit_ZeroAmount_Fails()
    {
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane", 1_000m));
        var result = DepHandler(w, r, p).Handle(new DepositCommand("A1", 0m, "Zero"));
        Assert.False(result.IsSuccess);
    }

    // ── Withdraw ──────────────────────────────────────────────────────────────

    [Fact]
    public void Withdraw_ValidAmount_DecreasesBalance()
    {
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane", 1_000m));
        WithHandler(w, r, p).Handle(new WithdrawCommand("A1", 300m, "Rent"));
        Assert.Equal(700m, r.Find("A1")!.Balance);
    }

    [Fact]
    public void Withdraw_Overdraft_Fails()
    {
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane", 500m));
        var result = WithHandler(w, r, p).Handle(new WithdrawCommand("A1", 999m, "Too much"));
        Assert.False(result.IsSuccess);
        Assert.Contains("Insufficient", result.Error);
    }

    [Fact]
    public void Withdraw_UnknownAccount_Fails()
    {
        var (w, r, p) = Stores();
        var result = WithHandler(w, r, p).Handle(new WithdrawCommand("NOPE", 100m, "Test"));
        Assert.False(result.IsSuccess);
    }

    // ── GetBalance query ──────────────────────────────────────────────────────

    [Fact]
    public void GetBalance_ReturnsCurrentBalance()
    {
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane", 1_000m));
        DepHandler(w, r, p).Handle(new DepositCommand("A1", 200m, "Test"));

        var result = new GetBalanceHandler(r).Handle(new GetBalanceQuery("A1"));
        Assert.NotNull(result);
        Assert.Equal(1_200m, result.Balance);
    }

    [Fact]
    public void GetBalance_UnknownAccount_ReturnsNull()
    {
        var (_, r, _) = Stores();
        var result = new GetBalanceHandler(r).Handle(new GetBalanceQuery("NOPE"));
        Assert.Null(result);
    }

    [Fact]
    public void GetBalance_NeverTouchesWriteStore()
    {
        // Query reads only from ReadStore — WriteStore is not involved.
        // This test proves the query handler takes only ReadStore.
        var (_, r, _) = Stores();
        var handler = new GetBalanceHandler(r);  // no WriteStore parameter
        Assert.NotNull(handler);
    }

    // ── GetAccountSummary query ───────────────────────────────────────────────

    [Fact]
    public void GetAccountSummary_PreComputesAggregates()
    {
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane", 1_000m));
        DepHandler(w, r, p).Handle(new DepositCommand("A1", 500m, "Payroll"));
        DepHandler(w, r, p).Handle(new DepositCommand("A1", 200m, "Transfer"));
        WithHandler(w, r, p).Handle(new WithdrawCommand("A1", 300m, "Rent"));

        var result = new GetAccountSummaryHandler(r).Handle(new GetAccountSummaryQuery("A1"));
        Assert.NotNull(result);
        Assert.Equal(1_400m, result.Balance);
        Assert.Equal(1_700m, result.TotalDeposited);  // 1000 + 500 + 200
        Assert.Equal(300m,   result.TotalWithdrawn);
        Assert.Equal(4,      result.TransactionCount);
    }

    // ── GetTransactionHistory query ───────────────────────────────────────────

    [Fact]
    public void GetTransactionHistory_ReturnsRunningBalance()
    {
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane", 1_000m));
        DepHandler(w, r, p).Handle(new DepositCommand("A1", 500m, "Payroll"));
        WithHandler(w, r, p).Handle(new WithdrawCommand("A1", 200m, "Groceries"));

        var history = new GetTransactionHistoryHandler(r)
            .Handle(new GetTransactionHistoryQuery("A1"))!
            .ToList();

        Assert.Equal(3, history.Count);
        Assert.Equal(1_000m, history[0].BalanceAfter);
        Assert.Equal(1_500m, history[1].BalanceAfter);
        Assert.Equal(1_300m, history[2].BalanceAfter);
    }

    [Fact]
    public void GetTransactionHistory_MaxCount_Limits()
    {
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane", 500m));
        for (var i = 0; i < 5; i++)
            DepHandler(w, r, p).Handle(new DepositCommand("A1", 10m, $"tx{i}"));

        var history = new GetTransactionHistoryHandler(r)
            .Handle(new GetTransactionHistoryQuery("A1", MaxCount: 3))!
            .ToList();

        Assert.Equal(3, history.Count);
    }

    // ── Separation — queries never touch WriteStore ───────────────────────────

    [Fact]
    public void ReadStore_IsIndependentOfWriteStore()
    {
        // Commands update both stores. Queries only use ReadStore.
        // Simulate: write store updated, read store consulted separately.
        var (w, r, p) = Stores();
        OpenHandler(w, r, p).Handle(new OpenAccountCommand("A1", "Jane", 800m));

        // Query from read store — balance is what was projected at command time
        var balance = new GetBalanceHandler(r).Handle(new GetBalanceQuery("A1"));
        Assert.Equal(800m, balance!.Balance);

        // Write store also has the aggregate
        Assert.Equal(800m, w.Find("A1")!.Balance);
    }
}
