using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Tools;

public static class CreationDefaults
{
    public const double DefaultStrokeThickness = 2.0;

    public const double DefaultFontSize = 24.0;

    public static readonly SizeD ShapeSize = new(160, 100);

    public static readonly ColorRgba ShapeFill = new(0xFF, 0xFF, 0xFF);

    public static readonly ColorRgba ShapeStroke = new(0x37, 0x47, 0x4F);

    public static readonly ColorRgba LineStroke = new(0x37, 0x47, 0x4F);

    public static readonly ColorRgba TextForeground = new(0x21, 0x21, 0x21);

    public const double PenThickness = 3.0;

    public static readonly ColorRgba PenStroke = new(0x21, 0x21, 0x21);

    public static RectangleElement CreateRectangle(RectD bounds) => new(bounds)
    {
        Fill = ShapeFill,
        Stroke = ShapeStroke,
        StrokeThickness = DefaultStrokeThickness,
    };

    public static EllipseElement CreateEllipse(RectD bounds) => new(bounds)
    {
        Fill = ShapeFill,
        Stroke = ShapeStroke,
        StrokeThickness = DefaultStrokeThickness,
    };

    public static LineElement CreateLine(PointD start, PointD end) => new(start, end)
    {
        Stroke = LineStroke,
        StrokeThickness = DefaultStrokeThickness,
    };

    public static ArrowElement CreateArrow(PointD start, PointD end) => new(start, end)
    {
        Stroke = LineStroke,
        StrokeThickness = DefaultStrokeThickness,
    };

    public static TextElement CreateText(PointD position) => new(position, "", DefaultFontSize)
    {
        Foreground = TextForeground,
    };
}
