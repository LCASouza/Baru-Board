using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Core.Tools;

/// <summary>
/// Object eraser for ink: removes whole <see cref="PathElement"/> strokes under
/// the cursor. Other element types are never touched.
/// </summary>
public sealed class EraserTool : ITool
{
    public const double RadiusDip = 12.0;

    public const double SampleSpacingDip = 6.0;

    private readonly BoardDocument _document;
    private readonly Viewport _viewport;
    private readonly SelectionState _selection;
    private readonly CommandHistory _history;

    // Removals made by the current gesture, in the order they happened; the whole
    // gesture becomes a single history entry. An element cannot show up twice
    // because removing it takes it out of the collection being scanned.
    private readonly List<RemovedElement> _erasedInGesture = [];
    private bool _isErasing;
    private PointD _lastScreenPoint;

    public EraserTool(BoardDocument document, Viewport viewport, SelectionState selection, CommandHistory history)
    {
        _document = document;
        _viewport = viewport;
        _selection = selection;
        _history = history;
    }

    public EditorCursor Cursor => EditorCursor.Cross;

    public bool PointerPressed(PointD screenPoint)
    {
        _isErasing = true;
        _erasedInGesture.Clear();
        _lastScreenPoint = screenPoint;
        return EraseAt(screenPoint);
    }

    public bool PointerMoved(PointD screenPoint)
    {
        if (!_isErasing)
            return false;

        var erased = EraseAlong(_lastScreenPoint, screenPoint);
        _lastScreenPoint = screenPoint;
        return erased;
    }

    public bool PointerReleased(PointD screenPoint)
    {
        if (!_isErasing)
            return false;

        _isErasing = false;
        if (_erasedInGesture.Count > 0)
        {
            _history.Record(new RemoveElementsCommand(_document, _erasedInGesture));
            _erasedInGesture.Clear();
        }

        return false;
    }

    // Interpolates in screen space so eraser density is zoom-independent even
    // when pointer events arrive far apart during a fast drag.
    private bool EraseAlong(PointD fromScreen, PointD toScreen)
    {
        var offset = toScreen - fromScreen;
        var steps = Math.Max(1, (int)Math.Ceiling(offset.Length / SampleSpacingDip));

        var erased = false;
        for (var i = 1; i <= steps; i++)
        {
            var t = (double)i / steps;
            var sample = new PointD(fromScreen.X + offset.X * t, fromScreen.Y + offset.Y * t);
            erased |= EraseAt(sample);
        }

        return erased;
    }

    private bool EraseAt(PointD screenPoint)
    {
        var worldPoint = _viewport.ScreenToWorld(screenPoint);
        var worldRadius = RadiusDip / _viewport.Zoom;

        List<PathElement>? hits = null;
        foreach (var element in _document.Elements)
        {
            if (element is PathElement path && path.Contains(worldPoint, worldRadius))
                (hits ??= []).Add(path);
        }

        if (hits is null)
            return false;

        foreach (var path in hits)
        {
            _erasedInGesture.Add(new RemovedElement(path, _document.IndexOf(path)));
            _document.RemoveElement(path);
            _selection.Remove(path);
        }

        return true;
    }
}
