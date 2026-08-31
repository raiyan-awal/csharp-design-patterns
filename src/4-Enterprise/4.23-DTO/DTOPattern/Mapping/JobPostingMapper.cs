using DTOPattern.Domain;
using DTOPattern.DTOs;

namespace DTOPattern.Mapping;

public static class JobPostingMapper
{
    public static JobPostingDto ToDto(JobPosting posting) =>
        new(posting.Id,
            posting.Title,
            posting.Company,
            posting.Location,
            posting.SalaryMinCAD,
            posting.SalaryMaxCAD,
            posting.RequiredSkills,
            posting.PostedAt,
            posting.IsActive);

    public static JobPosting ToDomain(CreateJobPostingRequest request) =>
        JobPosting.Create(
            request.Title,
            request.Company,
            request.Location,
            request.SalaryMinCAD,
            request.SalaryMaxCAD,
            request.RequiredSkills,
            request.InternalBudgetCAD);
}
