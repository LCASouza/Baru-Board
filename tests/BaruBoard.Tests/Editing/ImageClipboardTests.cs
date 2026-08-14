using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Editing;

public class ImageClipboardTests
{
    private static SelectionState CreateSelection(BoardElement element)
    {
        var selection = new SelectionState();
        selection.Select(element);
        return selection;
    }

    private static (BoardDocument Document, ImageElement Element, BoardAsset Asset) CreateBoardWithImage()
    {
        var document = new BoardDocument();
        var asset = document.AddAsset(BoardAsset.Create("imagem"u8, AssetMediaTypes.Png));
        var element = new ImageElement(new RectD(10, 20, 100, 80), asset.Id);
        document.AddElement(element);
        return (document, element, asset);
    }

    [Fact]
    public void PasteIntoAnotherDocument_BringsTheAssetAlong()
    {
        var (source, element, asset) = CreateBoardWithImage();
        var clipboard = new BoardClipboard();
        var selection = CreateSelection(element);
        EditingOperations.Copy(source, selection, clipboard);

        var target = new BoardDocument();
        var targetSelection = new SelectionState();
        var history = new CommandHistory();

        Assert.True(EditingOperations.Paste(target, targetSelection, clipboard, history));

        var pasted = Assert.IsType<ImageElement>(Assert.Single(target.Elements));
        Assert.Equal(asset.Id, pasted.AssetId);
        Assert.True(target.ContainsAsset(asset.Id));
        Assert.Equal(asset.Data.ToArray(), target.Assets.Single().Data.ToArray());
        Assert.Same(pasted, targetSelection.Primary);
    }

    [Fact]
    public void PastingTwice_ReusesTheSameAsset()
    {
        var (source, element, _) = CreateBoardWithImage();
        var clipboard = new BoardClipboard();
        EditingOperations.Copy(source, CreateSelection(element), clipboard);

        var target = new BoardDocument();
        var selection = new SelectionState();
        var history = new CommandHistory();
        EditingOperations.Paste(target, selection, clipboard, history);
        EditingOperations.Paste(target, selection, clipboard, history);

        Assert.Equal(2, target.Elements.Count);
        Assert.Single(target.Assets);
    }

    [Fact]
    public void DuplicateWithinTheDocument_SharesTheAssetId()
    {
        var (document, element, asset) = CreateBoardWithImage();
        var selection = CreateSelection(element);
        var history = new CommandHistory();

        Assert.True(EditingOperations.Duplicate(document, selection, history));

        var duplicate = Assert.IsType<ImageElement>(document.Elements[1]);
        Assert.Equal(asset.Id, duplicate.AssetId);
        Assert.Single(document.Assets);
    }

    [Fact]
    public void UndoingAPaste_LeavesTheAssetBehindForRedo()
    {
        var (source, element, asset) = CreateBoardWithImage();
        var clipboard = new BoardClipboard();
        EditingOperations.Copy(source, CreateSelection(element), clipboard);

        var target = new BoardDocument();
        var selection = new SelectionState();
        var history = new CommandHistory();
        EditingOperations.Paste(target, selection, clipboard, history);

        EditingOperations.Undo(history, target, selection);

        Assert.Empty(target.Elements);
        Assert.True(target.ContainsAsset(asset.Id));
        Assert.Empty(target.GetReferencedAssets());

        history.Redo();
        Assert.Single(target.GetReferencedAssets());
    }

    [Fact]
    public void CopyingANonImageElement_CarriesNoAssets()
    {
        var document = new BoardDocument();
        var element = new RectangleElement(new RectD(0, 0, 10, 10));
        document.AddElement(element);
        var clipboard = new BoardClipboard();

        EditingOperations.Copy(document, CreateSelection(element), clipboard);
        var target = new BoardDocument();
        EditingOperations.Paste(target, new SelectionState(), clipboard, new CommandHistory());

        Assert.Empty(target.Assets);
        Assert.Single(target.Elements);
    }
}
