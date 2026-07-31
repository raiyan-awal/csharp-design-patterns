namespace CQRSPattern;

public sealed record Transaction(
    string   Type,
    decimal  Amount,
    string   Description,
    DateTime Timestamp);
