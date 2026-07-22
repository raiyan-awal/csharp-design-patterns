namespace PipelinePattern;

// The shared context that flows through every pipeline step.
// Steps read the Application data and write their findings back here.
// The final state reflects the accumulated result of all steps that ran.
public sealed class LoanContext
{
    public LoanApplication Application { get; }
    public List<string>    Log         { get; } = [];

    // Data enriched by individual steps
    public decimal DebtToIncomeRatio { get; set; }
    public string  RiskLevel         { get; set; } = "Unknown";

    // Outcome set by the final decision step
    public bool    IsApproved        { get; private set; }
    public bool    IsRejected        { get; private set; }
    public string? OutcomeNote       { get; private set; }

    public LoanContext(LoanApplication application) => Application = application;

    public void Reject(string reason)
    {
        IsRejected  = true;
        OutcomeNote = reason;
        Log.Add($"      ✗ REJECTED — {reason}");
    }

    public void Approve(string note)
    {
        IsApproved  = true;
        OutcomeNote = note;
        Log.Add($"      ✓ APPROVED — {note}");
    }
}
