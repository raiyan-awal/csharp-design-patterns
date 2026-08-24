using AggregateRootPattern.Domain;
using AggregateRootPattern.Repositories;

public class InsurancePolicyConstructionTests
{
    private static InsurancePolicy Make() =>
        new(1, "NSL-2026-001", "Jean-François Tremblay", 500_000m, 1_200m);

    [Fact]
    public void Construction_SetsStatus_Active()
    {
        Assert.Equal(PolicyStatus.Active, Make().Status);
    }

    [Fact]
    public void Construction_StartsAtVersionZero()
    {
        Assert.Equal(0, Make().Version);
    }

    [Fact]
    public void Construction_EmptyRidersAndBeneficiaries()
    {
        var p = Make();
        Assert.Empty(p.Riders);
        Assert.Empty(p.Beneficiaries);
    }

    [Fact]
    public void Construction_NegativeBaseCoverage_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new InsurancePolicy(1, "P-001", "Test", -1m, 100m));
    }

    [Fact]
    public void Construction_ExceedsMaxCoverage_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new InsurancePolicy(1, "P-001", "Test", 6_000_000m, 100m));
    }
}

public class PolicyRiderTests
{
    private static InsurancePolicy MakePolicy(decimal baseCoverage = 500_000m) =>
        new(1, "NSL-2026-001", "Jean-François Tremblay", baseCoverage, 1_200m);

    [Fact]
    public void AddRider_ValidRider_AppearsInList()
    {
        var p = MakePolicy();
        p.AddRider("CriticalIllness", 250_000m, 420m);
        Assert.Single(p.Riders);
        Assert.Equal("CriticalIllness", p.Riders[0].Type);
    }

    [Fact]
    public void AddRider_IncrementsVersion()
    {
        var p = MakePolicy();
        p.AddRider("CriticalIllness", 250_000m, 420m);
        Assert.Equal(1, p.Version);
    }

    [Fact]
    public void AddRider_AssignsIncrementingLocalIds()
    {
        var p = MakePolicy();
        p.AddRider("CriticalIllness", 100_000m, 200m);
        p.AddRider("AccidentalDeath",  200_000m, 150m);
        Assert.Equal(1, p.Riders[0].RiderId);
        Assert.Equal(2, p.Riders[1].RiderId);
    }

    [Fact]
    public void AddRider_DuplicateType_Throws()
    {
        var p = MakePolicy();
        p.AddRider("CriticalIllness", 100_000m, 200m);
        Assert.Throws<InvalidOperationException>(() =>
            p.AddRider("CriticalIllness", 50_000m, 100m));
    }

    [Fact]
    public void AddRider_DuplicateThrows_RiderCountUnchanged()
    {
        var p = MakePolicy();
        p.AddRider("CriticalIllness", 100_000m, 200m);
        try { p.AddRider("CriticalIllness", 50_000m, 100m); } catch { }
        Assert.Single(p.Riders);
    }

    [Fact]
    public void AddRider_WouldExceedMaxCoverage_Throws()
    {
        var p = MakePolicy(4_800_000m);
        Assert.Throws<InvalidOperationException>(() =>
            p.AddRider("CriticalIllness", 300_000m, 500m));
    }

    [Fact]
    public void AddRider_OnCancelledPolicy_Throws()
    {
        var p = MakePolicy();
        p.Cancel("test");
        Assert.Throws<InvalidOperationException>(() =>
            p.AddRider("CriticalIllness", 100_000m, 200m));
    }

    [Fact]
    public void TotalCoverage_IncludesAllRiders()
    {
        var p = MakePolicy(500_000m);
        p.AddRider("CriticalIllness", 250_000m, 420m);
        p.AddRider("AccidentalDeath",  100_000m, 180m);
        Assert.Equal(850_000m, p.TotalCoverage);
    }

    [Fact]
    public void TotalAnnualPremium_IncludesAllRiders()
    {
        var p = MakePolicy();
        p.AddRider("CriticalIllness", 250_000m, 420m);
        p.AddRider("AccidentalDeath",  100_000m, 180m);
        Assert.Equal(1_800m, p.TotalAnnualPremium);
    }

    [Fact]
    public void RemoveRider_ExistingRider_RemovesFromList()
    {
        var p = MakePolicy();
        p.AddRider("CriticalIllness", 250_000m, 420m);
        p.RemoveRider("CriticalIllness");
        Assert.Empty(p.Riders);
    }

    [Fact]
    public void RemoveRider_NonExistentRider_Throws()
    {
        var p = MakePolicy();
        Assert.Throws<InvalidOperationException>(() =>
            p.RemoveRider("WaiverOfPremium"));
    }
}

public class BeneficiaryTests
{
    private static InsurancePolicy MakePolicy() =>
        new(1, "NSL-2026-001", "Jean-François Tremblay", 500_000m, 1_200m);

