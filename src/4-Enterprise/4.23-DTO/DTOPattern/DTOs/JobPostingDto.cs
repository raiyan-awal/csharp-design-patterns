namespace DTOPattern.DTOs;

// Response DTO — omits InternalBudgetCAD
public sealed record JobPostingDto(
    Guid                 Id,
    string               Title,
    string               Company,
    string               Location,
    decimal              SalaryMinCAD,
    decimal              SalaryMaxCAD,
    IReadOnlyList<string> RequiredSkills,
    DateTime             PostedAt,
    bool                 IsActive);
