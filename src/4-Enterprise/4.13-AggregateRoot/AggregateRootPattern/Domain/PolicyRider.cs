namespace AggregateRootPattern.Domain;

public sealed class PolicyRider
{
    public int     RiderId            { get; }
    public string  Type               { get; }
    public decimal AdditionalCoverage { get; }
    public decimal AnnualPremium      { get; }

    internal PolicyRider(int riderId, string type, decimal additionalCoverage, decimal annualPremium)
    {
        RiderId            = riderId;
        Type               = type;
        AdditionalCoverage = additionalCoverage;
        AnnualPremium      = annualPremium;
    }
}
