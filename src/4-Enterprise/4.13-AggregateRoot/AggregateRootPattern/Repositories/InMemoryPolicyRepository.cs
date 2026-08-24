namespace AggregateRootPattern.Repositories;

using AggregateRootPattern.Domain;

public sealed class InMemoryPolicyRepository : IPolicyRepository
{
    private readonly Dictionary<int, InsurancePolicy> _store = new();

    public InsurancePolicy? FindById(int id) =>
        _store.TryGetValue(id, out var p) ? p : null;

    public InsurancePolicy? FindByPolicyNumber(string policyNumber) =>
        _store.Values.FirstOrDefault(p => p.PolicyNumber == policyNumber);

    public IReadOnlyList<InsurancePolicy> FindActiveByHolder(string holderName) =>
        _store.Values
              .Where(p => p.HolderName == holderName && p.Status == PolicyStatus.Active)
              .ToList();

    public void Save(InsurancePolicy policy) => _store[policy.Id] = policy;
}
