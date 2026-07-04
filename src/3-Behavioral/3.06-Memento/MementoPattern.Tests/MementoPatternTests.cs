using MementoPattern;

public class MementoPatternTests
{
    // ── Save ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Save_CapturesAllCurrentState()
    {
        var hero = new GameCharacter("Aria", health: 80, mana: 60);
        hero.MoveTo(3, 7);
        hero.PickUp("Sword");

        var m = hero.Save("checkpoint");

        Assert.Equal(80,         m.Health);
        Assert.Equal(60,         m.Mana);
        Assert.Equal(new Position(3, 7), m.Position);
        Assert.Equal(1,          m.Level);
        Assert.Equal(["Sword"],  m.Inventory);
        Assert.Equal("checkpoint", m.Label);
    }

    [Fact]
    public void Save_DeepCopiesInventory_LiveChangeDoesNotAffectMemento()
    {
        var hero = new GameCharacter("Aria");
        hero.PickUp("Shield");

        var m = hero.Save("snap");
        hero.PickUp("Axe");   // add after save
        hero.Drop("Shield");  // remove after save

        Assert.Equal(["Shield"], m.Inventory);
    }

    [Fact]
    public void Save_RecordsTimestamp()
    {
        var before = DateTime.Now;
        var m = new GameCharacter("Aria").Save();
        var after = DateTime.Now;

        Assert.InRange(m.SavedAt, before, after);
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    [Fact]
    public void Restore_ReturnsCharacterToPreviousState()
    {
        var hero = new GameCharacter("Aria");
        var m = hero.Save("before");

        hero.TakeDamage(50);
        hero.MoveTo(5, 5);
        hero.PickUp("Key");

        hero.Restore(m);

        Assert.Equal(100,              hero.Health);
        Assert.Equal(new Position(0, 0), hero.Position);
        Assert.Empty(hero.Inventory);
    }

    [Fact]
    public void Restore_DeepCopiesInventory_ChangingCharacterAfterRestoreDoesNotAffectMemento()
    {
        var hero = new GameCharacter("Aria");
        hero.PickUp("Staff");
        var m = hero.Save("snap");

        hero.Restore(m);
        hero.PickUp("Wand");   // add after restore

        Assert.Equal(["Staff"], m.Inventory); // memento unchanged
    }

    [Fact]
    public void Restore_PreservesName()
    {
        var hero = new GameCharacter("Aria");
        var m = hero.Save();
        hero.Restore(m);
        Assert.Equal("Aria", hero.Name);
    }

    [Fact]
    public void Restore_FromLevelUpSnapshot_ReturnsCorrectLevel()
    {
        var hero = new GameCharacter("Aria");
        hero.LevelUp();
        var m = hero.Save("level 2");

        hero.LevelUp(); // advance further

        hero.Restore(m);
        Assert.Equal(2, hero.Level);
    }

    // ── Multiple snapshots ────────────────────────────────────────────────────

    [Fact]
    public void MultipleSnapshots_AreIndependent()
    {
        var hero = new GameCharacter("Aria");

        var m1 = hero.Save("start");        // HP:100
        hero.TakeDamage(30);
        var m2 = hero.Save("after fight");  // HP:70
        hero.TakeDamage(20);                // HP:50

        hero.Restore(m2);
        Assert.Equal(70, hero.Health);

        hero.Restore(m1);
        Assert.Equal(100, hero.Health);
    }

    [Fact]
    public void Restore_DoesNotAffectOtherCharacters()
    {
        var hero    = new GameCharacter("Aria");
        var villain = new GameCharacter("Zar");

        var m = hero.Save("snap");
        villain.TakeDamage(50);

        hero.Restore(m);

        Assert.Equal(50, villain.Health); // villain unaffected
    }

    // ── CheckpointHistory (Caretaker) ─────────────────────────────────────────

    [Fact]
    public void CheckpointHistory_CanUndo_FalseWhenEmpty()
    {
        Assert.False(new CheckpointHistory().CanUndo);
    }

    [Fact]
    public void CheckpointHistory_CanUndo_TrueAfterPush()
    {
        var history = new CheckpointHistory();
        history.Push(new GameCharacter("Aria").Save());
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void CheckpointHistory_PopReturnsLastPushed()
    {
        var history = new CheckpointHistory();
        var hero = new GameCharacter("Aria");

        history.Push(hero.Save("first"));
        hero.TakeDamage(10);
        history.Push(hero.Save("second"));

        Assert.Equal("second", history.Pop().Label);
        Assert.Equal("first",  history.Pop().Label);
    }

    [Fact]
    public void CheckpointHistory_Count_TracksCorrectly()
    {
        var history = new CheckpointHistory();
        var hero = new GameCharacter("Aria");

        history.Push(hero.Save());
        history.Push(hero.Save());
        Assert.Equal(2, history.Count);

        history.Pop();
        Assert.Equal(1, history.Count);
    }

    [Fact]
    public void CheckpointHistory_All_ReturnsCopyOfStack()
    {
        var history = new CheckpointHistory();
        var hero = new GameCharacter("Aria");
        history.Push(hero.Save("a"));
        history.Push(hero.Save("b"));

        var all = history.All;

        Assert.Equal(2, all.Count);
        Assert.Equal(2, history.Count); // Pop wasn't called — stack unchanged
    }
}
