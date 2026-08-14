using Avalonia;
using Avalonia.Media;
using BaruBoard.Core.Geometry;

namespace BaruBoard.App.Rendering;

internal static class GeometryConversions
{
    public static Point ToAvalonia(this PointD point) => new(point.X, point.Y);

    public static Rect ToAvalonia(this RectD rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    public static Color ToAvalonia(this ColorRgba color) => Color.FromArgb(color.A, color.R, color.G, color.B);

    public static PointD ToPointD(this Point point) => new(point.X, point.Y);
}
