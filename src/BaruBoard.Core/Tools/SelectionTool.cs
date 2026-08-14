using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Core.Tools;

public sealed class SelectionTool : ITool
{
    private enum Interaction
    {
        None,
        MoveCandidate,
        Moving,
        Resizing,
        MarqueeCandidate,
        Marquee,
    }

    private readonly BoardDocument _document;
    private readonly Viewport _viewport;
    private readonly SelectionState _selection;
    private readonly CommandHistory _history;
    private readonly SnapContext _snap;
    private readonly EditorInteractionState _interactionState;

    private readonly List<ElementMove> _movingElements = [];
    private Interaction _interaction = Interaction.None;
    private BoardElement? _resizeTarget;
    private ResizeHandle _activeHandle;
    private PointD _pressScreenPoint;
    private PointD _startWorldPoint;
    private PointD _anchorBeforeMove;
    private PointD _handleCenterBeforeResize;
    private RectD _initialBounds;

    public SelectionTool(
        BoardDocument document,
        Viewport viewport,
        SelectionState selection,
        CommandHistory history,
        SnapContext snap,
        EditorInteractionState interactionState)
    {
        _document = document;
        _viewport = viewport;
        _selection = selection;
        _history = history;
        _snap = snap;
        _interactionState = interactionState;
    }

    public EditorCursor Cursor { get; private set; } = EditorCursor.Default;

    public bool PointerPressed(PointD screenPoint)
    {
        var worldPoint = _viewport.ScreenToWorld(screenPoint);
        _pressScreenPoint = screenPoint;
        _startWorldPoint = worldPoint;

        // Resize handles only exist while a single element is selected.
        if (_selection.Count == 1 && _selection.Primary is { CanResize: true } single)
        {
            var handle = SelectionGeometry.HitTestHandles(
                single.Bounds, screenPoint, _viewport, single.ResizeMode);
            if (handle is not null)
            {
                _interaction = Interaction.Resizing;
                _resizeTarget = single;
                _activeHandle = handle.Value;
                _initialBounds = single.Bounds;
                _handleCenterBeforeResize = SelectionGeometry.GetHandleCenter(single.Bounds, handle.Value);
                Cursor = SelectionGeometry.GetCursor(handle.Value);
                return false;
            }
        }

        var hit = _document.GetTopmostElementAt(worldPoint, SelectionGeometry.HitTolerance / _viewport.Zoom);
        if (hit is null)
        {
            // The selection is only dropped on release, once a plain click can be
            // told apart from the start of a marquee.
            _interaction = Interaction.MarqueeCandidate;
            Cursor = EditorCursor.Default;
            return false;
        }

        if (_interactionState.IsMultiSelectModifierDown)
        {
            _selection.Toggle(hit);
            _interaction = Interaction.None;
            Cursor = ComputeHoverCursor(screenPoint);
            return true;
        }

        var selectionChanged = false;
        if (!_selection.Contains(hit))
        {
            _selection.Select(hit);
            selectionChanged = true;
        }

        _interaction = Interaction.MoveCandidate;
        Cursor = EditorCursor.Move;
        return selectionChanged;
    }

    public bool PointerMoved(PointD screenPoint)
    {
        if (_interaction is Interaction.MoveCandidate or Interaction.MarqueeCandidate)
        {
            if (!HasPassedDragThreshold(screenPoint))
                return false;

            if (_interaction == Interaction.MoveCandidate)
                BeginMove();
            else
                _interaction = Interaction.Marquee;
        }

        switch (_interaction)
        {
            case Interaction.Moving:
                ApplyMove(screenPoint);
                Cursor = EditorCursor.Move;
                return true;

            case Interaction.Resizing:
                ApplyResize(screenPoint);
                return true;

            case Interaction.Marquee:
                ApplyMarquee(screenPoint);
                return true;

            default:
                Cursor = ComputeHoverCursor(screenPoint);
                return false;
        }
    }

