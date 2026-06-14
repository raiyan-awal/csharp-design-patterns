using CommandPattern;

namespace CommandPattern.Tests;

// ── Test double ───────────────────────────────────────────────

file sealed class TrackingCommand : ICommand
{
    public int ExecuteCount { get; private set; }
    public int UndoCount    { get; private set; }

    public void Execute() => ExecuteCount++;
    public void Undo()    => UndoCount++;
}

// ── Document ──────────────────────────────────────────────────

public class DocumentTests
{
    [Fact]
    public void Insert_AtStart_PrependsText()
    {
        var doc = new Document("world");
        doc.Insert(0, "Hello ");
        Assert.Equal("Hello world", doc.Content);
    }

    [Fact]
    public void Insert_AtEnd_AppendsText()
    {
        var doc = new Document("Hello");
        doc.Insert(5, "!");
        Assert.Equal("Hello!", doc.Content);
    }

    [Fact]
    public void Insert_InMiddle_SplicesText()
    {
        var doc = new Document("Helo");
        doc.Insert(3, "l");
        Assert.Equal("Hello", doc.Content);
    }

    [Fact]
    public void Delete_RemovesCorrectSpan()
    {
        var doc = new Document("Hello world");
        doc.Delete(5, 6);
        Assert.Equal("Hello", doc.Content);
    }

    [Fact]
    public void Insert_NegativePosition_Throws()
    {
        var doc = new Document("abc");
        Assert.Throws<ArgumentOutOfRangeException>(() => doc.Insert(-1, "x"));
    }

    [Fact]
    public void Delete_BeyondLength_Throws()
    {
        var doc = new Document("abc");
        Assert.Throws<ArgumentOutOfRangeException>(() => doc.Delete(2, 5));
    }
}

// ── InsertTextCommand ─────────────────────────────────────────

public class InsertTextCommandTests
{
    [Fact]
    public void Execute_InsertsTextAtPosition()
    {
        var doc = new Document("Hello");
        var cmd = new InsertTextCommand(doc, 5, " world");
        cmd.Execute();
        Assert.Equal("Hello world", doc.Content);
    }

    [Fact]
    public void Undo_AfterExecute_RestoresOriginalContent()
    {
        var doc     = new Document("Hello");
        var cmd     = new InsertTextCommand(doc, 5, " world");
        var before  = doc.Content;
        cmd.Execute();
        cmd.Undo();
        Assert.Equal(before, doc.Content);
    }

    [Fact]
    public void Execute_Undo_Execute_ProducesCorrectContent()
    {
        var doc = new Document("Hi");
        var cmd = new InsertTextCommand(doc, 2, "!");
        cmd.Execute();
        cmd.Undo();
        cmd.Execute();
        Assert.Equal("Hi!", doc.Content);
    }
}

// ── DeleteTextCommand ─────────────────────────────────────────

public class DeleteTextCommandTests
{
    [Fact]
    public void Execute_RemovesCorrectSpan()
    {
        var doc = new Document("Hello world");
        var cmd = new DeleteTextCommand(doc, 5, 6);
        cmd.Execute();
        Assert.Equal("Hello", doc.Content);
    }

    [Fact]
    public void Undo_AfterExecute_RestoresDeletedText()
    {
        var doc    = new Document("Hello world");
        var cmd    = new DeleteTextCommand(doc, 5, 6);
        var before = doc.Content;
        cmd.Execute();
        cmd.Undo();
        Assert.Equal(before, doc.Content);
    }

    [Fact]
    public void Undo_WithoutExecute_ThrowsInvalidOperationException()
    {
        var doc = new Document("Hello");
        var cmd = new DeleteTextCommand(doc, 0, 3);
        Assert.Throws<InvalidOperationException>(() => cmd.Undo());
    }

    [Fact]
    public void Execute_CapturesTextAtExecuteTime_NotBeforeOrAfter()
    {
        // Verifies that _deletedText is the content that existed when Execute ran
        var doc = new Document("Hello world");
        var cmd = new DeleteTextCommand(doc, 6, 5);  // captures "world" at Execute time

        cmd.Execute();
        Assert.Equal("Hello ", doc.Content);

        cmd.Undo();
        Assert.Equal("Hello world", doc.Content);  // "world" was correctly captured
    }
}

// ── MacroCommand ──────────────────────────────────────────────

public class MacroCommandTests
{
    [Fact]
    public void Execute_RunsAllCommandsInOrder()
    {
        var doc   = new Document("abc");
        var macro = new MacroCommand(
        [
            new InsertTextCommand(doc, 0, "1"),
            new InsertTextCommand(doc, 1, "2"),
        ]);

        macro.Execute();

        Assert.Equal("12abc", doc.Content);
    }

