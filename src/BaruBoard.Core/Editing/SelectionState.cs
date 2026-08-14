using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Editing;

/// <summary>
/// Editor-side selection: an ordered set of elements plus the transient marquee
/// rectangle. It lives outside <see cref="BoardDocument"/> because selection is
/// interaction state, not persistable board content.
/// </summary>
public sealed class SelectionState
{
    private readonly List<BoardElement> _elements = [];

    public IReadOnlyList<BoardElement> Elements => _elements;

    /// <summary>
    /// Most recently included element, derived from the order instead of being
    /// tracked separately.
    /// </summary>
    public BoardElement? Primary => _elements.Count > 0 ? _elements[^1] : null;

    public int Count => _elements.Count;

    public bool IsEmpty => _elements.Count == 0;

    /// <summary>
    /// World-space rectangle being dragged by a marquee, or null when no marquee
    /// is in progress.
    /// </summary>
    public RectD? MarqueeBounds { get; set; }

    public RectD? Bounds
    {
        get
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
    }

    public bool Contains(BoardElement element) => _elements.Contains(element);

    public void Select(BoardElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _elements.Clear();
        _elements.Add(element);
    }

    // Re-adding keeps a single entry and makes the element the most recent one,
    // which is what Primary reports.
    public void Add(BoardElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _elements.Remove(element);
        _elements.Add(element);
    }

    public bool Remove(BoardElement element) => _elements.Remove(element);

    public void Toggle(BoardElement element)
    {
        if (!_elements.Remove(element))
            _elements.Add(element);
    }

    public void SelectMany(IEnumerable<BoardElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        _elements.Clear();
        foreach (var element in elements)
        {
            if (!_elements.Contains(element))
                _elements.Add(element);
        }
    }

    public void Clear() => _elements.Clear();

    /// <summary>
    /// Drops elements that no longer belong to the document, as happens after an
    /// undo or an eraser gesture.
    /// </summary>
    public void RemoveMissing(BoardDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _elements.RemoveAll(element => document.IndexOf(element) < 0);
    }
}
