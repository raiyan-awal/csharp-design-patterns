namespace CompositePattern;

// ============================================================
// COMPONENT INTERFACE
// ============================================================
// This is the uniform interface that both leaves (File) and
// composites (Directory) implement.
//
// The key insight of Composite: the CLIENT never needs to know
// whether it's working with a single file or an entire directory
// tree. GetSize() and Display() work the same way on both.
//
// This is what "treat individual objects and compositions
// uniformly" means in practice.

/// <summary>
/// The component interface. Both <see cref="File"/> (leaf) and
/// <see cref="Directory"/> (composite) implement this contract.
/// </summary>
public interface IFileSystemEntry
{
    /// <summary>Name of this file or directory.</summary>
    string Name { get; }

    /// <summary>
    /// Total size in bytes.
    /// For a file: its own size.
    /// For a directory: recursive sum of all children.
    /// </summary>
    long GetSize();

    /// <summary>
    /// Prints the entry to the console, indented by <paramref name="depth"/> levels.
    /// For a directory, recursively displays all children.
    /// </summary>
    void Display(int depth = 0);

    /// <summary>
    /// Searches this entry (and all descendants if composite)
    /// for entries matching the given predicate.
    /// </summary>
    IEnumerable<IFileSystemEntry> Search(Func<IFileSystemEntry, bool> predicate);
}
