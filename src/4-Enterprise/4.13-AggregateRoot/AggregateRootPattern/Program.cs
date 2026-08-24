using AggregateRootPattern.Domain;
using AggregateRootPattern.Repositories;

Console.WriteLine("=== Northern Shield Life Insurance — Aggregate Root Demo ===\n");

var repo = new InMemoryPolicyRepository();

// --- Creating Policies ---
Console.WriteLine("--- Creating Policies ---");

var tremblay = new InsurancePolicy(1, "NSL-2026-001", "Jean-François Tremblay", 500_000m,   1_200m);
var okonkwo  = new InsurancePolicy(2, "NSL-2026-002", "Amara Okonkwo",          1_000_000m, 2_100m);

repo.Save(tremblay);
repo.Save(okonkwo);

PrintPolicy(tremblay);
PrintPolicy(okonkwo);

Pause();

// --- Adding Riders ---
Console.WriteLine("--- Adding Riders ---");

tremblay.AddRider("CriticalIllness",  250_000m, 420m);
tremblay.AddRider("AccidentalDeath",  500_000m, 180m);
okonkwo.AddRider("DisabilityIncome",  200_000m, 310m);
okonkwo.AddRider("WaiverOfPremium",         0m,  95m);

repo.Save(tremblay);
repo.Save(okonkwo);

PrintRiders(tremblay);
PrintRiders(okonkwo);

Pause();

// --- Invariant Violations ---
Console.WriteLine("--- Invariant Violations ---");

TryAction(() => tremblay.AddRider("CriticalIllness", 100_000m, 200m),
    "Duplicate rider 'CriticalIllness'");

var nearLimit = new InsurancePolicy(3, "NSL-2026-003", "Northern Corp Ltd.", 4_800_000m, 9_000m);
TryAction(() => nearLimit.AddRider("CriticalIllness", 300_000m, 500m),
    "Rider would push total past $5,000,000 CAD limit");

TryAction(() => tremblay.RemoveRider("WaiverOfPremium"),
    "Remove non-existent rider 'WaiverOfPremium'");

Pause();

// --- Adding Beneficiaries ---
Console.WriteLine("--- Adding Beneficiaries ---");

tremblay.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 60m);
tremblay.AddBeneficiary("Luc Tremblay",          "Child",  30m);
tremblay.AddBeneficiary("Sophie Tremblay",        "Child",  10m);

repo.Save(tremblay);

PrintBeneficiaries(tremblay);

TryAction(() => tremblay.AddBeneficiary("Extra Person", "Friend",  5m),
    "Beneficiary allocation already at 100%");
TryAction(() => tremblay.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 5m),
    "Duplicate beneficiary name");

Pause();

// --- Cancelling a Policy ---
Console.WriteLine("--- Cancelling a Policy ---");

okonkwo.Cancel("Client requested cancellation — relocated outside Canada.");
repo.Save(okonkwo);

Console.WriteLine($"[POLICY] {okonkwo.PolicyNumber} | Status: {okonkwo.Status} | v{okonkwo.Version}");
Console.WriteLine($"[REASON] {okonkwo.CancellationReason}");

TryAction(() => okonkwo.AddRider("CriticalIllness", 100_000m, 200m),
    "Add rider to cancelled policy");
TryAction(() => okonkwo.Cancel("Second attempt"),
    "Cancel already-cancelled policy");

static void PrintPolicy(InsurancePolicy p) =>
    Console.WriteLine(
        $"[POLICY] {p.PolicyNumber} — {p.HolderName} | " +
        $"Base: ${p.BaseCoverage:N0} CAD | Premium: ${p.AnnualBasePremium:N2}/yr | " +
        $"Status: {p.Status} | v{p.Version}");

static void PrintRiders(InsurancePolicy p)
{
    Console.WriteLine(
        $"\n  {p.PolicyNumber} | Total coverage: ${p.TotalCoverage:N0} CAD | " +
        $"Total premium: ${p.TotalAnnualPremium:N2}/yr | v{p.Version}");
    foreach (var r in p.Riders)
        Console.WriteLine(
            $"    [{r.RiderId}] {r.Type,-20} +${r.AdditionalCoverage:N0,-12} ${r.AnnualPremium:N2}/yr");
}

static void PrintBeneficiaries(InsurancePolicy p)
{
    Console.WriteLine($"\n  {p.PolicyNumber} beneficiaries (total: {p.TotalBeneficiaryPercentage}%):");
    foreach (var b in p.Beneficiaries)
        Console.WriteLine(
            $"    [{b.BeneficiaryId}] {b.Name,-25} ({b.Relationship,-10}) — {b.Percentage}%");
}

static void TryAction(Action action, string label)
{
    Console.Write($"  {label}: ");
    try   { action(); Console.WriteLine("OK"); }
    catch (Exception ex) { Console.WriteLine($"[ERROR] {ex.Message}"); }
}

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
