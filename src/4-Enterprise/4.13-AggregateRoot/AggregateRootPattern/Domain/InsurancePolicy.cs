namespace AggregateRootPattern.Domain;

public sealed class InsurancePolicy : AggregateRoot
{
    private const decimal MaxTotalCoverage = 5_000_000m;

    private readonly List<PolicyRider> _riders        = new();
    private readonly List<Beneficiary> _beneficiaries = new();
    private int _nextRiderId       = 1;
    private int _nextBeneficiaryId = 1;

    public string       PolicyNumber       { get; }
    public string       HolderName         { get; }
    public decimal      BaseCoverage       { get; }
    public decimal      AnnualBasePremium  { get; }
    public PolicyStatus Status             { get; private set; }
    public string?      CancellationReason { get; private set; }

    public IReadOnlyList<PolicyRider> Riders        => _riders;
    public IReadOnlyList<Beneficiary> Beneficiaries => _beneficiaries;

    public decimal TotalCoverage              => BaseCoverage + _riders.Sum(r => r.AdditionalCoverage);
    public decimal TotalAnnualPremium         => AnnualBasePremium + _riders.Sum(r => r.AnnualPremium);
    public decimal TotalBeneficiaryPercentage => _beneficiaries.Sum(b => b.Percentage);

    public InsurancePolicy(int id, string policyNumber, string holderName,
                           decimal baseCoverage, decimal annualBasePremium)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(holderName);
        if (baseCoverage <= 0)
            throw new ArgumentException("Base coverage must be positive.", nameof(baseCoverage));
        if (baseCoverage > MaxTotalCoverage)
            throw new ArgumentException(
                $"Base coverage cannot exceed ${MaxTotalCoverage:N0} CAD.", nameof(baseCoverage));

        Id                = id;
        PolicyNumber      = policyNumber;
        HolderName        = holderName;
        BaseCoverage      = baseCoverage;
        AnnualBasePremium = annualBasePremium;
        Status            = PolicyStatus.Active;
    }

    public void AddRider(string type, decimal additionalCoverage, decimal annualPremium)
    {
        EnsureActive();
        if (_riders.Any(r => r.Type == type))
            throw new InvalidOperationException(
                $"Rider '{type}' is already attached to this policy.");
        if (TotalCoverage + additionalCoverage > MaxTotalCoverage)
            throw new InvalidOperationException(
                $"Adding this rider would exceed the maximum total coverage of ${MaxTotalCoverage:N0} CAD.");

        _riders.Add(new PolicyRider(_nextRiderId++, type, additionalCoverage, annualPremium));
        IncrementVersion();
    }

    public void RemoveRider(string type)
    {
        EnsureActive();
        var rider = _riders.FirstOrDefault(r => r.Type == type)
            ?? throw new InvalidOperationException($"Rider '{type}' is not attached to this policy.");
        _riders.Remove(rider);
        IncrementVersion();
    }

    public void AddBeneficiary(string name, string relationship, decimal percentage)
    {
        EnsureActive();
        if (percentage <= 0 || percentage > 100)
            throw new ArgumentException("Percentage must be between 1 and 100.", nameof(percentage));
        if (_beneficiaries.Any(b => b.Name == name))
            throw new InvalidOperationException($"'{name}' is already listed as a beneficiary.");
        if (TotalBeneficiaryPercentage + percentage > 100)
            throw new InvalidOperationException(
                $"Adding {percentage}% would exceed 100% total allocation " +
                $"(currently at {TotalBeneficiaryPercentage}%).");

        _beneficiaries.Add(new Beneficiary(_nextBeneficiaryId++, name, relationship, percentage));
        IncrementVersion();
    }

    public void RemoveBeneficiary(string name)
    {
        EnsureActive();
        var b = _beneficiaries.FirstOrDefault(b => b.Name == name)
            ?? throw new InvalidOperationException($"'{name}' is not listed as a beneficiary.");
        _beneficiaries.Remove(b);
        IncrementVersion();
    }

    public void Cancel(string reason)
    {
        if (Status == PolicyStatus.Cancelled)
            throw new InvalidOperationException("Policy is already cancelled.");
        Status             = PolicyStatus.Cancelled;
        CancellationReason = reason;
        IncrementVersion();
    }

    private void EnsureActive()
    {
        if (Status != PolicyStatus.Active)
            throw new InvalidOperationException(
                $"Policy {PolicyNumber} is {Status} and cannot be modified.");
    }
}
