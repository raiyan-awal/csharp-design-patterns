using DTOPattern.Domain;
using DTOPattern.DTOs;

namespace DTOPattern.Mapping;

public static class CandidateMapper
{
    public static CandidateDto ToDto(Candidate candidate) =>
        new(candidate.Id,
            candidate.Name,
            candidate.Email,
            candidate.Skills,
            candidate.YearsOfExperience,
            candidate.SalaryExpectationCAD);

    public static CandidateSummaryDto ToSummaryDto(Candidate candidate) =>
        new(candidate.Id,
            candidate.Name,
            candidate.Skills.FirstOrDefault() ?? "(no skills listed)",
            candidate.YearsOfExperience);

    public static Candidate ToDomain(CreateCandidateRequest request) =>
        Candidate.Create(
            request.Name,
            request.Email,
            request.Skills,
            request.YearsOfExperience,
            request.SalaryExpectationCAD);
}
