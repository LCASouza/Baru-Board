using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Editing;

/// <summary>
/// Editor-level operations driven by shortcuts and menus rather than pointer
/// interaction. Each one reports whether it changed anything, so callers only
/// repaint when needed.
/// </summary>
public static class EditingOperations
{
    public static bool Undo(CommandHistory history, BoardDocument document, SelectionState selection)
    {
        if (!history.Undo())
            return false;

        selection.RemoveMissing(document);
        return true;
    }

    public static bool Redo(CommandHistory history, BoardDocument document, SelectionState selection)
    {
        if (!history.Redo())
            return false;

        selection.RemoveMissing(document);
        return true;
    }

    public static bool SelectAll(BoardDocument document, SelectionState selection)
    {
        if (document.Elements.Count == 0)
            return false;

        selection.SelectMany(document.Elements);
        return true;
    }

    public static bool ClearSelection(SelectionState selection)
    {
        if (selection.IsEmpty)
            return false;

        selection.Clear();
        return true;
    }

    public static bool Copy(BoardDocument document, SelectionState selection, BoardClipboard clipboard)
    {
        if (selection.IsEmpty)
            return false;

        clipboard.Copy(selection.Elements, document);
        return true;
    }

    public static bool Paste(
        BoardDocument document, SelectionState selection, BoardClipboard clipboard, CommandHistory history)
    {
        if (clipboard.CreatePasteCopy() is not { } content)
            return false;

        // Assets travel with the elements so pasting into another board works;
        // content addressing makes this a no-op when the board already has them.
        foreach (var asset in content.Assets)
            document.AddAsset(asset);

        AddElements(document, selection, history, content.Elements);
        return true;
    }

    public static bool Duplicate(BoardDocument document, SelectionState selection, CommandHistory history)
    {
        if (selection.IsEmpty)
            return false;

        var copies = new List<BoardElement>(selection.Count);
        foreach (var element in selection.Elements)
        {
            var copy = element.CreateCopy();
            copy.MoveTo(new PointD(
                copy.Bounds.X + EditingDefaults.PasteOffset,
                copy.Bounds.Y + EditingDefaults.PasteOffset));
            copies.Add(copy);
        }

        AddElements(document, selection, history, copies);
        return true;
    }

    public static bool Align(SelectionState selection, CommandHistory history, AlignmentMode mode) =>
        ApplyMoves(history, ElementArrangement.Align(selection.Elements, mode));

    public static bool Distribute(SelectionState selection, CommandHistory history, DistributionMode mode) =>
        ApplyMoves(history, ElementArrangement.Distribute(selection.Elements, mode));

    private static bool ApplyMoves(CommandHistory history, IReadOnlyList<ElementMove> moves)
    {
        if (moves.Count == 0)
            return false;

        history.Execute(new MoveElementsCommand(moves));
        return true;
    }

    private static void AddElements(
        BoardDocument document, SelectionState selection, CommandHistory history, IReadOnlyList<BoardElement> elements)
    {
        var additions = new List<AddedElement>(elements.Count);
        for (var i = 0; i < elements.Count; i++)
            additions.Add(new AddedElement(elements[i], document.Elements.Count + i));

        history.Execute(new AddElementsCommand(document, additions));
        selection.SelectMany(elements);
    }
}
