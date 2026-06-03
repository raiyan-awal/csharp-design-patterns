namespace CompositePattern;

// ============================================================
// LEAF
// ============================================================
// A File is a leaf node — it has no children, cannot contain
// other entries, and returns its own size directly.
//
// Leaf classes are where the actual work happens.
// All composite behaviour (recursion, aggregation) lives in
// Directory. File just answers for itself.

/// <summary>
/// A leaf node in the file system tree. Has a fixed size and no children.
/// </summary>
public sealed class File : IFileSystemEntry
{
    private readonly long _sizeInBytes;

    public File(string name, long sizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("File name cannot be empty.", nameof(name));
        if (sizeInBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Size cannot be negative.");

        Name         = name;
        _sizeInBytes = sizeInBytes;
    }

    public string Name { get; }

    // A file's size is just its own size — no recursion needed.
    public long GetSize() => _sizeInBytes;

    public void Display(int depth = 0)
    {
        string indent = new(' ', depth * 2);
        Console.WriteLine($"{indent}📄 {Name} ({FormatSize(_sizeInBytes)})");
    }

    // A file matches only itself.
    public IEnumerable<IFileSystemEntry> Search(Func<IFileSystemEntry, bool> predicate)
    {
        if (predicate(this))
            yield return this;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576     => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024         => $"{bytes / 1_024.0:F1} KB",
        _                => $"{bytes} B"
    };
}
