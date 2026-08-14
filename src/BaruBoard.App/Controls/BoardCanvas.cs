using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using BaruBoard.App.Rendering;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Tools;
using BaruBoard.Core.Viewports;

namespace BaruBoard.App.Controls;

public sealed class BoardCanvas : Control
{
    private static readonly Cursor ArrowCursor = new(StandardCursorType.Arrow);
    private static readonly Cursor MoveCursor = new(StandardCursorType.SizeAll);
    private static readonly Cursor HorizontalResizeCursor = new(StandardCursorType.SizeWestEast);
    private static readonly Cursor VerticalResizeCursor = new(StandardCursorType.SizeNorthSouth);
    private static readonly Cursor NwSeResizeCursor = new(StandardCursorType.TopLeftCorner);
    private static readonly Cursor NeSwResizeCursor = new(StandardCursorType.TopRightCorner);
    private static readonly Cursor PanCursor = new(StandardCursorType.Hand);
    private static readonly Cursor CrossCursor = new(StandardCursorType.Cross);
    private static readonly Cursor TextCursor = new(StandardCursorType.Ibeam);

    private readonly AssetBitmapCache _bitmaps = new();
    private readonly DiagnosticsOverlay _diagnostics = new();
    private readonly GridSettings _grid = new();
    private readonly EditorInteractionState _interactionState = new();
    private readonly SnapContext _snap;
    private readonly BoardRenderer _renderer;
    private readonly SelectionOverlayRenderer _overlayRenderer = new();
    private readonly SelectionState _selection = new();
    private readonly ToolManager _toolManager = new();
    private readonly Dictionary<ToolKind, ITool> _tools = new();
    private readonly CommandHistory _history = new();
    private readonly BoardClipboard _clipboard = new();

    private BoardDocument _document = new();
    private SelectionTool? _selectionTool;
    private ToolKind _activeToolKind = ToolKind.Selection;
    private BoardElement? _editingElement;
    private bool _isSpaceDown;
    private bool _isPanning;
    private bool _isToolInteracting;
    private Point _lastPointerPosition;

    public BoardCanvas()
    {
        Focusable = true;
        ClipToBounds = true;
        _snap = new SnapContext(_grid, _interactionState);
        _renderer = new BoardRenderer(_bitmaps, _grid);
        AttachToolsTo(_document);

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        LostFocus += (_, _) => ResetTransientInput();
    }

    public event Action<ToolKind>? ActiveToolChanged;

    public event Action<TextElement, bool>? TextEditRequested;

    public event Action<IReadOnlyList<string>, PointD>? FilesDropped;

    public event Action? ViewportChanged;

    public BoardDocument Document
    {
        get => _document;
        set
        {
            _document = value;
            _bitmaps.Clear();
            _renderer.ClearCaches();
            _interactionState.Reset();
            AttachToolsTo(value);
            InvalidateVisual();
        }
    }

    public Viewport Viewport { get; } = new();

    public SelectionState Selection => _selection;

    public CommandHistory History => _history;

    public GridSettings Grid => _grid;

    public DiagnosticsOverlay Diagnostics => _diagnostics;

    public BoardClipboard Clipboard => _clipboard;

    /// <summary>
    /// True while a tool owns the pointer, from press until release.
    /// </summary>
    public bool IsInteracting => _isToolInteracting;

    public ToolKind ActiveToolKind => _activeToolKind;

    public void ActivateTool(ToolKind kind)
    {
        if (_activeToolKind == kind && ReferenceEquals(_toolManager.ActiveTool, _tools[kind]))
            return;

        _activeToolKind = kind;
        _toolManager.ActiveTool = _tools[kind];

        // Drawing and erasing must not keep another element's overlay on screen.
        if (kind is ToolKind.Pen or ToolKind.Eraser && !_selection.IsEmpty)
        {
            _selection.Clear();
            InvalidateVisual();
        }

        UpdateToolCursor(_toolManager.ActiveTool);
        ActiveToolChanged?.Invoke(kind);
    }

