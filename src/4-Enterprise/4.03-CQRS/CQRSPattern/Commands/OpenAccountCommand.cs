namespace CQRSPattern;

public sealed record OpenAccountCommand(string AccountId, string OwnerName, decimal InitialDeposit);
