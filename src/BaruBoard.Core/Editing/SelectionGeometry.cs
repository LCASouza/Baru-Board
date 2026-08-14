using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Core.Editing;

/// <summary>
/// Shared geometry for the selection overlay and its interaction: handle layout,
/// screen-space handle hit testing and resize math. Handles have a constant
/// on-screen size in DIPs regardless of zoom.
/// </summary>
public static class SelectionGeometry
{
    public const double HandleScreenSize = 8.0;

    public const double HandleHitTolerance = 6.0;

    // Screen-space slack for hitting elements, mainly thin lines and arrows.
    public const double HitTolerance = 4.0;

    public const double DragThreshold = 3.0;

    public const double MinElementSize = 8.0;

    // Corners first so they win when handles overlap on small elements.
    public static readonly ResizeHandle[] AllHandles =
    [
        ResizeHandle.TopLeft,
        ResizeHandle.TopRight,
        ResizeHandle.BottomRight,
        ResizeHandle.BottomLeft,
        ResizeHandle.Top,
        ResizeHandle.Right,
        ResizeHandle.Bottom,
        ResizeHandle.Left,
    ];

    public static readonly ResizeHandle[] CornerHandles =
    [
        ResizeHandle.TopLeft,
        ResizeHandle.TopRight,
        ResizeHandle.BottomRight,
        ResizeHandle.BottomLeft,
    ];

    private static readonly ResizeHandle[] NoHandles = [];

    /// <summary>
    /// Handles an element actually offers. Proportional elements expose only the
    /// corners, so dragging can never mean "stretch one axis".
    /// </summary>
    public static IReadOnlyList<ResizeHandle> GetHandles(ElementResizeMode mode) => mode switch
    {
        ElementResizeMode.Free => AllHandles,
        ElementResizeMode.ProportionalCorners => CornerHandles,
        _ => NoHandles,
    };

    public static PointD GetHandleCenter(RectD bounds, ResizeHandle handle) => handle switch
    {
        ResizeHandle.TopLeft => new PointD(bounds.Left, bounds.Top),
        ResizeHandle.Top => new PointD(bounds.Left + bounds.Width / 2, bounds.Top),
        ResizeHandle.TopRight => new PointD(bounds.Right, bounds.Top),
        ResizeHandle.Right => new PointD(bounds.Right, bounds.Top + bounds.Height / 2),
        ResizeHandle.BottomRight => new PointD(bounds.Right, bounds.Bottom),
        ResizeHandle.Bottom => new PointD(bounds.Left + bounds.Width / 2, bounds.Bottom),
        ResizeHandle.BottomLeft => new PointD(bounds.Left, bounds.Bottom),
        ResizeHandle.Left => new PointD(bounds.Left, bounds.Top + bounds.Height / 2),
        _ => throw new ArgumentOutOfRangeException(nameof(handle)),
    };

    public static PointD GetOppositeCorner(RectD bounds, ResizeHandle handle) => handle switch
    {
        ResizeHandle.TopLeft => new PointD(bounds.Right, bounds.Bottom),
        ResizeHandle.TopRight => new PointD(bounds.Left, bounds.Bottom),
        ResizeHandle.BottomRight => new PointD(bounds.Left, bounds.Top),
        ResizeHandle.BottomLeft => new PointD(bounds.Right, bounds.Top),
        _ => throw new ArgumentOutOfRangeException(nameof(handle)),
    };

    public static ResizeHandle? HitTestHandles(
        RectD bounds, PointD screenPoint, Viewport viewport, ElementResizeMode mode = ElementResizeMode.Free)
    {
        foreach (var handle in GetHandles(mode))
        {
            var center = viewport.WorldToScreen(GetHandleCenter(bounds, handle));
            if (Math.Abs(screenPoint.X - center.X) <= HandleHitTolerance &&
                Math.Abs(screenPoint.Y - center.Y) <= HandleHitTolerance)
            {
                return handle;
            }
        }

        return null;
    }

    public static RectD Resize(
        RectD initialBounds,
        ResizeHandle handle,
        VectorD worldDelta,
        ElementResizeMode mode = ElementResizeMode.Free) =>
        mode == ElementResizeMode.ProportionalCorners
            ? ResizeProportional(initialBounds, handle, worldDelta)
            : ResizeFree(initialBounds, handle, worldDelta);

    // Each handle moves only its own edges; the dragged edge clamps against the
    // opposite one so the element never inverts, it just stops at the minimum size.
    private static RectD ResizeFree(RectD initialBounds, ResizeHandle handle, VectorD worldDelta)
    {
        var left = initialBounds.Left;
        var top = initialBounds.Top;
        var right = initialBounds.Right;
        var bottom = initialBounds.Bottom;

        var movesLeft = handle is ResizeHandle.TopLeft or ResizeHandle.Left or ResizeHandle.BottomLeft;
        var movesRight = handle is ResizeHandle.TopRight or ResizeHandle.Right or ResizeHandle.BottomRight;
        var movesTop = handle is ResizeHandle.TopLeft or ResizeHandle.Top or ResizeHandle.TopRight;
        var movesBottom = handle is ResizeHandle.BottomLeft or ResizeHandle.Bottom or ResizeHandle.BottomRight;

        if (movesLeft)
            left = Math.Min(left + worldDelta.X, right - MinElementSize);
        if (movesRight)
            right = Math.Max(right + worldDelta.X, left + MinElementSize);
        if (movesTop)
            top = Math.Min(top + worldDelta.Y, bottom - MinElementSize);
        if (movesBottom)
            bottom = Math.Max(bottom + worldDelta.Y, top + MinElementSize);

        return new RectD(left, top, right - left, bottom - top);
    }

    // The opposite corner stays anchored and the aspect ratio is preserved; the
    // axis dragged furthest drives the scale.
    private static RectD ResizeProportional(RectD initialBounds, ResizeHandle handle, VectorD worldDelta)
    {
        if (initialBounds.Width <= 0 || initialBounds.Height <= 0)
            return initialBounds;

        var anchor = GetOppositeCorner(initialBounds, handle);
        var dragged = GetHandleCenter(initialBounds, handle) + worldDelta;

        var scale = Math.Max(
            Math.Abs(dragged.X - anchor.X) / initialBounds.Width,
            Math.Abs(dragged.Y - anchor.Y) / initialBounds.Height);

        var minScale = Math.Max(
            MinElementSize / initialBounds.Width,
            MinElementSize / initialBounds.Height);
        scale = Math.Max(scale, minScale);

        var width = initialBounds.Width * scale;
        var height = initialBounds.Height * scale;

        var left = handle is ResizeHandle.TopLeft or ResizeHandle.BottomLeft ? anchor.X - width : anchor.X;
        var top = handle is ResizeHandle.TopLeft or ResizeHandle.TopRight ? anchor.Y - height : anchor.Y;

        return new RectD(left, top, width, height);
    }

    public static EditorCursor GetCursor(ResizeHandle handle) => handle switch
    {
        ResizeHandle.TopLeft or ResizeHandle.BottomRight => EditorCursor.ResizeNwSe,
        ResizeHandle.TopRight or ResizeHandle.BottomLeft => EditorCursor.ResizeNeSw,
        ResizeHandle.Top or ResizeHandle.Bottom => EditorCursor.ResizeVertical,
        _ => EditorCursor.ResizeHorizontal,
    };
}
