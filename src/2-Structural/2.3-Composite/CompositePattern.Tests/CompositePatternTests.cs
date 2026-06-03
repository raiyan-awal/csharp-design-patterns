using CompositePattern;

using Directory = CompositePattern.Directory;

namespace CompositePattern.Tests;

// ============================================================
// COMPOSITE PATTERN TESTS
// ============================================================
// Tests cover:
//   • File (leaf): size, name, validation, search
//   • Directory (composite): size aggregation, child management,
//     recursive search, nested trees
//   • Uniform treatment: operations on IFileSystemEntry work
//     identically whether the target is a file or directory
// ============================================================

public class FileTests
{
    [Fact]
    public void File_GetSize_ReturnsItsOwnSize()
    {
        var file = new File("notes.txt", 1_024);
        Assert.Equal(1_024, file.GetSize());
    }

    [Fact]
    public void File_Name_ReturnsCorrectName()
    {
        var file = new File("report.pdf", 500);
        Assert.Equal("report.pdf", file.Name);
    }

    [Fact]
    public void File_ZeroSize_IsAllowed()
    {
        var file = new File("empty.txt", 0);
        Assert.Equal(0, file.GetSize());
    }

    [Fact]
    public void File_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new File("", 100));
    }

    [Fact]
    public void File_WhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new File("   ", 100));
    }

    [Fact]
    public void File_NegativeSize_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new File("bad.txt", -1));
    }

    [Fact]
    public void File_Search_MatchingPredicate_ReturnsSelf()
    {
        var file = new File("Program.cs", 2_000);
        var results = file.Search(e => e.Name.EndsWith(".cs")).ToList();

        Assert.Single(results);
        Assert.Same(file, results[0]);
    }

    [Fact]
    public void File_Search_NonMatchingPredicate_ReturnsEmpty()
    {
        var file = new File("Program.cs", 2_000);
        var results = file.Search(e => e.Name.EndsWith(".txt")).ToList();

        Assert.Empty(results);
    }
}

