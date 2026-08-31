using DTOPattern.Domain;
using DTOPattern.DTOs;
using DTOPattern.Mapping;
using DTOPattern.Services;

namespace DTOPattern.Tests;

// ── Suite 1: CandidateMapper ──────────────────────────────────────────────────

public sealed class CandidateMapperTests
{
    private static Candidate MakeCandidate(
        string name  = "Sophie Tremblay",
        string email = "sophie@example.com",
        int    years = 6,
        decimal salary = 115_000m,
        string[]? skills = null)
    {
        var candidate = Candidate.Create(name, email, skills ?? ["C#", ".NET"], years, salary);
        return candidate;
    }

    [Fact]
    public void ToDto_MapsName()
    {
        var dto = CandidateMapper.ToDto(MakeCandidate(name: "Alice Chen"));
        Assert.Equal("Alice Chen", dto.Name);
    }

    [Fact]
    public void ToDto_MapsEmail()
    {
        var dto = CandidateMapper.ToDto(MakeCandidate(email: "alice@maple.ca"));
        Assert.Equal("alice@maple.ca", dto.Email);
    }

    [Fact]
    public void ToDto_MapsSkills()
    {
        var dto = CandidateMapper.ToDto(MakeCandidate(skills: ["Python", "ML"]));
        Assert.Equal(["Python", "ML"], dto.Skills);
    }

    [Fact]
    public void ToDto_MapsYearsOfExperience()
    {
        var dto = CandidateMapper.ToDto(MakeCandidate(years: 8));
        Assert.Equal(8, dto.YearsOfExperience);
    }

    [Fact]
    public void ToDto_MapsSalaryExpectation()
    {
        var dto = CandidateMapper.ToDto(MakeCandidate(salary: 140_000m));
        Assert.Equal(140_000m, dto.SalaryExpectationCAD);
    }

    [Fact]
    public void ToDto_DoesNotExposeInternalScore()
    {
        var candidate = MakeCandidate();
        candidate.RecordInternalAssessment(score: 90, notes: "Excellent.");

        var dto     = CandidateMapper.ToDto(candidate);
        var dtoType = typeof(CandidateDto);

        // CandidateDto must not have InternalScore or BackgroundCheckNotes properties
        Assert.Null(dtoType.GetProperty("InternalScore"));
        Assert.Null(dtoType.GetProperty("BackgroundCheckNotes"));
    }

    [Fact]
    public void ToSummaryDto_MapsName()
    {
        var dto = CandidateMapper.ToSummaryDto(MakeCandidate(name: "Marcus Osei"));
        Assert.Equal("Marcus Osei", dto.Name);
    }

    [Fact]
    public void ToSummaryDto_TopSkill_IsFirstSkill()
    {
        var dto = CandidateMapper.ToSummaryDto(MakeCandidate(skills: ["TypeScript", "React"]));
        Assert.Equal("TypeScript", dto.TopSkill);
    }

    [Fact]
    public void ToSummaryDto_TopSkill_FallsBackWhenNoSkills()
    {
        var dto = CandidateMapper.ToSummaryDto(MakeCandidate(skills: []));
        Assert.Equal("(no skills listed)", dto.TopSkill);
    }

    [Fact]
    public void ToSummaryDto_DoesNotExposeEmail()
    {
        var dtoType = typeof(CandidateSummaryDto);
        Assert.Null(dtoType.GetProperty("Email"));
    }

    [Fact]
    public void ToDomain_CreatesCandidateWithCorrectName()
    {
        var request   = new CreateCandidateRequest("Sophie Tremblay", "s@t.ca", ["C#"], 6, 115_000m);
        var candidate = CandidateMapper.ToDomain(request);
        Assert.Equal("Sophie Tremblay", candidate.Name);
    }

    [Fact]
    public void ToDomain_CreatesCandidateWithCorrectSkills()
    {
        var request   = new CreateCandidateRequest("Alice", "a@b.ca", ["Go", "Rust"], 5, 100_000m);
        var candidate = CandidateMapper.ToDomain(request);
        Assert.Equal(["Go", "Rust"], candidate.Skills);
    }
}

// ── Suite 2: JobPostingMapper ─────────────────────────────────────────────────

public sealed class JobPostingMapperTests
{
    private static JobPosting MakePosting(
        string  title    = "Senior Developer",
        string  company  = "Maple Systems Inc.",
        string  location = "Toronto, ON",
        decimal salMin   = 100_000m,
        decimal salMax   = 130_000m,
        decimal budget   = 145_000m,
        string[]? skills  = null) =>
        JobPosting.Create(title, company, location, salMin, salMax, skills ?? ["C#"], budget);

    [Fact]
    public void ToDto_MapsTitle()
    {
        var dto = JobPostingMapper.ToDto(MakePosting(title: "ML Engineer"));
        Assert.Equal("ML Engineer", dto.Title);
    }

    [Fact]
    public void ToDto_MapsCompany()
    {
        var dto = JobPostingMapper.ToDto(MakePosting(company: "Northern Digital"));
        Assert.Equal("Northern Digital", dto.Company);
    }

    [Fact]
    public void ToDto_MapsSalaryRange()
    {
        var dto = JobPostingMapper.ToDto(MakePosting(salMin: 120_000m, salMax: 155_000m));
        Assert.Equal(120_000m, dto.SalaryMinCAD);
        Assert.Equal(155_000m, dto.SalaryMaxCAD);
    }

