using DTOPattern.Domain;
using DTOPattern.DTOs;
using DTOPattern.Mapping;

namespace DTOPattern.Services;

public sealed class JobPostingService
{
    private readonly List<JobPosting> _postings = [];

    public JobPostingDto Create(CreateJobPostingRequest request)
    {
        var posting = JobPostingMapper.ToDomain(request);
        _postings.Add(posting);
        return JobPostingMapper.ToDto(posting);
    }

    public IReadOnlyList<JobPostingDto> ListActive() =>
        _postings.Where(p => p.IsActive).Select(JobPostingMapper.ToDto).ToList();

    public JobPostingDto? FindById(Guid id)
    {
        var posting = _postings.FirstOrDefault(p => p.Id == id);
        return posting is null ? null : JobPostingMapper.ToDto(posting);
    }
}
