namespace CQRSPattern;

public sealed record WithdrawCommand(string AccountId, decimal Amount, string Description);
