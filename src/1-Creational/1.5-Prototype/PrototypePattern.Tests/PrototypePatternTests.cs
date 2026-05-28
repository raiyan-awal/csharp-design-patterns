using PrototypePattern;

namespace PrototypePattern.Tests;

/// <summary>
/// Unit tests for the Prototype pattern implementation.
///
/// These tests verify that:
/// 1. ShallowClone produces a distinct instance
/// 2. ShallowClone copies value-type fields correctly
/// 3. ShallowClone SHARES reference-type fields (the defining behaviour)
/// 4. DeepClone produces a distinct instance
/// 5. DeepClone copies value-type fields correctly
/// 6. DeepClone creates NEW objects for every reference-type field
/// 7. Mutations on a deep clone never affect the original
/// 8. The Registry hands out independent deep clones on every Spawn()
/// 9. The Registry protects its internal templates from external mutation
/// 10. The Registry throws a meaningful error for unknown keys
/// </summary>
public class PrototypePatternTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private static Enemy CreateGoblin() => new()
    {
        Name      = "Goblin",
        Faction   = "Dark Horde",
        Health    = 80,
        Damage    = 12,
        Equipment = ["Rusty Dagger", "Leather Vest"],
        Stats     = new CombatStats { Armor = 5, Speed = 8, XpReward = 25 }
    };

    // ─────────────────────────────────────────────────────────────────────────
    // SHALLOW CLONE — identity
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShallowClone_ReturnsDistinctInstance()
    {
        var original = CreateGoblin();
        var clone    = original.ShallowClone();

        Assert.NotSame(original, clone);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SHALLOW CLONE — value-type fields are copied
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShallowClone_CopiesHealth()
    {
        var original = CreateGoblin();
        var clone    = original.ShallowClone();

        Assert.Equal(original.Health, clone.Health);
    }

    [Fact]
    public void ShallowClone_CopiesDamage()
    {
        var original = CreateGoblin();
        var clone    = original.ShallowClone();

        Assert.Equal(original.Damage, clone.Damage);
    }

    [Fact]
    public void ShallowClone_CopiesName()
    {
        var original = CreateGoblin();
        var clone    = original.ShallowClone();

        Assert.Equal(original.Name, clone.Name);
    }

    [Fact]
    public void ShallowClone_ValueTypeFields_AreIndependent()
    {
        // Changing an int field on the clone must NOT affect the original
        var original = CreateGoblin();
        var clone    = original.ShallowClone();

        clone.Health = 999;

        Assert.Equal(80, original.Health);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SHALLOW CLONE — reference-type fields ARE shared (the known limitation)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShallowClone_Equipment_IsTheSameListInstance()
    {
        var original = CreateGoblin();
        var clone    = original.ShallowClone();

        // Both variables point to the identical List<string> object
        Assert.Same(original.Equipment, clone.Equipment);
    }

    [Fact]
    public void ShallowClone_Stats_IsTheSameCombatStatsInstance()
    {
        var original = CreateGoblin();
        var clone    = original.ShallowClone();

        Assert.Same(original.Stats, clone.Stats);
    }

    [Fact]
    public void ShallowClone_MutatingEquipmentOnClone_AlsoAffectsOriginal()
    {
        // This test documents the DANGER of shallow cloning
        var original = CreateGoblin();
        var clone    = original.ShallowClone();

        clone.Equipment.Add("Stolen Sword");

        // The original is polluted because both share the same list
        Assert.Contains("Stolen Sword", original.Equipment);
    }

    [Fact]
    public void ShallowClone_MutatingStatsOnClone_AlsoAffectsOriginal()
    {
        var original = CreateGoblin();
        var clone    = original.ShallowClone();

        clone.Stats.Armor = 999;

        Assert.Equal(999, original.Stats.Armor);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DEEP CLONE — identity
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DeepClone_ReturnsDistinctInstance()
    {
        var original = CreateGoblin();
        var clone    = original.DeepClone();

        Assert.NotSame(original, clone);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DEEP CLONE — value-type fields are copied
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DeepClone_CopiesHealth()
    {
        var original = CreateGoblin();
        var clone    = original.DeepClone();

        Assert.Equal(original.Health, clone.Health);
    }

    [Fact]
    public void DeepClone_CopiesName()
    {
        var original = CreateGoblin();
        var clone    = original.DeepClone();

        Assert.Equal(original.Name, clone.Name);
    }

    [Fact]
    public void DeepClone_CopiesAllEquipmentItems()
    {
        var original = CreateGoblin();
        var clone    = original.DeepClone();

        Assert.Equal(original.Equipment, clone.Equipment); // same contents
    }

    [Fact]
    public void DeepClone_CopiesStatsValues()
    {
        var original = CreateGoblin();
        var clone    = original.DeepClone();

        Assert.Equal(original.Stats.Armor,    clone.Stats.Armor);
        Assert.Equal(original.Stats.Speed,    clone.Stats.Speed);
        Assert.Equal(original.Stats.XpReward, clone.Stats.XpReward);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DEEP CLONE — reference-type fields are NEW independent objects
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DeepClone_Equipment_IsADifferentListInstance()
    {
        var original = CreateGoblin();
        var clone    = original.DeepClone();

        Assert.NotSame(original.Equipment, clone.Equipment);
    }

    [Fact]
    public void DeepClone_Stats_IsADifferentCombatStatsInstance()
    {
        var original = CreateGoblin();
        var clone    = original.DeepClone();

        Assert.NotSame(original.Stats, clone.Stats);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DEEP CLONE — mutations on clone never touch the original
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DeepClone_AddingEquipmentToClone_DoesNotAffectOriginal()
    {
        var original      = CreateGoblin();
        var clone         = original.DeepClone();
        var originalCount = original.Equipment.Count;

        clone.Equipment.Add("Enchanted Sword");

        Assert.Equal(originalCount, original.Equipment.Count);
        Assert.DoesNotContain("Enchanted Sword", original.Equipment);
    }

    [Fact]
    public void DeepClone_ChangingStatsOnClone_DoesNotAffectOriginal()
    {
        var original      = CreateGoblin();
        var clone         = original.DeepClone();
        var originalArmor = original.Stats.Armor;

        clone.Stats.Armor = 999;

        Assert.Equal(originalArmor, original.Stats.Armor);
    }

    [Fact]
    public void DeepClone_ChangingHealthOnClone_DoesNotAffectOriginal()
    {
        var original = CreateGoblin();
        var clone    = original.DeepClone();

        clone.Health = 1;

        Assert.Equal(80, original.Health);
    }

    [Fact]
    public void DeepClone_MultipleClones_AreAllIndependent()
    {
        var original = CreateGoblin();

        var clone1 = original.DeepClone();
        var clone2 = original.DeepClone();

        clone1.Equipment.Add("Sword");
        clone2.Equipment.Add("Shield");

        Assert.DoesNotContain("Sword",  clone2.Equipment);
        Assert.DoesNotContain("Shield", clone1.Equipment);
        Assert.DoesNotContain("Sword",  original.Equipment);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REGISTRY — basic registration and spawning
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Registry_AfterRegister_IsRegistered_ReturnsTrue()
    {
        var registry = new EnemyRegistry();
        registry.Register("goblin", CreateGoblin());

        Assert.True(registry.IsRegistered("goblin"));
    }

    [Fact]
    public void Registry_Count_ReflectsNumberOfTemplates()
    {
        var registry = new EnemyRegistry();
        registry.Register("goblin", CreateGoblin());
        registry.Register("troll",  CreateGoblin()); // reusing factory; name doesn't matter here

        Assert.Equal(2, registry.Count);
    }

    [Fact]
    public void Registry_Spawn_ReturnsDistinctInstanceFromTemplate()
    {
        var registry = new EnemyRegistry();
        var template = CreateGoblin();
        registry.Register("goblin", template);

        var spawn = registry.Spawn("goblin");

        // Must not be the same object as anything the registry holds internally
        Assert.NotSame(template, spawn);
    }

    [Fact]
    public void Registry_Spawn_ReturnsCopiedValues()
    {
        var registry = new EnemyRegistry();
        registry.Register("goblin", CreateGoblin());

        var spawn = registry.Spawn("goblin");

        Assert.Equal("Goblin",      spawn.Name);
        Assert.Equal(80,            spawn.Health);
        Assert.Equal("Dark Horde",  spawn.Faction);
    }

    [Fact]
    public void Registry_TwoSpawns_AreIndependentInstances()
    {
        var registry = new EnemyRegistry();
        registry.Register("goblin", CreateGoblin());

        var spawn1 = registry.Spawn("goblin");
        var spawn2 = registry.Spawn("goblin");

        Assert.NotSame(spawn1, spawn2);
        Assert.NotSame(spawn1.Equipment, spawn2.Equipment);
    }

    [Fact]
    public void Registry_MutatingSpawn_DoesNotAffectNextSpawn()
    {
        var registry = new EnemyRegistry();
        registry.Register("goblin", CreateGoblin());

        var spawn1 = registry.Spawn("goblin");
        spawn1.Equipment.Add("Enchanted Dagger");
        spawn1.Health = 1;

        var spawn2 = registry.Spawn("goblin");

        Assert.Equal(80, spawn2.Health);
        Assert.DoesNotContain("Enchanted Dagger", spawn2.Equipment);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REGISTRY — template protection
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Registry_MutatingOriginalAfterRegister_DoesNotAffectSpawn()
    {
        // The registry stores its own deep clone on Register(), so
        // mutating the caller's template afterwards has no effect.
        var registry = new EnemyRegistry();
        var template = CreateGoblin();
        registry.Register("goblin", template);

        template.Health = 9999;
        template.Equipment.Add("Hacked Item");

        var spawn = registry.Spawn("goblin");

        Assert.Equal(80, spawn.Health);
        Assert.DoesNotContain("Hacked Item", spawn.Equipment);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REGISTRY — error handling
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Registry_Spawn_UnknownKey_ThrowsKeyNotFoundException()
    {
        var registry = new EnemyRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.Spawn("dragon"));
    }

    [Fact]
    public void Registry_Spawn_IsCaseInsensitive()
    {
        var registry = new EnemyRegistry();
        registry.Register("goblin", CreateGoblin());

        // All three should find the same template
        var exception = Record.Exception(() =>
        {
            registry.Spawn("goblin");
            registry.Spawn("Goblin");
            registry.Spawn("GOBLIN");
        });

        Assert.Null(exception);
    }
}
