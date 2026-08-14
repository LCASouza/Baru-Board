using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.App.Rendering;

// Selection decorations are drawn in screen space so outline, handles and the
// marquee keep a constant on-screen size at any zoom level.
public sealed class SelectionOverlayRenderer
{
    private static readonly ImmutableSolidColorBrush AccentBrush = new(Color.FromRgb(0x21, 0x96, 0xF3));
    private static readonly ImmutablePen OutlinePen = new(AccentBrush, 1.5);
    private static readonly ImmutablePen GroupPen = new(AccentBrush, 1.0, new ImmutableDashStyle([4, 3], 0));
    private static readonly ImmutablePen HandlePen = new(AccentBrush, 1.5);
    private static readonly ImmutableSolidColorBrush HandleFill = new(Colors.White);
    private static readonly ImmutableSolidColorBrush MarqueeFill = new(Color.FromArgb(0x33, 0x21, 0x96, 0xF3));
    private static readonly ImmutablePen MarqueePen = new(AccentBrush, 1.0);

    public void Render(DrawingContext context, SelectionState selection, Viewport viewport)
    {
        foreach (var element in selection.Elements)
            context.DrawRectangle(null, OutlinePen, ToScreen(element.Bounds, viewport));

        if (selection.Count > 1 && selection.Bounds is { } groupBounds)
            context.DrawRectangle(null, GroupPen, ToScreen(groupBounds, viewport));

        // Resize handles belong to a single element; group resizing is not offered.
        if (selection.Count == 1 && selection.Primary is { } single)
        {
            foreach (var handle in SelectionGeometry.GetHandles(single.ResizeMode))
            {
                var center = viewport.WorldToScreen(SelectionGeometry.GetHandleCenter(single.Bounds, handle));
                var half = SelectionGeometry.HandleScreenSize / 2;
                context.DrawRectangle(
                    HandleFill,
                    HandlePen,
                    new Rect(
                        center.X - half,
                        center.Y - half,
                        SelectionGeometry.HandleScreenSize,
                        SelectionGeometry.HandleScreenSize));
            }
        }

        if (selection.MarqueeBounds is { } marquee)
            context.DrawRectangle(MarqueeFill, MarqueePen, ToScreen(marquee, viewport));
    }

    private static Rect ToScreen(RectD bounds, Viewport viewport)
    {
        var topLeft = viewport.WorldToScreen(bounds.Position).ToAvalonia();
        var bottomRight = viewport.WorldToScreen(new PointD(bounds.Right, bounds.Bottom)).ToAvalonia();
        return new Rect(topLeft, bottomRight);
    }
}
