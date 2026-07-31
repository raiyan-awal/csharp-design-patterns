namespace CQRSPattern;

// Read-side transaction — adds BalanceAfter which is pre-computed at projection time.
// The write model stores only the transaction itself; the running balance is derived here.
public sealed record TransactionView(
    string   Type,
    decimal  Amount,
    string   Description,
    DateTime Timestamp,
    decimal  BalanceAfter);
