namespace AggregateRootPattern.Domain;

public sealed class Beneficiary
{
    public int     BeneficiaryId { get; }
    public string  Name         { get; }
    public string  Relationship { get; }
    public decimal Percentage   { get; }

    internal Beneficiary(int beneficiaryId, string name, string relationship, decimal percentage)
    {
        BeneficiaryId = beneficiaryId;
        Name          = name;
        Relationship  = relationship;
        Percentage    = percentage;
    }
}