    public bool PointerReleased(PointD screenPoint)
    {
        var interaction = _interaction;
        _interaction = Interaction.None;
        var changed = false;

        switch (interaction)
        {
            case Interaction.Moving:
                changed = RecordMove();
                break;

            case Interaction.Resizing:
                if (_resizeTarget is { } target && _initialBounds != target.Bounds)
                    _history.Record(new ResizeElementCommand(target, _initialBounds, target.Bounds));
                break;

            case Interaction.MarqueeCandidate:
                // A click on empty space clears the selection.
                changed = !_selection.IsEmpty;
                _selection.Clear();
                break;

            case Interaction.Marquee:
                _selection.MarqueeBounds = null;
                changed = true;
                break;
        }

        _resizeTarget = null;
        _movingElements.Clear();
        Cursor = ComputeHoverCursor(screenPoint);
        return changed;
    }

    public bool DeleteSelection()
    {
        if (_selection.IsEmpty)
            return false;

        var removals = new List<RemovedElement>();
        foreach (var element in _selection.Elements.ToList())
        {
            var index = _document.IndexOf(element);
            if (index < 0)
                continue;

            _document.RemoveElement(element);
            removals.Add(new RemovedElement(element, index));
        }

        if (removals.Count == 0)
            return false;

        _history.Record(new RemoveElementsCommand(_document, removals));
        _selection.Clear();
        _interaction = Interaction.None;
        _movingElements.Clear();
        return true;
    }

    private bool HasPassedDragThreshold(PointD screenPoint)
    {
        // A real drag only starts past the threshold, so a plain click with
        // pointer jitter never nudges an element or opens a marquee.
        var offset = screenPoint - _pressScreenPoint;
        return offset.LengthSquared >= SelectionGeometry.DragThreshold * SelectionGeometry.DragThreshold;
    }

    private void BeginMove()
    {
        _interaction = Interaction.Moving;
        _movingElements.Clear();
        foreach (var element in _selection.Elements)
            _movingElements.Add(new ElementMove(element, element.Bounds.Position, element.Bounds.Position));

        _anchorBeforeMove = _selection.Bounds?.Position ?? _startWorldPoint;
    }

    // The whole selection is snapped through a single anchor, so the elements
    // keep their relative arrangement no matter where each one sits.
    private void ApplyMove(PointD screenPoint)
    {
        var pointerDelta = _viewport.ScreenToWorld(screenPoint) - _startWorldPoint;
        var snappedAnchor = _snap.SnapPoint(_anchorBeforeMove + pointerDelta);
        var delta = snappedAnchor - _anchorBeforeMove;

        foreach (var move in _movingElements)
            move.Element.MoveTo(new PointD(move.Before.X + delta.X, move.Before.Y + delta.Y));
    }

    private bool RecordMove()
    {
        var moves = new List<ElementMove>();
        foreach (var move in _movingElements)
        {
            var after = move.Element.Bounds.Position;
            if (after != move.Before)
                moves.Add(move with { After = after });
        }

        if (moves.Count == 0)
            return false;

        _history.Record(new MoveElementsCommand(moves));
        return true;
    }

    private void ApplyResize(PointD screenPoint)
    {
        if (_resizeTarget is not { } target)
            return;

        var pointerDelta = _viewport.ScreenToWorld(screenPoint) - _startWorldPoint;
        var snappedHandle = _snap.SnapPoint(_handleCenterBeforeResize + pointerDelta);
        var delta = snappedHandle - _handleCenterBeforeResize;
        target.ResizeTo(SelectionGeometry.Resize(_initialBounds, _activeHandle, delta, target.ResizeMode));
    }

    private void ApplyMarquee(PointD screenPoint)
    {
        var bounds = RectD.FromPoints(_startWorldPoint, _viewport.ScreenToWorld(screenPoint));
        _selection.MarqueeBounds = bounds;
        _selection.SelectMany(_document.GetElementsIntersecting(bounds));
    }

    private EditorCursor ComputeHoverCursor(PointD screenPoint)
    {
        if (_selection.Count == 1 && _selection.Primary is { } single)
        {
            if (single.CanResize)
            {
                var handle = SelectionGeometry.HitTestHandles(
                    single.Bounds, screenPoint, _viewport, single.ResizeMode);
                if (handle is not null)
                    return SelectionGeometry.GetCursor(handle.Value);
            }
        }

        var worldTolerance = SelectionGeometry.HitTolerance / _viewport.Zoom;
        var worldPoint = _viewport.ScreenToWorld(screenPoint);
        foreach (var element in _selection.Elements)
        {
            if (element.Contains(worldPoint, worldTolerance))
                return EditorCursor.Move;
        }

        return EditorCursor.Default;
    }
}
