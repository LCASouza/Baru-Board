using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Core.Tools;

public sealed class PenTool : ITool
{
    // Capture-time filter: drops pointer samples closer than this to the last
    // accepted one. Final geometric cleanup is done by RDP on release.
    public const double MinSampleDistanceDip = 0.75;

    public const double SimplifyToleranceDip = 0.75;

    private readonly BoardDocument _document;
    private readonly Viewport _viewport;
    private readonly CommandHistory _history;

    private PathElement? _current;
    private PointD _lastAcceptedScreenPoint;
    private double _strokeZoom = 1.0;

    public PenTool(BoardDocument document, Viewport viewport, CommandHistory history)
    {
        _document = document;
        _viewport = viewport;
        _history = history;
    }

    public event Action<PathElement>? StrokeCompleted;

    public EditorCursor Cursor => EditorCursor.Cross;

    public bool PointerPressed(PointD screenPoint)
    {
        // The canvas freezes the viewport for the whole gesture, so this zoom
        // stays valid until the stroke is released.
        _strokeZoom = _viewport.Zoom;
        _lastAcceptedScreenPoint = screenPoint;
        _current = new PathElement(_viewport.ScreenToWorld(screenPoint))
        {
            Stroke = CreationDefaults.PenStroke,
            StrokeThickness = CreationDefaults.PenThickness,
        };
        _document.AddElement(_current);
        return true;
    }

    public bool PointerMoved(PointD screenPoint)
    {
        if (_current is null)
            return false;

        var offset = screenPoint - _lastAcceptedScreenPoint;
        if (offset.LengthSquared < MinSampleDistanceDip * MinSampleDistanceDip)
            return false;

        _lastAcceptedScreenPoint = screenPoint;
        _current.AppendPoint(_viewport.ScreenToWorld(screenPoint));
        return true;
    }

    public bool PointerReleased(PointD screenPoint)
    {
        if (_current is null)
            return false;

        var element = _current;
        _current = null;

        var simplified = PathSimplification.Simplify(element.Points, SimplifyToleranceDip / _strokeZoom);
        if (simplified.Count < element.Points.Count)
            element.SetPoints(simplified);

        _history.Record(new AddElementCommand(_document, element, _document.IndexOf(element)));
        StrokeCompleted?.Invoke(element);
        return true;
    }
}
