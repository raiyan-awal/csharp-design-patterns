namespace AggregateRootPattern.Repositories;

using AggregateRootPattern.Domain;

public interface IPolicyRepository
{
    InsurancePolicy?              FindById(int id);
    InsurancePolicy?              FindByPolicyNumber(string policyNumber);
    IReadOnlyList<InsurancePolicy> FindActiveByHolder(string holderName);
    void                          Save(InsurancePolicy policy);
}
