using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Core.Tools;

public sealed class LineCreationTool : ITool
{
    private readonly BoardDocument _document;
    private readonly Viewport _viewport;
    private readonly CommandHistory _history;
    private readonly SnapContext _snap;
    private readonly Func<PointD, PointD, LineElement> _createElement;

    private LineElement? _current;
    private PointD _pressScreenPoint;

    public LineCreationTool(
        BoardDocument document,
        Viewport viewport,
        CommandHistory history,
        SnapContext snap,
        Func<PointD, PointD, LineElement> createElement)
    {
        _document = document;
        _viewport = viewport;
        _history = history;
        _snap = snap;
        _createElement = createElement;
    }

    public event Action<BoardElement>? CreationCompleted;

    public EditorCursor Cursor => EditorCursor.Cross;

    public bool PointerPressed(PointD screenPoint)
    {
        _pressScreenPoint = screenPoint;
        var worldPoint = _snap.SnapPoint(_viewport.ScreenToWorld(screenPoint));
        _current = _createElement(worldPoint, worldPoint);
        _document.AddElement(_current);
        return true;
    }

    public bool PointerMoved(PointD screenPoint)
    {
        if (_current is null)
            return false;

        // Only the endpoint being dragged snaps; the line has no bounding box of
        // its own to align.
        _current.End = _snap.SnapPoint(_viewport.ScreenToWorld(screenPoint));
        return true;
    }

    public bool PointerReleased(PointD screenPoint)
    {
        if (_current is null)
            return false;

        var element = _current;
        _current = null;

        var offset = screenPoint - _pressScreenPoint;
        var thresholdSquared = SelectionGeometry.DragThreshold * SelectionGeometry.DragThreshold;
        if (offset.LengthSquared < thresholdSquared)
        {
            // A click without a drag has no direction to give the line; discard it.
            _document.RemoveElement(element);
            return true;
        }

        _history.Record(new AddElementCommand(_document, element, _document.IndexOf(element)));
        CreationCompleted?.Invoke(element);
        return true;
    }
}