    [Fact]
    public void AddBeneficiary_ValidEntry_AppearsInList()
    {
        var p = MakePolicy();
        p.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 60m);
        Assert.Single(p.Beneficiaries);
        Assert.Equal("Marie-Claire Tremblay", p.Beneficiaries[0].Name);
    }

    [Fact]
    public void AddBeneficiary_AssignsIncrementingLocalIds()
    {
        var p = MakePolicy();
        p.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 60m);
        p.AddBeneficiary("Luc Tremblay",           "Child",  40m);
        Assert.Equal(1, p.Beneficiaries[0].BeneficiaryId);
        Assert.Equal(2, p.Beneficiaries[1].BeneficiaryId);
    }

    [Fact]
    public void AddBeneficiary_DuplicateName_Throws()
    {
        var p = MakePolicy();
        p.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 50m);
        Assert.Throws<InvalidOperationException>(() =>
            p.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 10m));
    }

    [Fact]
    public void AddBeneficiary_WouldExceed100Percent_Throws()
    {
        var p = MakePolicy();
        p.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 90m);
        Assert.Throws<InvalidOperationException>(() =>
            p.AddBeneficiary("Luc Tremblay", "Child", 15m));
    }

    [Fact]
    public void AddBeneficiary_ZeroPercentage_Throws()
    {
        var p = MakePolicy();
        Assert.Throws<ArgumentException>(() =>
            p.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 0m));
    }

    [Fact]
    public void AddBeneficiary_OnCancelledPolicy_Throws()
    {
        var p = MakePolicy();
        p.Cancel("test");
        Assert.Throws<InvalidOperationException>(() =>
            p.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 50m));
    }

    [Fact]
    public void TotalBeneficiaryPercentage_SumsCorrectly()
    {
        var p = MakePolicy();
        p.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 60m);
        p.AddBeneficiary("Luc Tremblay",          "Child",  30m);
        p.AddBeneficiary("Sophie Tremblay",        "Child",  10m);
        Assert.Equal(100m, p.TotalBeneficiaryPercentage);
    }

    [Fact]
    public void RemoveBeneficiary_ExistingEntry_RemovesFromList()
    {
        var p = MakePolicy();
        p.AddBeneficiary("Marie-Claire Tremblay", "Spouse", 60m);
        p.RemoveBeneficiary("Marie-Claire Tremblay");
        Assert.Empty(p.Beneficiaries);
    }

    [Fact]
    public void RemoveBeneficiary_NonExistentEntry_Throws()
    {
        var p = MakePolicy();
        Assert.Throws<InvalidOperationException>(() =>
            p.RemoveBeneficiary("Nobody"));
    }
}

public class CancellationTests
{
    private static InsurancePolicy MakePolicy() =>
        new(1, "NSL-2026-001", "Jean-François Tremblay", 500_000m, 1_200m);

    [Fact]
    public void Cancel_TransitionsToCancelled()
    {
        var p = MakePolicy();
        p.Cancel("Client request");
        Assert.Equal(PolicyStatus.Cancelled, p.Status);
    }

    [Fact]
    public void Cancel_StoresReason()
    {
        var p = MakePolicy();
        p.Cancel("Client request");
        Assert.Equal("Client request", p.CancellationReason);
    }

    [Fact]
    public void Cancel_IncrementsVersion()
    {
        var p = MakePolicy();
        p.Cancel("Client request");
        Assert.Equal(1, p.Version);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var p = MakePolicy();
        p.Cancel("Client request");
        Assert.Throws<InvalidOperationException>(() => p.Cancel("Again"));
    }
}

public class RepositoryTests
{
    [Fact]
    public void Save_AndFindById_ReturnsSameAggregate()
    {
        var repo   = new InMemoryPolicyRepository();
        var policy = new InsurancePolicy(1, "NSL-001", "Jean-François Tremblay", 500_000m, 1_200m);
        repo.Save(policy);
        var found = repo.FindById(1);
        Assert.NotNull(found);
        Assert.Equal("NSL-001", found.PolicyNumber);
    }

    [Fact]
    public void FindByPolicyNumber_ReturnsCorrectPolicy()
    {
        var repo   = new InMemoryPolicyRepository();
        var policy = new InsurancePolicy(1, "NSL-001", "Jean-François Tremblay", 500_000m, 1_200m);
        repo.Save(policy);
        var found = repo.FindByPolicyNumber("NSL-001");
        Assert.NotNull(found);
        Assert.Equal(1, found.Id);
    }

    [Fact]
    public void FindActiveByHolder_ExcludesCancelledPolicies()
    {
        var repo      = new InMemoryPolicyRepository();
        var active    = new InsurancePolicy(1, "NSL-001", "Jean-François Tremblay", 500_000m, 1_200m);
        var cancelled = new InsurancePolicy(2, "NSL-002", "Jean-François Tremblay", 500_000m, 1_200m);
        cancelled.Cancel("test");
        repo.Save(active);
        repo.Save(cancelled);
        var found = repo.FindActiveByHolder("Jean-François Tremblay");
        Assert.Single(found);
        Assert.Equal("NSL-001", found[0].PolicyNumber);
    }

    [Fact]
    public void FindById_NonExistent_ReturnsNull()
    {
        var repo = new InMemoryPolicyRepository();
        Assert.Null(repo.FindById(99));
    }
}
