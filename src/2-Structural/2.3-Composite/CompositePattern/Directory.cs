namespace CompositePattern;

// ============================================================
// COMPOSITE
// ============================================================
// A Directory can hold any number of IFileSystemEntry children —
// that includes both Files (leaves) and other Directories (composites).
//
// The composite's job is to forward operations to its children
// and aggregate results. It doesn't know or care whether a child
// is a File or another Directory — it just calls the same interface.
//
// This recursive structure is the essence of the Composite pattern:
// a tree of uniform nodes, each capable of being a leaf or a branch.

/// <summary>
/// A composite node in the file system tree.
/// Can contain <see cref="File"/> leaves and nested <see cref="Directory"/> composites.
/// </summary>
public sealed class Directory : IFileSystemEntry
{
    private readonly List<IFileSystemEntry> _children = [];

    public Directory(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Directory name cannot be empty.", nameof(name));

        Name = name;
    }

    public string Name { get; }

    // ── Child management ──────────────────────────────────────

    public Directory Add(IFileSystemEntry entry)
    {
        _children.Add(entry);
        return this;  // fluent — allows chaining: dir.Add(file1).Add(file2)
    }

    public bool Remove(IFileSystemEntry entry) => _children.Remove(entry);

    public IReadOnlyList<IFileSystemEntry> Children => _children.AsReadOnly();

    // ── IFileSystemEntry ──────────────────────────────────────

    // Recursively sums the size of all children.
    // Each child may itself be a Directory that recurses further.
    // The call tree mirrors the directory tree exactly.
    public long GetSize() => _children.Sum(c => c.GetSize());

    public void Display(int depth = 0)
    {
        string indent = new(' ', depth * 2);
        Console.WriteLine($"{indent}📁 {Name}/ ({FormatSize(GetSize())})");

        // Recursively display all children, one level deeper
        foreach (var child in _children)
            child.Display(depth + 1);
    }

    // Searches this directory and all descendants.
    public IEnumerable<IFileSystemEntry> Search(Func<IFileSystemEntry, bool> predicate)
    {
        // Check the directory itself first
        if (predicate(this))
            yield return this;

        // Then recursively delegate to each child
        foreach (var child in _children)
            foreach (var match in child.Search(predicate))
                yield return match;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576     => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024         => $"{bytes / 1_024.0:F1} KB",
        _                => $"{bytes} B"
    };
}
