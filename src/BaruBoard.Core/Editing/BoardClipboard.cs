using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Editing;

public sealed record ClipboardContent(IReadOnlyList<BoardElement> Elements, IReadOnlyList<BoardAsset> Assets);

/// <summary>
/// Application-local clipboard holding detached copies of a selection together
/// with the assets they cannot live without, so pasting into another board works.
/// The system clipboard is out of scope while interoperability with external
/// formats is not a goal.
/// </summary>
public sealed class BoardClipboard
{
    private ClipboardContent? _content;
    private int _pasteCount;

    public bool HasContent => _content is not null;

    public void Copy(IReadOnlyList<BoardElement> elements, BoardDocument source)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(source);

        if (elements.Count == 0)
            return;

        var copies = new List<BoardElement>(elements.Count);
        var assets = new List<BoardAsset>();
        var seenAssets = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in elements)
        {
            copies.Add(element.CreateCopy());
            foreach (var assetId in element.RequiredAssetIds)
            {
                if (seenAssets.Add(assetId) && source.TryGetAsset(assetId, out var asset))
                    assets.Add(asset);
            }
        }

        _content = new ClipboardContent(copies, assets);
        _pasteCount = 0;
    }

    /// <summary>
    /// Fresh copies displaced as a group, so repeated pastes step away from the
    /// original while the relative arrangement is preserved.
    /// </summary>
    public ClipboardContent? CreatePasteCopy()
    {
        if (_content is null)
            return null;

        _pasteCount++;
        var offset = EditingDefaults.PasteOffset * _pasteCount;

        var copies = new List<BoardElement>(_content.Elements.Count);
        foreach (var element in _content.Elements)
        {
            var copy = element.CreateCopy();
            copy.MoveTo(new PointD(copy.Bounds.X + offset, copy.Bounds.Y + offset));
            copies.Add(copy);
        }

        return new ClipboardContent(copies, _content.Assets);
    }
}
