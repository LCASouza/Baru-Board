using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Boards;

public sealed class BoardDocument
{
    private readonly List<BoardElement> _elements = [];
    private readonly Dictionary<string, BoardAsset> _assets = new(StringComparer.Ordinal);

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "Untitled";

    public IReadOnlyList<BoardElement> Elements => _elements;

    public void AddElement(BoardElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _elements.Add(element);
    }

    // Position matters: draw order and hit-test tie-breaking both depend on it,
    // so undoing a removal has to put the element back where it was.
    public void InsertElement(int index, BoardElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _elements.Count);
        _elements.Insert(index, element);
    }

    public bool RemoveElement(BoardElement element) => _elements.Remove(element);

    public int IndexOf(BoardElement element) => _elements.IndexOf(element);

    public IReadOnlyCollection<BoardAsset> Assets => _assets.Values;

    /// <summary>
    /// Stores the asset and returns the instance held by the document. Content
    /// addressing makes repeated imports of the same bytes share a single asset.
    /// </summary>
    public BoardAsset AddAsset(BoardAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (_assets.TryGetValue(asset.Id, out var existing))
            return existing;

        _assets.Add(asset.Id, asset);
        return asset;
    }

    public bool TryGetAsset(string assetId, out BoardAsset asset) => _assets.TryGetValue(assetId, out asset!);

    public bool ContainsAsset(string assetId) => _assets.ContainsKey(assetId);

    /// <summary>
    /// Assets still referenced by live elements. Orphans stay in memory so undo
    /// keeps working, but only these are worth writing to disk.
    /// </summary>
    public IReadOnlyList<BoardAsset> GetReferencedAssets()
    {
        var referenced = new List<BoardAsset>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in _elements)
        {
            foreach (var assetId in element.RequiredAssetIds)
            {
                if (seen.Add(assetId) && _assets.TryGetValue(assetId, out var asset))
                    referenced.Add(asset);
            }
        }

        return referenced;
    }

    // Mirrors the renderer's draw order: higher ZIndex wins, ties go to the
    // element added last, so the element on top visually is the one hit.
    public BoardElement? GetTopmostElementAt(PointD worldPoint, double worldTolerance = 0.0)
    {
        BoardElement? topmost = null;
        foreach (var element in _elements)
        {
            if (!element.Contains(worldPoint, worldTolerance))
                continue;

            if (topmost is null || element.ZIndex >= topmost.ZIndex)
                topmost = element;
        }

        return topmost;
    }

    /// <summary>
    /// Union of every element's bounds, or null for an empty board. Driven by
    /// user commands rather than by frames, so it is computed on demand.
    /// </summary>
    public RectD? GetContentBounds()
    {
        if (_elements.Count == 0)
            return null;

        var left = double.MaxValue;
        var top = double.MaxValue;
        var right = double.MinValue;
        var bottom = double.MinValue;

        foreach (var element in _elements)
        {
            left = Math.Min(left, element.Bounds.Left);
            top = Math.Min(top, element.Bounds.Top);
            right = Math.Max(right, element.Bounds.Right);
            bottom = Math.Max(bottom, element.Bounds.Bottom);
        }

        return new RectD(left, top, right - left, bottom - top);
    }

    // Linear scan by design; swap the internals for a spatial index only if
    // profiling ever shows this as a bottleneck.
    public IEnumerable<BoardElement> GetElementsIntersecting(RectD bounds)
    {
        foreach (var element in _elements)
        {
            if (element.Bounds.Intersects(bounds))
                yield return element;
        }
    }
}
