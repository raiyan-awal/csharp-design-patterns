namespace FlyweightPattern;

// ============================================================
// FLYWEIGHT — Intrinsic State
// ============================================================
// Stores state that is IDENTICAL across many Tree instances:
// species name, colour, and texture path. This object is
// immutable and shared. The factory ensures only one instance
// exists per unique (species, colour, texture) combination.
//
// Extrinsic state (position, height, age) is NEVER stored here.
// It is passed in at render time by each Tree context.

public sealed class TreeType
{
    public string Species { get; }
    public string Colour  { get; }
    public string Texture { get; }

    // internal so only TreeTypeFactory can create instances
    internal TreeType(string species, string colour, string texture)
    {
        Species = species;
        Colour  = colour;
        Texture = texture;
    }

    // Extrinsic state flows IN as parameters — never stored
    public void Draw(int x, int y, int heightCm, int ageYears)
        => Console.WriteLine(
            $"  [{Species,-10}] ({x,4},{y,4})  h={heightCm,4}cm  age={ageYears,3}yr  tex={Texture}");
}
