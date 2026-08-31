namespace DTOPattern.DTOs;

public sealed record CreateJobPostingRequest(
    string               Title,
    string               Company,
    string               Location,
    decimal              SalaryMinCAD,
    decimal              SalaryMaxCAD,
    IReadOnlyList<string> RequiredSkills,
    decimal              InternalBudgetCAD);
