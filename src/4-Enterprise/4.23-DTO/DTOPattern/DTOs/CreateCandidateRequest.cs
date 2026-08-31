namespace DTOPattern.DTOs;

public sealed record CreateCandidateRequest(
    string               Name,
    string               Email,
    IReadOnlyList<string> Skills,
    int                  YearsOfExperience,
    decimal              SalaryExpectationCAD);
