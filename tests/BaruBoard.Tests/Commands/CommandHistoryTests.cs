using BaruBoard.Core.Commands;

namespace BaruBoard.Tests.Commands;

public class CommandHistoryTests
{
    private sealed class TrackingCommand : IUndoableCommand
    {
        private readonly List<string> _log;
        private readonly string _name;

        public TrackingCommand(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public int ExecuteCount { get; private set; }

        public void Execute()
        {
            ExecuteCount++;
            _log.Add($"execute:{_name}");
        }

        public void Undo() => _log.Add($"undo:{_name}");
    }

    [Fact]
    public void Record_DoesNotExecuteTheCommand()
    {
        var log = new List<string>();
        var history = new CommandHistory();
        var command = new TrackingCommand(log, "a");

        history.Record(command);

        Assert.Equal(0, command.ExecuteCount);
        Assert.Empty(log);
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void Execute_AppliesAndRegistersTheCommand()
    {
        var log = new List<string>();
        var history = new CommandHistory();

        history.Execute(new TrackingCommand(log, "a"));

        Assert.Equal(["execute:a"], log);
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void Undo_WalksBackInReverseOrder()
    {
        var log = new List<string>();
        var history = new CommandHistory();
        history.Record(new TrackingCommand(log, "a"));
        history.Record(new TrackingCommand(log, "b"));

        history.Undo();
        history.Undo();

        Assert.Equal(["undo:b", "undo:a"], log);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Redo_ReappliesInOriginalOrder()
    {
        var log = new List<string>();
        var history = new CommandHistory();
        history.Record(new TrackingCommand(log, "a"));
        history.Record(new TrackingCommand(log, "b"));
        history.Undo();
        history.Undo();
        log.Clear();

        history.Redo();
        history.Redo();

        Assert.Equal(["execute:a", "execute:b"], log);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void NewCommand_ClearsTheRedoStack()
    {
        var log = new List<string>();
        var history = new CommandHistory();
        history.Record(new TrackingCommand(log, "a"));
        history.Undo();
        Assert.True(history.CanRedo);

        history.Record(new TrackingCommand(log, "b"));

        Assert.False(history.CanRedo);
    }

    [Fact]
    public void UndoAndRedo_OnEmptyHistory_AreNoOps()
    {
        var history = new CommandHistory();

        Assert.False(history.Undo());
        Assert.False(history.Redo());
    }

    [Fact]
    public void Capacity_DiscardsTheOldestEntry()
    {
        var log = new List<string>();
        var history = new CommandHistory(capacity: 2);
        history.Record(new TrackingCommand(log, "a"));
        history.Record(new TrackingCommand(log, "b"));
        history.Record(new TrackingCommand(log, "c"));

        Assert.Equal(2, history.Count);
        history.Undo();
        history.Undo();

        Assert.Equal(["undo:c", "undo:b"], log);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Clear_EmptiesBothStacks()
    {
        var log = new List<string>();
        var history = new CommandHistory();
        history.Record(new TrackingCommand(log, "a"));
        history.Undo();

        history.Clear();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CommandHistory(0));
    }
}
