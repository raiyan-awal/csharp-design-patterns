namespace AntiCorruptionLayerPattern.Domain;

public sealed class Shipment
{
    public required string Id { get; init; }
    public required string RecipientName { get; init; }
    public required Address Destination { get; init; }
    public required Dimensions Package { get; init; }
    public required ShipmentStatus Status { get; init; }
    public required DateOnly ShippedOn { get; init; }
    public DateOnly? EstimatedDelivery { get; init; }
}
