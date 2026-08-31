using DTOPattern.DTOs;
using DTOPattern.Services;

Console.WriteLine("=== Maple Talent — DTO Pattern Demo ===\n");

var candidates = new CandidateService();
var jobs       = new JobPostingService();

// ── Section 1: Register candidates ───────────────────────────────────────────
Console.WriteLine("--- Section 1: Register Candidates (Request DTO → Domain → Response DTO) ---");
Console.WriteLine("  The service accepts a CreateCandidateRequest and returns a CandidateDto.");
Console.WriteLine("  Sensitive fields (InternalScore, BackgroundCheckNotes) never appear in the DTO.\n");

var sophie = candidates.Register(new CreateCandidateRequest(
    Name:                 "Sophie Tremblay",
    Email:                "sophie.tremblay@gmail.com",
    Skills:               ["C#", ".NET", "Azure"],
    YearsOfExperience:    6,
    SalaryExpectationCAD: 115_000m));

var marcus = candidates.Register(new CreateCandidateRequest(
    Name:                 "Marcus Osei",
    Email:                "marcus.osei@outlook.com",
    Skills:               ["TypeScript", "React", "Node.js"],
    YearsOfExperience:    4,
    SalaryExpectationCAD: 95_000m));

var alice = candidates.Register(new CreateCandidateRequest(
    Name:                 "Alice Chen",
    Email:                "alice.chen@maple.ca",
    Skills:               ["Python", "Machine Learning", "PyTorch"],
    YearsOfExperience:    8,
    SalaryExpectationCAD: 140_000m));

// Record internal assessments — these live only on the domain object
candidates.RecordAssessment(sophie.Id, score: 88, notes: "Strong system design, passed all technical rounds.");
candidates.RecordAssessment(marcus.Id, score: 74, notes: "Good frontend, weaker on backend architecture.");

Console.WriteLine($"  Registered: {sophie.Name}  |  {sophie.Email}  |  ${sophie.SalaryExpectationCAD:N0} CAD");
Console.WriteLine($"  Skills    : {string.Join(", ", sophie.Skills)}");
Console.WriteLine();
Console.WriteLine($"  Registered: {marcus.Name}  |  {marcus.Email}  |  ${marcus.SalaryExpectationCAD:N0} CAD");
Console.WriteLine($"  Skills    : {string.Join(", ", marcus.Skills)}");
Console.WriteLine();
Console.WriteLine("  Note: InternalScore and BackgroundCheckNotes are NOT present on CandidateDto.");

Pause();

// ── Section 2: List candidate summaries ──────────────────────────────────────
Console.WriteLine("--- Section 2: List View — CandidateSummaryDto (lighter shape) ---");
Console.WriteLine("  The summary DTO carries only what a recruiter needs at a glance:");
Console.WriteLine("  Id, Name, TopSkill, YearsOfExperience — no email, no salary, no internal data.\n");

foreach (var summary in candidates.ListSummaries())
    Console.WriteLine($"  {summary.Name,-20}  Top skill: {summary.TopSkill,-22}  {summary.YearsOfExperience} yr(s)");

Pause();

// ── Section 3: Create job postings ───────────────────────────────────────────
Console.WriteLine("--- Section 3: Create Job Postings (InternalBudgetCAD hidden in DTO) ---");
Console.WriteLine("  InternalBudgetCAD (what the company will actually pay) is stored on the");
Console.WriteLine("  domain object but excluded from JobPostingDto so job seekers can't see it.\n");

var job1 = jobs.Create(new CreateJobPostingRequest(
    Title:             "Senior .NET Developer",
    Company:           "Maple Systems Inc.",
    Location:          "Toronto, ON",
    SalaryMinCAD:      105_000m,
    SalaryMaxCAD:      130_000m,
    RequiredSkills:    ["C#", ".NET", "Azure"],
    InternalBudgetCAD: 145_000m));  // hidden from job seekers

var job2 = jobs.Create(new CreateJobPostingRequest(
    Title:             "ML Engineer",
    Company:           "Northern Digital",
    Location:          "Vancouver, BC",
    SalaryMinCAD:      120_000m,
    SalaryMaxCAD:      155_000m,
    RequiredSkills:    ["Python", "Machine Learning", "PyTorch"],
    InternalBudgetCAD: 170_000m));  // hidden from job seekers

foreach (var job in jobs.ListActive())
{
    Console.WriteLine($"  [{job.Company}] {job.Title}");
    Console.WriteLine($"    Location : {job.Location}");
    Console.WriteLine($"    Salary   : ${job.SalaryMinCAD:N0}–${job.SalaryMaxCAD:N0} CAD");
    Console.WriteLine($"    Skills   : {string.Join(", ", job.RequiredSkills)}");
    Console.WriteLine($"    Active   : {job.IsActive}");
    Console.WriteLine();
}

Console.WriteLine("  Note: InternalBudgetCAD is NOT present on JobPostingDto.");

Pause();

// ── Section 4: Find by ID — round-trip ───────────────────────────────────────
Console.WriteLine("--- Section 4: Find by ID — Demonstrating the Mapping Round-Trip ---");
Console.WriteLine("  Request DTO → domain object (stored) → response DTO (returned).\n");

var found = candidates.FindById(alice.Id);
if (found is not null)
{
    Console.WriteLine($"  Found     : {found.Name}");
    Console.WriteLine($"  Email     : {found.Email}");
    Console.WriteLine($"  Skills    : {string.Join(", ", found.Skills)}");
    Console.WriteLine($"  Salary    : ${found.SalaryExpectationCAD:N0} CAD");
    Console.WriteLine($"  Experience: {found.YearsOfExperience} years");
}

var notFound = candidates.FindById(Guid.NewGuid());
Console.WriteLine($"\n  Random ID lookup: {(notFound is null ? "not found (returns null, not exception)" : "found")}");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
