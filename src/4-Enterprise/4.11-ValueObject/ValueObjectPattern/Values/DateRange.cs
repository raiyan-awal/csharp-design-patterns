namespace ValueObjectPattern.Values;

// Structural equality: two DateRange instances with the same Start and End are equal.
// Immutable: all operations return new values or primitives.
public readonly record struct DateRange
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
            throw new ArgumentException($"End ({end}) must not be before Start ({start}).");
        Start = start;
        End   = end;
    }

    public int DurationDays => End.DayNumber - Start.DayNumber;

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public bool Overlaps(DateRange other) => Start <= other.End && End >= other.Start;

    public DateRange? Intersection(DateRange other)
    {
        var start = Start > other.Start ? Start : other.Start;
        var end   = End   < other.End   ? End   : other.End;
        return start <= end ? new DateRange(start, end) : null;
    }

    public override string ToString() =>
        $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd} ({DurationDays} days)";
}
