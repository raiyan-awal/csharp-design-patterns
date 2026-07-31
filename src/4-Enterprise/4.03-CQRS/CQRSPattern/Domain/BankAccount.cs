namespace CQRSPattern;

// Write-side aggregate — the authoritative source of truth.
// Contains only what is needed to enforce invariants and record facts.
// Queries never touch this object; they read from AccountView instead.
public sealed class BankAccount
{
    private readonly List<Transaction> _transactions = [];

    public string  AccountId  { get; }
    public string  OwnerName  { get; }
    public decimal Balance    { get; private set; }

    public IReadOnlyList<Transaction> Transactions => _transactions;

    public BankAccount(string accountId, string ownerName, decimal initialDeposit)
    {
        if (initialDeposit < 0)
            throw new ArgumentOutOfRangeException(nameof(initialDeposit), "Initial deposit cannot be negative.");

        AccountId = accountId;
        OwnerName = ownerName;
        Balance   = initialDeposit;
        _transactions.Add(new Transaction("OPEN", initialDeposit, "Account opened", DateTime.UtcNow));
    }

    public void Deposit(decimal amount, string description)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive.");

        Balance += amount;
        _transactions.Add(new Transaction("DEPOSIT", amount, description, DateTime.UtcNow));
    }

    public void Withdraw(decimal amount, string description)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be positive.");
        if (amount > Balance)
            throw new InvalidOperationException($"Insufficient funds. Balance: ${Balance:F2}, Requested: ${amount:F2}");

        Balance -= amount;
        _transactions.Add(new Transaction("WITHDRAWAL", amount, description, DateTime.UtcNow));
    }
}
