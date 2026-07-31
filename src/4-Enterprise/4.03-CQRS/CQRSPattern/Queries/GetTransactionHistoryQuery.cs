namespace CQRSPattern;

public sealed record GetTransactionHistoryQuery(string AccountId, int MaxCount = 10);
