using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Core.Tools;

public sealed class ShapeCreationTool : ITool
{
    private readonly BoardDocument _document;
    private readonly Viewport _viewport;
    private readonly CommandHistory _history;
    private readonly SnapContext _snap;
    private readonly Func<RectD, BoardElement> _createElement;

    private BoardElement? _current;
    private PointD _pressScreenPoint;
    private PointD _anchorWorldPoint;

    public ShapeCreationTool(
        BoardDocument document,
        Viewport viewport,
        CommandHistory history,
        SnapContext snap,
        Func<RectD, BoardElement> createElement)
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
        _anchorWorldPoint = _snap.SnapPoint(_viewport.ScreenToWorld(screenPoint));
        _current = _createElement(new RectD(_anchorWorldPoint, new SizeD(0, 0)));
        _document.AddElement(_current);
        return true;
    }

    public bool PointerMoved(PointD screenPoint)
    {
        if (_current is null)
            return false;

        _current.ResizeTo(RectD.FromPoints(
            _anchorWorldPoint,
            _snap.SnapPoint(_viewport.ScreenToWorld(screenPoint))));
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
            // Plain click: place a comfortably sized shape centered on the click.
            var size = CreationDefaults.ShapeSize;
            element.ResizeTo(new RectD(
                new PointD(_anchorWorldPoint.X - size.Width / 2, _anchorWorldPoint.Y - size.Height / 2),
                size));
        }
        else
        {
            var bounds = element.Bounds;
            var width = Math.Max(bounds.Width, SelectionGeometry.MinElementSize);
            var height = Math.Max(bounds.Height, SelectionGeometry.MinElementSize);
            if (width != bounds.Width || height != bounds.Height)
                element.ResizeTo(new RectD(bounds.X, bounds.Y, width, height));
        }

        _history.Record(new AddElementCommand(_document, element, _document.IndexOf(element)));
        CreationCompleted?.Invoke(element);
        return true;
    }
}
