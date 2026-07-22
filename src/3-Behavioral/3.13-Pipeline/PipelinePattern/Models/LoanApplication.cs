namespace PipelinePattern;

public sealed class LoanApplication
{
    public string  ApplicantName    { get; init; } = "";
    public string  Sin              { get; init; } = "";   // Social Insurance Number (9 digits)
    public decimal RequestedAmount  { get; init; }
    public decimal AnnualIncome     { get; init; }
    public decimal MonthlyDebt      { get; init; }         // existing monthly debt obligations
    public int     CreditScore      { get; init; }         // Equifax/TransUnion: 300–900
    public bool    OnWatchlist      { get; init; }         // simulates FINTRAC AML result
}
