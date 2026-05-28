namespace PrototypePattern;

/// <summary>
/// Combat statistics — a nested reference type.
///
/// Used to demonstrate that a deep clone must recursively clone nested objects,
/// not just the top-level fields.
/// </summary>
public sealed class CombatStats
{
    public int  Armor    { get; set; }
    public int  Speed    { get; set; }
    public int  XpReward { get; set; }

    /// <summary>Creates an independent copy of these stats.</summary>
    public CombatStats Clone() => new()
    {
        Armor    = Armor,
        Speed    = Speed,
        XpReward = XpReward
    };

    public override string ToString() =>
        $"Armor={Armor}, Speed={Speed}, XP={XpReward}";
}

/// <summary>
/// CONCRETE PROTOTYPE — an enemy template that knows how to clone itself.
///
/// Fields intentionally mix value types and reference types so the
/// shallow-vs-deep distinction is visible and testable:
///
///   Value types  (int, string*) ─── copied by MemberwiseClone → safe
///   List&lt;string&gt; Equipment      ─── shared in shallow clone → dangerous
///   CombatStats  Stats          ─── shared in shallow clone → dangerous
///
/// (*) string is technically a reference type but is immutable, so sharing
///     it between instances is always safe — mutations create a new string.
/// </summary>
public sealed class Enemy : IPrototype<Enemy>
{
    // ── Identity ─────────────────────────────────────────────────────────────
    public string Name    { get; set; } = string.Empty;
    public string Faction { get; set; } = string.Empty;

    // ── Value-type stats (safe to share) ────────────────────────────────────
    public int Health { get; set; }
    public int Damage { get; set; }

    // ── Reference-type fields (dangerous to share) ──────────────────────────

    /// <summary>
    /// Mutable list — the key demonstration of the shallow-copy danger.
    /// If two enemies share the same List instance, equipping one changes both.
    /// </summary>
    public List<string> Equipment { get; set; } = [];

    /// <summary>
    /// Nested object — must be cloned separately in DeepClone().
    /// </summary>
    public CombatStats Stats { get; set; } = new();

    // ── Prototype implementation ─────────────────────────────────────────────

    /// <summary>
    /// Shallow clone via MemberwiseClone().
    ///
    /// MemberwiseClone copies every field bit-for-bit:
    ///   • int/float fields → new copy of the value ✓
    ///   • string fields    → same reference, but strings are immutable ✓
    ///   • List&lt;string&gt;     → same reference → SHARED LIST ✗
    ///   • CombatStats      → same reference → SHARED STATS OBJECT ✗
    /// </summary>
    public Enemy ShallowClone() => (Enemy)MemberwiseClone();

    /// <summary>
    /// Deep clone — every nested mutable object gets its own independent copy.
    ///
    /// Strategy:
    ///   1. Start with MemberwiseClone() to copy all value types in one call
    ///   2. Replace each reference-type field with a freshly constructed copy
    /// </summary>
    public Enemy DeepClone()
    {
        // Step 1 — copy all value types cheaply
        var clone = (Enemy)MemberwiseClone();

        // Step 2 — replace shared references with independent copies
        clone.Equipment = new List<string>(Equipment); // new list, same string values
        clone.Stats     = Stats.Clone();               // new CombatStats object

        return clone;
    }

    public override string ToString() =>
        $"{Name} ({Faction}) | HP:{Health} DMG:{Damage} | " +
        $"Equipment:[{string.Join(", ", Equipment)}] | Stats:[{Stats}]";
}
