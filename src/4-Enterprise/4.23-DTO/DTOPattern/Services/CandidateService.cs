using DTOPattern.Domain;
using DTOPattern.DTOs;
using DTOPattern.Mapping;

namespace DTOPattern.Services;

public sealed class CandidateService
{
    private readonly List<Candidate> _candidates = [];

    public CandidateDto Register(CreateCandidateRequest request)
    {
        var candidate = CandidateMapper.ToDomain(request);
        _candidates.Add(candidate);
        return CandidateMapper.ToDto(candidate);
    }

    public IReadOnlyList<CandidateSummaryDto> ListSummaries() =>
        _candidates.Select(CandidateMapper.ToSummaryDto).ToList();

    public CandidateDto? FindById(Guid id)
    {
        var candidate = _candidates.FirstOrDefault(c => c.Id == id);
        return candidate is null ? null : CandidateMapper.ToDto(candidate);
    }

    // Internal method only — recruiter tool, not exposed via API
    public void RecordAssessment(Guid id, int score, string notes)
    {
        var candidate = _candidates.FirstOrDefault(c => c.Id == id);
        candidate?.RecordInternalAssessment(score, notes);
    }
}
