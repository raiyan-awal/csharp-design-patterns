namespace FlyweightPattern;

// ============================================================
// CONTEXT — Extrinsic State + Flyweight Reference
// ============================================================
// Each Tree holds only what is UNIQUE to it: position and size.
// The shared intrinsic state (species, colour, texture) lives
// in the TreeType flyweight — referenced here but not copied.
//
// Memory per tree instance (approximate):
//   X, Y, HeightCm, AgeYears  — 4 × 4 bytes  =  16 bytes
//   _type reference            —       8 bytes  =   8 bytes
//                                               ─────────
//   Total per tree             —               ~24 bytes
//
// Without Flyweight every tree would also duplicate the strings
// stored in TreeType — roughly 150+ additional bytes per tree.

public sealed class Tree
{
    public int X        { get; }
    public int Y        { get; }
    public int HeightCm { get; }
    public int AgeYears { get; }

    private readonly TreeType _type;

    public string Species => _type.Species;

    // Expose shared type so callers can verify sharing (e.g. ReferenceEquals)
    public TreeType Type => _type;

    public Tree(int x, int y, int heightCm, int ageYears, TreeType type)
    {
        X        = x;
        Y        = y;
        HeightCm = heightCm;
        AgeYears = ageYears;
        _type    = type;
    }

    // Delegates to the flyweight, passing extrinsic state as arguments
    public void Draw() => _type.Draw(X, Y, HeightCm, AgeYears);
}
