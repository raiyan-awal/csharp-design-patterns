namespace FlyweightPattern;

// ============================================================
// CLIENT CONTAINER
// ============================================================
// Forest manages a large collection of Tree contexts and owns
// the flyweight factory. Callers just call PlantTree() with all
// the data they have; the Forest handles deduplication silently.

public sealed class Forest
{
    private readonly List<Tree>      _trees   = [];
    private readonly TreeTypeFactory _factory = new();

    public int TreeCount       => _trees.Count;
    public int UniqueTypeCount => _factory.UniqueTypeCount;

    public IReadOnlyList<Tree>            Trees       => _trees;
    public IReadOnlyCollection<TreeType>  CachedTypes => _factory.CachedTypes;

    public void PlantTree(int x, int y, int heightCm, int ageYears,
                          string species, string colour, string texture)
    {
        var type = _factory.GetOrCreate(species, colour, texture);
        _trees.Add(new Tree(x, y, heightCm, ageYears, type));
    }

    public void Draw()
    {
        foreach (var tree in _trees)
            tree.Draw();
    }
}
