namespace PrototypePattern;

/// <summary>
/// PROTOTYPE REGISTRY — a catalogue of named enemy templates.
///
/// The registry stores one master copy of each enemy type.
/// Callers ask for a spawn by name; the registry deep-clones the template
/// and returns an independent instance.
///
/// This separates two concerns:
///   • Template authoring  — level designers configure templates once
///   • Instance creation   — game engine spawns hundreds of copies at runtime
///
/// The registry never exposes its master copies directly — every Spawn()
/// call returns a fresh clone, so external code cannot accidentally mutate
/// the template and corrupt future spawns.
/// </summary>
public sealed class EnemyRegistry
{
    private readonly Dictionary<string, Enemy> _templates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a template under the given key.
    /// Overwrites any existing template with the same key.
    /// </summary>
    public void Register(string key, Enemy template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(template, nameof(template));

        // Store a deep clone of the template so the caller can't mutate
        // the registry's internal copy after registration.
        _templates[key] = template.DeepClone();
    }

    /// <summary>
    /// Returns a fresh deep clone of the named template.
    /// Each call produces a fully independent Enemy instance.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="key"/> is not registered.</exception>
    public Enemy Spawn(string key)
    {
        if (!_templates.TryGetValue(key, out var template))
            throw new KeyNotFoundException(
                $"No enemy template registered for key '{key}'. " +
                $"Registered keys: [{string.Join(", ", _templates.Keys)}]");

        return template.DeepClone();
    }

    /// <summary>Returns the number of registered templates.</summary>
    public int Count => _templates.Count;

    /// <summary>Returns all registered template keys.</summary>
    public IReadOnlyCollection<string> Keys => _templates.Keys;

    /// <summary>Returns true if a template with the given key exists.</summary>
    public bool IsRegistered(string key) => _templates.ContainsKey(key);
}
