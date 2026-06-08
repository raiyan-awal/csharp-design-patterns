using FlyweightPattern;

namespace FlyweightPattern.Tests;

// ── TreeTypeFactory ──────────────────────────────────────────

public class TreeTypeFactoryTests
{
    private readonly TreeTypeFactory _factory = new();

    [Fact]
    public void GetOrCreate_ReturnsSameInstance_ForIdenticalArguments()
    {
        var a = _factory.GetOrCreate("Oak", "green", "oak.png");
        var b = _factory.GetOrCreate("Oak", "green", "oak.png");
        Assert.Same(a, b);
    }

    [Fact]
    public void GetOrCreate_ReturnsDifferentInstance_ForDifferentSpecies()
    {
        var oak  = _factory.GetOrCreate("Oak",  "green", "oak.png");
        var pine = _factory.GetOrCreate("Pine", "green", "pine.png");
        Assert.NotSame(oak, pine);
    }

    [Fact]
    public void GetOrCreate_ReturnsDifferentInstance_ForDifferentColour()
    {
        var a = _factory.GetOrCreate("Oak", "dark green",  "oak.png");
        var b = _factory.GetOrCreate("Oak", "light green", "oak.png");
        Assert.NotSame(a, b);
    }

    [Fact]
    public void UniqueTypeCount_StartsAtZero()
        => Assert.Equal(0, _factory.UniqueTypeCount);

    [Fact]
    public void UniqueTypeCount_IncrementsOnlyForNewTypes()
    {
        _factory.GetOrCreate("Oak",  "green", "oak.png");
        _factory.GetOrCreate("Oak",  "green", "oak.png");  // same — no increment
        _factory.GetOrCreate("Pine", "green", "pine.png"); // new
        Assert.Equal(2, _factory.UniqueTypeCount);
    }

    [Fact]
    public void GetOrCreate_StoresCorrectProperties()
    {
        var type = _factory.GetOrCreate("Birch", "yellow", "birch.png");
        Assert.Equal("Birch",      type.Species);
        Assert.Equal("yellow",     type.Colour);
        Assert.Equal("birch.png",  type.Texture);
    }
}

// ── Tree ─────────────────────────────────────────────────────

public class TreeTests
{
    private static TreeType MakeType(string species = "Oak")
    {
        var factory = new TreeTypeFactory();
        return factory.GetOrCreate(species, "green", "oak.png");
    }

    [Fact]
    public void Tree_ExposesExtrinsicState()
    {
        var tree = new Tree(10, 20, 500, 30, MakeType());
        Assert.Equal(10,  tree.X);
        Assert.Equal(20,  tree.Y);
        Assert.Equal(500, tree.HeightCm);
        Assert.Equal(30,  tree.AgeYears);
    }

    [Fact]
    public void Tree_Species_ComesFromFlyweight()
    {
        var tree = new Tree(0, 0, 100, 5, MakeType("Pine"));
        Assert.Equal("Pine", tree.Species);
    }

    [Fact]
    public void Tree_Type_ReturnsSharedFlyweight()
    {
        var type = MakeType();
        var tree = new Tree(0, 0, 100, 5, type);
        Assert.Same(type, tree.Type);
    }
}

// ── Forest ───────────────────────────────────────────────────

public class ForestTests
{
    [Fact]
    public void PlantTree_IncreasesTreeCount()
    {
        var forest = new Forest();
        forest.PlantTree(0, 0, 100, 5, "Oak", "green", "oak.png");
        Assert.Equal(1, forest.TreeCount);
    }

    [Fact]
    public void PlantTree_DifferentSpecies_IncreasesUniqueTypeCount()
    {
        var forest = new Forest();
        forest.PlantTree(0, 0, 100, 5, "Oak",  "green", "oak.png");
        forest.PlantTree(1, 1, 200, 8, "Pine", "green", "pine.png");
        Assert.Equal(2, forest.UniqueTypeCount);
    }

    [Fact]
    public void PlantTree_SameSpecies_DoesNotIncreaseUniqueTypeCount()
    {
        var forest = new Forest();
        forest.PlantTree(0, 0, 100, 5, "Oak", "green", "oak.png");
        forest.PlantTree(1, 1, 200, 8, "Oak", "green", "oak.png");
        Assert.Equal(1, forest.UniqueTypeCount);
    }

    [Fact]
    public void SameSpecies_TreesShareSameFlyweightInstance()
    {
        var forest = new Forest();
        forest.PlantTree(0, 0, 100, 5, "Oak", "green", "oak.png");
        forest.PlantTree(1, 1, 200, 8, "Oak", "green", "oak.png");

        var trees = forest.Trees;
        Assert.Same(trees[0].Type, trees[1].Type);
    }

    [Fact]
    public void DifferentSpecies_TreesHaveDifferentFlyweightInstances()
    {
        var forest = new Forest();
        forest.PlantTree(0, 0, 100, 5, "Oak",  "green", "oak.png");
        forest.PlantTree(1, 1, 200, 8, "Pine", "green", "pine.png");

        Assert.NotSame(forest.Trees[0].Type, forest.Trees[1].Type);
    }

    [Fact]
    public void UniqueTypeCount_AlwaysLessThanOrEqualToTreeCount()
    {
        var forest = new Forest();
        for (int i = 0; i < 100; i++)
            forest.PlantTree(i, i, 100 + i, 5, "Oak", "green", "oak.png");

        Assert.True(forest.UniqueTypeCount <= forest.TreeCount);
        Assert.Equal(1, forest.UniqueTypeCount);
        Assert.Equal(100, forest.TreeCount);
    }

    [Fact]
    public void LargeForest_ManySpecies_CorrectTypeCounts()
    {
        var forest = new Forest();
        string[] names = ["Oak", "Pine", "Birch", "Maple", "Spruce"];

        foreach (var name in names)
            for (int i = 0; i < 1_000; i++)
                forest.PlantTree(i, i, 100, 10, name, "green", $"{name}.png");

        Assert.Equal(5_000, forest.TreeCount);
        Assert.Equal(5,     forest.UniqueTypeCount);
    }

    [Fact]
    public void Trees_HaveCorrectExtrinsicState_AfterPlanting()
    {
        var forest = new Forest();
        forest.PlantTree(42, 99, 750, 25, "Oak", "green", "oak.png");

        var tree = forest.Trees[0];
        Assert.Equal(42,  tree.X);
        Assert.Equal(99,  tree.Y);
        Assert.Equal(750, tree.HeightCm);
        Assert.Equal(25,  tree.AgeYears);
    }
}
