namespace MementoPattern;

public readonly record struct Position(int X, int Y)
{
    public override string ToString() => $"({X}, {Y})";
}