    public void SetEditingElement(BoardElement? element)
    {
        _editingElement = element;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var options = new BoardRenderOptions
        {
            DrawGrid = true,
            DrawBackground = true,
            SkipElement = _editingElement,
        };

        if (_diagnostics.IsEnabled)
        {
            _diagnostics.MeasureBoardRender(() => _renderer.Render(context, _document, Viewport, options));
        }
        else
        {
            _renderer.Render(context, _document, Viewport, options);
        }

        _overlayRenderer.Render(context, _selection, Viewport);
        _diagnostics.Render(context, _document, Viewport);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Viewport.ViewportSize = new SizeD(e.NewSize.Width, e.NewSize.Height);
        ViewportChanged?.Invoke();
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        UpdateModifiers(e.KeyModifiers);

        // A tool gesture owns the pointer until release: no pan or re-entrant
        // presses may alter the viewport mid-gesture, so ScreenToWorld stays
        // consistent for every sample of the interaction.
        if (_isToolInteracting)
        {
            e.Handled = true;
            return;
        }

        var properties = e.GetCurrentPoint(this).Properties;
        var startsPan = properties.IsMiddleButtonPressed ||
                        (_isSpaceDown && properties.IsLeftButtonPressed);
        if (startsPan)
        {
            _isPanning = true;
            _lastPointerPosition = e.GetPosition(this);
            Cursor = PanCursor;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!properties.IsLeftButtonPressed)
            return;

        if (e.ClickCount == 2 && ReferenceEquals(_toolManager.ActiveTool, _selectionTool))
        {
            var screen = e.GetPosition(this).ToPointD();
            var world = Viewport.ScreenToWorld(screen);
            var tolerance = SelectionGeometry.HitTolerance / Viewport.Zoom;
            if (_document.GetTopmostElementAt(world, tolerance) is TextElement text)
            {
                // Editing always targets exactly one element.
                _selection.Select(text);
                InvalidateVisual();
                TextEditRequested?.Invoke(text, false);
                e.Handled = true;
                return;
            }
        }

        if (_toolManager.ActiveTool is { } tool)
        {
            _isToolInteracting = true;
            _lastPointerPosition = e.GetPosition(this);
            e.Pointer.Capture(this);
            if (tool.PointerPressed(_lastPointerPosition.ToPointD()))
                InvalidateVisual();
            UpdateToolCursor(_toolManager.ActiveTool);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        UpdateModifiers(e.KeyModifiers);
        var position = e.GetPosition(this);

        if (_isPanning)
        {
            var delta = new VectorD(
                position.X - _lastPointerPosition.X,
                position.Y - _lastPointerPosition.Y);
            _lastPointerPosition = position;
            Viewport.Pan(delta);
            ViewportChanged?.Invoke();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _lastPointerPosition = position;
        if (_toolManager.ActiveTool is { } tool)
        {
            if (tool.PointerMoved(position.ToPointD()))
                InvalidateVisual();
            UpdateToolCursor(tool);
            if (_isToolInteracting)
                e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            UpdateToolCursor(_toolManager.ActiveTool);
            e.Handled = true;
            return;
        }

        if (_isToolInteracting && _toolManager.ActiveTool is { } tool)
        {
            _isToolInteracting = false;
            e.Pointer.Capture(null);
            if (tool.PointerReleased(e.GetPosition(this).ToPointD()))
                InvalidateVisual();
            UpdateToolCursor(_toolManager.ActiveTool);
            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isPanning = false;

        if (_isToolInteracting && _toolManager.ActiveTool is { } tool)
        {
            _isToolInteracting = false;
            // Finishes the interaction at the last known position so no drag
            // state lingers when the capture is taken away mid-drag.
            if (tool.PointerReleased(_lastPointerPosition.ToPointD()))
                InvalidateVisual();
            UpdateToolCursor(_toolManager.ActiveTool);
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_isToolInteracting)
        {
            e.Handled = true;
            return;
        }

        Viewport.ZoomBy(e.GetPosition(this).ToPointD(), e.Delta.Y);
        ViewportChanged?.Invoke();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        UpdateModifiers(e.KeyModifiers);

        if (e.Key == Key.Space)
        {
            _isSpaceDown = true;
            e.Handled = true;
            return;
        }

        // An active pointer gesture owns the board until it is released: editing
        // shortcuts must not run against a half-applied operation.
        if (_isToolInteracting)
            return;

        var control = e.KeyModifiers == KeyModifiers.Control;
        var controlShift = e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift);
        if (control || controlShift)
        {
            HandleEditingShortcut(e, control, controlShift);
            return;
        }

        if (e.KeyModifiers != KeyModifiers.None)
            return;

        if (e.Key is Key.Delete or Key.Back)
        {
            if (_selectionTool?.DeleteSelection() == true)
                InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (EditingOperations.ClearSelection(_selection))
                InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (GetToolShortcut(e.Key) is { } kind)
        {
            ActivateTool(kind);
            e.Handled = true;
        }
    }

    private void HandleEditingShortcut(KeyEventArgs e, bool control, bool controlShift)
    {
        switch (e.Key)
        {
            case Key.Z when controlShift:
            case Key.Y when control:
                ApplyEditingResult(e, EditingOperations.Redo(_history, _document, _selection));
                break;

            case Key.Z when control:
                ApplyEditingResult(e, EditingOperations.Undo(_history, _document, _selection));
                break;

            case Key.C when control:
                EditingOperations.Copy(_document, _selection, _clipboard);
                e.Handled = true;
                break;

            case Key.V when control:
                ApplyEditingResult(e, EditingOperations.Paste(_document, _selection, _clipboard, _history));
                break;

            case Key.D when control:
                ApplyEditingResult(e, EditingOperations.Duplicate(_document, _selection, _history));
                break;

            case Key.A when control:
                ApplyEditingResult(e, EditingOperations.SelectAll(_document, _selection));
                break;
        }
    }

    private void ApplyEditingResult(KeyEventArgs e, bool changed)
    {
        if (changed)
            InvalidateVisual();

        e.Handled = true;
    }

    private static ToolKind? GetToolShortcut(Key key) => key switch
    {
        Key.V => ToolKind.Selection,
        Key.R => ToolKind.Rectangle,
        Key.O => ToolKind.Ellipse,
        Key.L => ToolKind.Line,
        Key.A => ToolKind.Arrow,
        Key.T => ToolKind.Text,
        Key.P => ToolKind.Pen,
        Key.E => ToolKind.Eraser,
        _ => null,
    };

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        UpdateModifiers(e.KeyModifiers);

        if (e.Key == Key.Space)
        {
            _isSpaceDown = false;
            e.Handled = true;
        }
    }

    // Key up may never arrive once focus is gone, and a stuck modifier would
    // silently keep snapping disabled.
    private void ResetTransientInput()
    {
        _interactionState.Reset();
        _isSpaceDown = false;
    }

    private void UpdateModifiers(KeyModifiers modifiers)
    {
        _interactionState.IsSnapSuppressed = modifiers.HasFlag(KeyModifiers.Alt);
        _interactionState.IsMultiSelectModifierDown =
            modifiers.HasFlag(KeyModifiers.Shift) || modifiers.HasFlag(KeyModifiers.Control);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = GetDroppedPaths(e).Count > 0 ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var paths = GetDroppedPaths(e);

        e.Handled = true;
        if (paths.Count == 0)
            return;

        Focus();
        FilesDropped?.Invoke(paths, Viewport.ScreenToWorld(e.GetPosition(this).ToPointD()));
    }

    private static IReadOnlyList<string> GetDroppedPaths(DragEventArgs e) =>
        e.DataTransfer.TryGetFiles() is { } files
            ? [.. files.Select(file => file.TryGetLocalPath()).OfType<string>()]
            : [];

    private void AttachToolsTo(BoardDocument document)
    {
        _selection.Clear();
        _selection.MarqueeBounds = null;
        _editingElement = null;
        _tools.Clear();
        _history.Clear();

        _selectionTool = new SelectionTool(document, Viewport, _selection, _history, _snap, _interactionState);
        _tools[ToolKind.Selection] = _selectionTool;

        var rectangleTool = new ShapeCreationTool(document, Viewport, _history, _snap, CreationDefaults.CreateRectangle);
        var ellipseTool = new ShapeCreationTool(document, Viewport, _history, _snap, CreationDefaults.CreateEllipse);
        var lineTool = new LineCreationTool(document, Viewport, _history, _snap, CreationDefaults.CreateLine);
        var arrowTool = new LineCreationTool(document, Viewport, _history, _snap, CreationDefaults.CreateArrow);
        rectangleTool.CreationCompleted += OnCreationCompleted;
        ellipseTool.CreationCompleted += OnCreationCompleted;
        lineTool.CreationCompleted += OnCreationCompleted;
        arrowTool.CreationCompleted += OnCreationCompleted;
        _tools[ToolKind.Rectangle] = rectangleTool;
        _tools[ToolKind.Ellipse] = ellipseTool;
        _tools[ToolKind.Line] = lineTool;
        _tools[ToolKind.Arrow] = arrowTool;

        var textTool = new TextTool(document, Viewport);
        textTool.EditRequested += OnTextEditRequested;
        _tools[ToolKind.Text] = textTool;

        _tools[ToolKind.Pen] = new PenTool(document, Viewport, _history);
        _tools[ToolKind.Eraser] = new EraserTool(document, Viewport, _selection, _history);

        _toolManager.ActiveTool = _tools[ToolKind.Selection];
        if (_activeToolKind != ToolKind.Selection)
        {
            _activeToolKind = ToolKind.Selection;
            ActiveToolChanged?.Invoke(ToolKind.Selection);
        }
    }

    private void OnCreationCompleted(BoardElement element)
    {
        _selection.Select(element);
        ActivateTool(ToolKind.Selection);
        InvalidateVisual();
    }

    private void OnTextEditRequested(TextElement element)
    {
        _selection.Select(element);
        ActivateTool(ToolKind.Selection);
        InvalidateVisual();
        TextEditRequested?.Invoke(element, true);
    }

    private void UpdateToolCursor(ITool? tool)
    {
        if (_isPanning)
            return;

        Cursor = tool?.Cursor switch
        {
            EditorCursor.Move => MoveCursor,
            EditorCursor.ResizeHorizontal => HorizontalResizeCursor,
            EditorCursor.ResizeVertical => VerticalResizeCursor,
            EditorCursor.ResizeNwSe => NwSeResizeCursor,
            EditorCursor.ResizeNeSw => NeSwResizeCursor,
            EditorCursor.Cross => CrossCursor,
            EditorCursor.Text => TextCursor,
            _ => ArrowCursor,
        };
    }
}
