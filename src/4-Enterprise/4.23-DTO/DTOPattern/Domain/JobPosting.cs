namespace DTOPattern.Domain;

public sealed class JobPosting
{
    public Guid                  Id              { get; private set; } = Guid.NewGuid();
    public string                Title           { get; private set; } = "";
    public string                Company         { get; private set; } = "";
    public string                Location        { get; private set; } = "";
    public decimal               SalaryMinCAD    { get; private set; }
    public decimal               SalaryMaxCAD    { get; private set; }
    public IReadOnlyList<string> RequiredSkills  { get; private set; } = [];
    public DateTime              PostedAt        { get; private set; } = DateTime.UtcNow;
    public bool                  IsActive        { get; private set; } = true;

    // Internal field — never exposed to job seekers via DTO
    public decimal InternalBudgetCAD { get; private set; }

    private JobPosting() { }

    public static JobPosting Create(
        string              title,
        string              company,
        string              location,
        decimal             salaryMinCAD,
        decimal             salaryMaxCAD,
        IEnumerable<string> requiredSkills,
        decimal             internalBudgetCAD) =>
        new()
        {
            Title             = title,
            Company           = company,
            Location          = location,
            SalaryMinCAD      = salaryMinCAD,
            SalaryMaxCAD      = salaryMaxCAD,
            RequiredSkills    = [.. requiredSkills],
            InternalBudgetCAD = internalBudgetCAD,
        };

    public void Close() => IsActive = false;
}
