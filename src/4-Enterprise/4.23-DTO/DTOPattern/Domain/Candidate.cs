namespace DTOPattern.Domain;

public sealed class Candidate
{
    public Guid                  Id                   { get; private set; } = Guid.NewGuid();
    public string                Name                 { get; private set; } = "";
    public string                Email                { get; private set; } = "";
    public IReadOnlyList<string> Skills               { get; private set; } = [];
    public int                   YearsOfExperience    { get; private set; }
    public decimal               SalaryExpectationCAD { get; private set; }

    // Internal fields — never exposed to API callers via DTO
    public int    InternalScore        { get; private set; }
    public string BackgroundCheckNotes { get; private set; } = "";

    private Candidate() { }

    public static Candidate Create(
        string              name,
        string              email,
        IEnumerable<string> skills,
        int                 yearsOfExperience,
        decimal             salaryExpectationCAD) =>
        new()
        {
            Name                 = name,
            Email                = email,
            Skills               = [.. skills],
            YearsOfExperience    = yearsOfExperience,
            SalaryExpectationCAD = salaryExpectationCAD,
        };

    public void RecordInternalAssessment(int score, string notes)
    {
        InternalScore        = score;
        BackgroundCheckNotes = notes;
    }
}
