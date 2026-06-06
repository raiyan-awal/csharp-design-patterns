namespace FacadePattern;

// ============================================================
// SUBSYSTEM: Audit
// ============================================================
// Records a tamper-evident log of every significant action.
// Has no knowledge of any other subsystem.

public sealed class AuditLogger
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public void Log(string action, string details)
    {
        string entry = $"[{DateTime.UtcNow:HH:mm:ss.fff}] {action}: {details}";
        _entries.Add(entry);
        Console.WriteLine($"[AUDIT] {entry}");
    }
}
