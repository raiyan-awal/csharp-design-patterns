namespace OutboxPattern.Core;

public sealed class OutboxMessage
{
    public Guid      Id             { get; init; } = Guid.NewGuid();
    public string    EventType      { get; init; } = "";
    public string    Payload        { get; init; } = "";
    public DateTime  CreatedAtUtc   { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public bool      IsProcessed    => ProcessedAtUtc.HasValue;
}
