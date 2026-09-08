namespace HostedServicePattern.Domain;

public sealed record Order(string Id, string CustomerName, string Item, decimal AmountCAD);
