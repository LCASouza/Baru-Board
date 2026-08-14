using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Geometry;
using BaruBoard.Storage.Files;

namespace BaruBoard.Tests.Storage;

public class BoardFileStateTests
{
    private static (CommandHistory History, BoardDocument Document) CreateBoard()
    {
        var document = new BoardDocument();
        return (new CommandHistory(), document);
    }

    private static void ApplyChange(CommandHistory history, BoardDocument document)
    {
        var element = new RectangleElement(new RectD(0, 0, 10, 10));
        document.AddElement(element);
        history.Record(new AddElementCommand(document, element, document.IndexOf(element)));
    }

    [Fact]
    public void NewDocument_StartsClean()
    {
        var (history, _) = CreateBoard();
        var state = new BoardFileState();

        state.MarkNewDocument();

        Assert.False(state.IsDirty(history));
        Assert.Null(state.FilePath);
    }

    [Fact]
    public void OpenedDocument_StartsClean()
    {
        var (history, _) = CreateBoard();
        var state = new BoardFileState();

        state.MarkOpened("/tmp/board.baru");

        Assert.False(state.IsDirty(history));
        Assert.Equal("/tmp/board.baru", state.FilePath);
    }

    [Fact]
    public void RecoveredDocument_StartsDirtyEvenWithEmptyHistory()
    {
        var (history, _) = CreateBoard();
        var state = new BoardFileState();

        state.MarkRecovered("/tmp/board.baru");

        Assert.Equal(0, history.CurrentStateId);
        Assert.True(state.IsDirty(history));
        Assert.Equal("/tmp/board.baru", state.FilePath);
    }

    [Fact]
    public void RecoveredUnsavedDocument_KeepsNoPathAndStaysDirty()
    {
        var (history, _) = CreateBoard();
        var state = new BoardFileState();

        state.MarkRecovered(null);

        Assert.Null(state.FilePath);
        Assert.True(state.IsDirty(history));
    }

    [Fact]
    public void ChangeAfterSave_MakesItDirty()
    {
        var (history, document) = CreateBoard();
        var state = new BoardFileState();
        state.MarkNewDocument();

        ApplyChange(history, document);
        Assert.True(state.IsDirty(history));

        state.MarkSaved("/tmp/board.baru", history.CurrentStateId);
        Assert.False(state.IsDirty(history));

        ApplyChange(history, document);
        Assert.True(state.IsDirty(history));
    }

    [Fact]
    public void UndoBackToTheSavedState_IsCleanAgain()
    {
        var (history, document) = CreateBoard();
        var state = new BoardFileState();
        ApplyChange(history, document);
        state.MarkSaved("/tmp/board.baru", history.CurrentStateId);

        ApplyChange(history, document);
        history.Undo();

        Assert.False(state.IsDirty(history));
    }

    [Fact]
    public void UndoPastTheSavedState_IsDirty()
    {
        var (history, document) = CreateBoard();
        var state = new BoardFileState();
        ApplyChange(history, document);
        state.MarkSaved("/tmp/board.baru", history.CurrentStateId);

        history.Undo();

        Assert.True(state.IsDirty(history));
    }

    [Fact]
    public void BranchAfterUndo_IsDirtyEvenWithTheSameHistoryDepth()
    {
        var (history, document) = CreateBoard();
        var state = new BoardFileState();
        ApplyChange(history, document);
        state.MarkSaved("/tmp/board.baru", history.CurrentStateId);

        history.Undo();
        ApplyChange(history, document);

        Assert.Equal(1, history.Count);
        Assert.True(state.IsDirty(history));
    }

    [Fact]
    public void RedoBackToTheSavedState_IsCleanAgain()
    {
        var (history, document) = CreateBoard();
        var state = new BoardFileState();
        ApplyChange(history, document);
        state.MarkSaved("/tmp/board.baru", history.CurrentStateId);
        history.Undo();

        history.Redo();

        Assert.False(state.IsDirty(history));
    }

    [Fact]
    public void SaveAfterRecovery_ClearsTheDirtyState()
    {
        var (history, document) = CreateBoard();
        var state = new BoardFileState();
        state.MarkRecovered(null);
        ApplyChange(history, document);

        state.MarkSaved("/tmp/novo.baru", history.CurrentStateId);

        Assert.False(state.IsDirty(history));
        Assert.Equal("/tmp/novo.baru", state.FilePath);
    }
}
