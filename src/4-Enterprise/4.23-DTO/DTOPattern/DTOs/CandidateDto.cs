namespace DTOPattern.DTOs;

// Full response DTO — omits InternalScore and BackgroundCheckNotes
public sealed record CandidateDto(
    Guid                 Id,
    string               Name,
    string               Email,
    IReadOnlyList<string> Skills,
    int                  YearsOfExperience,
    decimal              SalaryExpectationCAD);