public class DirectoryTests
{
    [Fact]
    public void Directory_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Directory(""));
    }

    [Fact]
    public void Directory_NoChildren_SizeIsZero()
    {
        var dir = new Directory("empty");
        Assert.Equal(0, dir.GetSize());
    }

    [Fact]
    public void Directory_WithFiles_SizeIsSumOfChildren()
    {
        var dir = new Directory("src");
        dir.Add(new File("a.cs", 1_000))
           .Add(new File("b.cs", 2_000))
           .Add(new File("c.cs", 3_000));

        Assert.Equal(6_000, dir.GetSize());
    }

    [Fact]
    public void Directory_NestedDirectories_SizeIsRecursiveSum()
    {
        var inner = new Directory("inner");
        inner.Add(new File("x.cs", 500))
             .Add(new File("y.cs", 500));

        var outer = new Directory("outer");
        outer.Add(new File("z.cs", 1_000))
             .Add(inner);

        // outer: 1000 + inner (500 + 500) = 2000
        Assert.Equal(2_000, outer.GetSize());
    }

    [Fact]
    public void Directory_Add_ReturnsItself_ForFluentChaining()
    {
        var dir = new Directory("src");
        var returned = dir.Add(new File("a.cs", 100));

        Assert.Same(dir, returned);
    }

    [Fact]
    public void Directory_Children_ReflectsAddedEntries()
    {
        var dir = new Directory("src");
        var file = new File("a.cs", 100);
        dir.Add(file);

        Assert.Contains(file, dir.Children);
    }

    [Fact]
    public void Directory_Remove_ExistingChild_ReturnsTrueAndRemoves()
    {
        var dir  = new Directory("src");
        var file = new File("a.cs", 100);
        dir.Add(file);

        bool removed = dir.Remove(file);

        Assert.True(removed);
        Assert.DoesNotContain(file, dir.Children);
    }

    [Fact]
    public void Directory_Remove_NonExistentChild_ReturnsFalse()
    {
        var dir  = new Directory("src");
        var file = new File("ghost.cs", 100);

        Assert.False(dir.Remove(file));
    }

    [Fact]
    public void Directory_Search_FindsMatchingFiles()
    {
        var dir = new Directory("src");
        dir.Add(new File("Program.cs", 1_000))
           .Add(new File("README.md",  500));

        var results = dir.Search(e => e.Name.EndsWith(".cs")).ToList();

        Assert.Single(results);
        Assert.Equal("Program.cs", results[0].Name);
    }

    [Fact]
    public void Directory_Search_RecursivelySearchesChildren()
    {
        var inner = new Directory("controllers");
        inner.Add(new File("HomeController.cs", 2_000))
             .Add(new File("OrderController.cs", 3_000));

        var outer = new Directory("src");
        outer.Add(new File("Program.cs", 1_000))
             .Add(inner);

        var csFiles = outer.Search(e => e.Name.EndsWith(".cs")).ToList();

        Assert.Equal(3, csFiles.Count);
    }

    [Fact]
    public void Directory_Search_CanMatchDirectoriesThemselves()
    {
        var inner = new Directory("tests");
        var outer = new Directory("root");
        outer.Add(inner);

        var dirs = outer.Search(e => e is Directory).ToList();

        // Should find both outer and inner
        Assert.Equal(2, dirs.Count);
    }

    [Fact]
    public void Directory_Search_NoPredicate_ReturnsEmpty()
    {
        var dir = new Directory("src");
        dir.Add(new File("a.cs", 100));

        var results = dir.Search(_ => false).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void Directory_DeeplyNested_GetSize_IsCorrect()
    {
        // Build: root → level1 → level2 → file (1000 bytes)
        var level2 = new Directory("level2");
        level2.Add(new File("deep.txt", 1_000));

        var level1 = new Directory("level1");
        level1.Add(level2);

        var root = new Directory("root");
        root.Add(level1);

        Assert.Equal(1_000, root.GetSize());
    }
}

public class UniformTreatmentTests
{
    // These tests prove the core Composite guarantee:
    // IFileSystemEntry works identically on leaves and composites.

    [Fact]
    public void GetSize_WorksUniformly_OnFileAndDirectory()
    {
        IFileSystemEntry leaf      = new File("data.csv", 5_000);
        IFileSystemEntry composite = new Directory("folder");
        ((Directory)composite).Add(new File("a.txt", 2_000))
                               .Add(new File("b.txt", 3_000));

        // Same call — different behaviour — same result
        Assert.Equal(5_000, leaf.GetSize());
        Assert.Equal(5_000, composite.GetSize());
    }

    [Fact]
    public void Search_WorksUniformly_OnFileAndDirectory()
    {
        IFileSystemEntry leaf      = new File("match.cs", 100);
        IFileSystemEntry composite = new Directory("src");
        ((Directory)composite).Add(new File("match.cs", 100));

        Func<IFileSystemEntry, bool> predicate = e => e.Name == "match.cs";

        Assert.Single(leaf.Search(predicate));
        Assert.Single(composite.Search(predicate));
    }

    [Fact]
    public void DirectoryInsideDirectory_SizeAggregatesCorrectly()
    {
        var docs = new Directory("docs");
        docs.Add(new File("api.md",       8_192))
            .Add(new File("readme.md",    2_048));

        var assets = new Directory("assets");
        assets.Add(new File("logo.png",  51_200))
              .Add(new File("banner.jpg", 102_400));

        var root = new Directory("project");
        root.Add(docs).Add(assets);

        Assert.Equal(8_192 + 2_048 + 51_200 + 102_400, root.GetSize());
    }

    [Fact]
    public void EmptyDirectory_ContributesZeroToParentSize()
    {
        var empty  = new Directory("empty");
        var parent = new Directory("parent");
        parent.Add(new File("file.txt", 1_000))
              .Add(empty);

        Assert.Equal(1_000, parent.GetSize());
    }
}
