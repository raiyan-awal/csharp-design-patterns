namespace AntiCorruptionLayerPattern.Domain;

public sealed record Dimensions(
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    decimal WeightKg);
