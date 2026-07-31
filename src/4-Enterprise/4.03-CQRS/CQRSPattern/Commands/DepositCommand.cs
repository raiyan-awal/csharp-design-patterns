namespace CQRSPattern;

public sealed record DepositCommand(string AccountId, decimal Amount, string Description);
