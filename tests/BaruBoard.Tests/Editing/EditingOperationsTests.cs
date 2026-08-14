using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Editing;

public class EditingOperationsTests
{
    private const double Tolerance = 1e-9;

    private sealed record Scene(
        BoardDocument Document,
        SelectionState Selection,
        BoardClipboard Clipboard,
        CommandHistory History,
        RectangleElement Element);

    private static Scene CreateScene()
    {
        var document = new BoardDocument();
        var element = new RectangleElement(new RectD(100, 100, 80, 40));
        document.AddElement(element);
        return new Scene(document, new SelectionState(), new BoardClipboard(), new CommandHistory(), element);
    }

    [Fact]
    public void Copy_WithoutSelection_DoesNothing()
    {
        var scene = CreateScene();

        Assert.False(EditingOperations.Copy(scene.Document, scene.Selection, scene.Clipboard));
        Assert.False(scene.Clipboard.HasContent);
    }

    [Fact]
    public void Copy_DoesNotTouchTheDocument()
    {
        var scene = CreateScene();
        scene.Selection.Select(scene.Element);

        Assert.True(EditingOperations.Copy(scene.Document, scene.Selection, scene.Clipboard));
        Assert.Single(scene.Document.Elements);
        Assert.False(scene.History.CanUndo);
    }

    [Fact]
    public void Paste_WithoutClipboardContent_IsNoOp()
    {
        var scene = CreateScene();

        Assert.False(EditingOperations.Paste(scene.Document, scene.Selection, scene.Clipboard, scene.History));
        Assert.Single(scene.Document.Elements);
        Assert.False(scene.History.CanUndo);
    }

    [Fact]
    public void Paste_AddsOffsetCopyAndSelectsIt()
    {
        var scene = CreateScene();
        scene.Selection.Select(scene.Element);
        EditingOperations.Copy(scene.Document, scene.Selection, scene.Clipboard);

        EditingOperations.Paste(scene.Document, scene.Selection, scene.Clipboard, scene.History);

        Assert.Equal(2, scene.Document.Elements.Count);
        var pasted = scene.Document.Elements[1];
        Assert.NotSame(scene.Element, pasted);
        Assert.Same(pasted, scene.Selection.Primary);
        Assert.Equal(100 + EditingDefaults.PasteOffset, pasted.Bounds.X, Tolerance);
        Assert.Equal(100 + EditingDefaults.PasteOffset, pasted.Bounds.Y, Tolerance);
    }

    [Fact]
    public void RepeatedPaste_AccumulatesTheOffset()
    {
        var scene = CreateScene();
        scene.Selection.Select(scene.Element);
        EditingOperations.Copy(scene.Document, scene.Selection, scene.Clipboard);

        EditingOperations.Paste(scene.Document, scene.Selection, scene.Clipboard, scene.History);
        EditingOperations.Paste(scene.Document, scene.Selection, scene.Clipboard, scene.History);
        EditingOperations.Paste(scene.Document, scene.Selection, scene.Clipboard, scene.History);

        Assert.Equal(100 + EditingDefaults.PasteOffset, scene.Document.Elements[1].Bounds.X, Tolerance);
        Assert.Equal(100 + EditingDefaults.PasteOffset * 2, scene.Document.Elements[2].Bounds.X, Tolerance);
        Assert.Equal(100 + EditingDefaults.PasteOffset * 3, scene.Document.Elements[3].Bounds.X, Tolerance);
    }

    [Fact]
    public void NewCopy_RestartsTheOffsetSequence()
    {
        var scene = CreateScene();
        scene.Selection.Select(scene.Element);
        EditingOperations.Copy(scene.Document, scene.Selection, scene.Clipboard);
        EditingOperations.Paste(scene.Document, scene.Selection, scene.Clipboard, scene.History);
        EditingOperations.Paste(scene.Document, scene.Selection, scene.Clipboard, scene.History);

        var other = new EllipseElement(new RectD(500, 500, 40, 40));
        scene.Document.AddElement(other);
        scene.Selection.Select(other);
        EditingOperations.Copy(scene.Document, scene.Selection, scene.Clipboard);
        EditingOperations.Paste(scene.Document, scene.Selection, scene.Clipboard, scene.History);

        var pasted = scene.Document.Elements[^1];
        Assert.IsType<EllipseElement>(pasted);
        Assert.Equal(500 + EditingDefaults.PasteOffset, pasted.Bounds.X, Tolerance);
    }

    [Fact]
    public void Paste_IsUndoable()
    {
        var scene = CreateScene();
        scene.Selection.Select(scene.Element);
        EditingOperations.Copy(scene.Document, scene.Selection, scene.Clipboard);
        EditingOperations.Paste(scene.Document, scene.Selection, scene.Clipboard, scene.History);

        EditingOperations.Undo(scene.History, scene.Document, scene.Selection);

        Assert.Single(scene.Document.Elements);
        Assert.Null(scene.Selection.Primary);
    }

    [Fact]
    public void Duplicate_DoesNotUseOrChangeTheClipboard()
    {
        var scene = CreateScene();
        scene.Selection.Select(scene.Element);

        Assert.True(EditingOperations.Duplicate(scene.Document, scene.Selection, scene.History));

        Assert.False(scene.Clipboard.HasContent);
        Assert.Equal(2, scene.Document.Elements.Count);
        var duplicate = scene.Document.Elements[1];
        Assert.Same(duplicate, scene.Selection.Primary);
        Assert.Equal(100 + EditingDefaults.PasteOffset, duplicate.Bounds.X, Tolerance);
    }

    [Fact]
    public void Duplicate_WithoutSelection_IsNoOp()
    {
        var scene = CreateScene();

        Assert.False(EditingOperations.Duplicate(scene.Document, scene.Selection, scene.History));
        Assert.Single(scene.Document.Elements);
    }

    [Fact]
    public void Undo_ClearsSelectionWhenTheElementIsGone()
    {
        var scene = CreateScene();
        scene.Selection.Select(scene.Element);
        EditingOperations.Duplicate(scene.Document, scene.Selection, scene.History);
        var duplicate = scene.Selection.Primary;

        EditingOperations.Undo(scene.History, scene.Document, scene.Selection);

        Assert.NotNull(duplicate);
        Assert.Null(scene.Selection.Primary);
    }

    [Fact]
    public void Undo_KeepsSelectionWhenTheElementSurvives()
    {
        var scene = CreateScene();
        scene.Selection.Select(scene.Element);
        scene.History.Execute(new MoveElementCommand(scene.Element, new PointD(100, 100), new PointD(300, 300)));

        EditingOperations.Undo(scene.History, scene.Document, scene.Selection);

        Assert.Same(scene.Element, scene.Selection.Primary);
        Assert.Equal(100, scene.Element.Bounds.X, Tolerance);
    }

    [Fact]
    public void UndoAndRedo_OnEmptyHistory_ReportNoChange()
    {
        var scene = CreateScene();

        Assert.False(EditingOperations.Undo(scene.History, scene.Document, scene.Selection));
        Assert.False(EditingOperations.Redo(scene.History, scene.Document, scene.Selection));
    }
}
