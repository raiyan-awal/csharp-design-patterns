namespace DTOPattern.DTOs;

// Lightweight DTO for list views — only the fields a recruiter needs at a glance
public sealed record CandidateSummaryDto(
    Guid   Id,
    string Name,
    string TopSkill,
    int    YearsOfExperience);