    [Fact]
    public void ToDto_DoesNotExposeInternalBudget()
    {
        var dtoType = typeof(JobPostingDto);
        Assert.Null(dtoType.GetProperty("InternalBudgetCAD"));
    }

    [Fact]
    public void ToDto_IsActive_IsTrue_ByDefault()
    {
        var dto = JobPostingMapper.ToDto(MakePosting());
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void ToDomain_CreatePostingWithCorrectTitle()
    {
        var request = new CreateJobPostingRequest(
            "Dev", "Co", "Toronto, ON", 90_000m, 110_000m, ["C#"], 120_000m);
        var posting = JobPostingMapper.ToDomain(request);
        Assert.Equal("Dev", posting.Title);
    }

    [Fact]
    public void ToDomain_InternalBudget_StoredOnDomain_NotInDto()
    {
        var request = new CreateJobPostingRequest(
            "Dev", "Co", "Toronto, ON", 90_000m, 110_000m, ["C#"], 145_000m);
        var posting = JobPostingMapper.ToDomain(request);
        var dto     = JobPostingMapper.ToDto(posting);

        Assert.Equal(145_000m, posting.InternalBudgetCAD);  // present on domain
        Assert.Null(typeof(JobPostingDto).GetProperty("InternalBudgetCAD")); // absent from DTO
    }
}

// ── Suite 3: CandidateService ─────────────────────────────────────────────────

public sealed class CandidateServiceTests
{
    private static CreateCandidateRequest Request(
        string   name   = "Sophie Tremblay",
        string   email  = "sophie@example.com",
        int      years  = 6,
        decimal  salary = 115_000m,
        string[]? skills = null) =>
        new(name, email, skills ?? ["C#", ".NET"], years, salary);

    [Fact]
    public void Register_ReturnsCandidateDto_WithCorrectName()
    {
        var service = new CandidateService();
        var dto     = service.Register(Request(name: "Marcus Osei"));
        Assert.Equal("Marcus Osei", dto.Name);
    }

    [Fact]
    public void Register_AssignsNonEmptyId()
    {
        var service = new CandidateService();
        var dto     = service.Register(Request());
        Assert.NotEqual(Guid.Empty, dto.Id);
    }

    [Fact]
    public void ListSummaries_ReturnsOneEntry_AfterOneRegister()
    {
        var service = new CandidateService();
        service.Register(Request());
        Assert.Single(service.ListSummaries());
    }

    [Fact]
    public void ListSummaries_ReturnsSummaryDto_NotFullDto()
    {
        var service  = new CandidateService();
        service.Register(Request());
        var summaries = service.ListSummaries();
        Assert.IsType<CandidateSummaryDto>(summaries[0]);
    }

    [Fact]
    public void FindById_ReturnsDto_WhenFound()
    {
        var service = new CandidateService();
        var dto     = service.Register(Request(name: "Alice Chen"));
        var found   = service.FindById(dto.Id);
        Assert.NotNull(found);
        Assert.Equal("Alice Chen", found.Name);
    }

    [Fact]
    public void FindById_ReturnsNull_WhenNotFound()
    {
        var service = new CandidateService();
        Assert.Null(service.FindById(Guid.NewGuid()));
    }

    [Fact]
    public void RecordAssessment_DoesNotAffectDto()
    {
        var service = new CandidateService();
        var dto     = service.Register(Request());
        service.RecordAssessment(dto.Id, score: 92, notes: "Outstanding.");

        var found = service.FindById(dto.Id);
        Assert.NotNull(found);
        // CandidateDto has no InternalScore — assert the type doesn't have the property
        Assert.Null(typeof(CandidateDto).GetProperty("InternalScore"));
    }
}

// ── Suite 4: JobPostingService ────────────────────────────────────────────────

public sealed class JobPostingServiceTests
{
    private static CreateJobPostingRequest Request(
        string   title    = "Senior Developer",
        string   company  = "Maple Systems Inc.",
        string   location = "Toronto, ON",
        decimal  salMin   = 100_000m,
        decimal  salMax   = 130_000m,
        decimal  budget   = 145_000m,
        string[]? skills   = null) =>
        new(title, company, location, salMin, salMax, skills ?? ["C#"], budget);

    [Fact]
    public void Create_ReturnsJobPostingDto_WithCorrectTitle()
    {
        var service = new JobPostingService();
        var dto     = service.Create(Request(title: "ML Engineer"));
        Assert.Equal("ML Engineer", dto.Title);
    }

    [Fact]
    public void Create_IsActive_ByDefault()
    {
        var service = new JobPostingService();
        var dto     = service.Create(Request());
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void ListActive_ReturnsPosting_AfterCreate()
    {
        var service = new JobPostingService();
        service.Create(Request());
        Assert.Single(service.ListActive());
    }

    [Fact]
    public void Create_InternalBudget_NotExposedInDto()
    {
        var service = new JobPostingService();
        service.Create(Request(budget: 175_000m));
        var active = service.ListActive();
        Assert.Null(typeof(JobPostingDto).GetProperty("InternalBudgetCAD"));
    }

    [Fact]
    public void FindById_ReturnsDto_WhenFound()
    {
        var service = new JobPostingService();
        var dto     = service.Create(Request(title: "Data Engineer"));
        var found   = service.FindById(dto.Id);
        Assert.NotNull(found);
        Assert.Equal("Data Engineer", found.Title);
    }

    [Fact]
    public void FindById_ReturnsNull_WhenNotFound()
    {
        var service = new JobPostingService();
        Assert.Null(service.FindById(Guid.NewGuid()));
    }
}