    [Fact]
    public void Undo_RunsCommandsInReverseOrder()
    {
        var doc    = new Document("abc");
        var before = doc.Content;
        var macro  = new MacroCommand(
        [
            new InsertTextCommand(doc, 0, "1"),
            new InsertTextCommand(doc, 1, "2"),
        ]);

        macro.Execute();
        macro.Undo();

        Assert.Equal(before, doc.Content);
    }

    [Fact]
    public void Execute_CallsEachCommandOnce()
    {
        var c1    = new TrackingCommand();
        var c2    = new TrackingCommand();
        var macro = new MacroCommand([c1, c2]);

        macro.Execute();

        Assert.Equal(1, c1.ExecuteCount);
        Assert.Equal(1, c2.ExecuteCount);
    }

    [Fact]
    public void Undo_CallsEachCommandUndoOnce()
    {
        var c1    = new TrackingCommand();
        var c2    = new TrackingCommand();
        var macro = new MacroCommand([c1, c2]);

        macro.Execute();
        macro.Undo();

        Assert.Equal(1, c1.UndoCount);
        Assert.Equal(1, c2.UndoCount);
    }

    [Fact]
    public void EmptyMacro_ExecuteAndUndo_DoNotThrow()
    {
        var macro = new MacroCommand([]);
        var exEx  = Record.Exception(() => macro.Execute());
        var exUn  = Record.Exception(() => macro.Undo());
        Assert.Null(exEx);
        Assert.Null(exUn);
    }
}

// ── CommandHistory ────────────────────────────────────────────

public class CommandHistoryTests
{
    [Fact]
    public void InitialState_CannotUndoOrRedo()
    {
        var history = new CommandHistory();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Execute_CallsCommandExecute()
    {
        var cmd     = new TrackingCommand();
        var history = new CommandHistory();
        history.Execute(cmd);
        Assert.Equal(1, cmd.ExecuteCount);
    }

    [Fact]
    public void Execute_EnablesUndo()
    {
        var history = new CommandHistory();
        history.Execute(new TrackingCommand());
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void Undo_CallsCommandUndo()
    {
        var cmd     = new TrackingCommand();
        var history = new CommandHistory();
        history.Execute(cmd);
        history.Undo();
        Assert.Equal(1, cmd.UndoCount);
    }

    [Fact]
    public void Undo_EnablesRedo()
    {
        var history = new CommandHistory();
        history.Execute(new TrackingCommand());
        history.Undo();
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Redo_CallsCommandExecuteAgain()
    {
        var cmd     = new TrackingCommand();
        var history = new CommandHistory();
        history.Execute(cmd);
        history.Undo();
        history.Redo();
        Assert.Equal(2, cmd.ExecuteCount);
    }

    [Fact]
    public void Execute_AfterUndo_ClearsRedoStack()
    {
        var history = new CommandHistory();
        history.Execute(new TrackingCommand());
        history.Undo();
        Assert.True(history.CanRedo);

        history.Execute(new TrackingCommand());
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Undo_WhenEmpty_DoesNotThrow()
    {
        var history = new CommandHistory();
        var ex      = Record.Exception(() => history.Undo());
        Assert.Null(ex);
    }

    [Fact]
    public void Redo_WhenEmpty_DoesNotThrow()
    {
        var history = new CommandHistory();
        var ex      = Record.Exception(() => history.Redo());
        Assert.Null(ex);
    }

    [Fact]
    public void MultipleUndos_RestoreStateInOrder()
    {
        var doc     = new Document();
        var history = new CommandHistory();

        history.Execute(new InsertTextCommand(doc, 0, "A"));
        history.Execute(new InsertTextCommand(doc, 1, "B"));
        history.Execute(new InsertTextCommand(doc, 2, "C"));
        Assert.Equal("ABC", doc.Content);

        history.Undo();
        Assert.Equal("AB", doc.Content);

        history.Undo();
        Assert.Equal("A", doc.Content);

        history.Undo();
        Assert.Equal("", doc.Content);

        Assert.False(history.CanUndo);
    }

    [Fact]
    public void UndoCount_And_RedoCount_TrackStacks()
    {
        var history = new CommandHistory();

        history.Execute(new TrackingCommand());
        history.Execute(new TrackingCommand());
        Assert.Equal(2, history.UndoCount);
        Assert.Equal(0, history.RedoCount);

        history.Undo();
        Assert.Equal(1, history.UndoCount);
        Assert.Equal(1, history.RedoCount);
    }
}
