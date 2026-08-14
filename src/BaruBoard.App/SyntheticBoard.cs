#if DEBUG
using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.App;

/// <summary>
/// Development-only stress content, used to get real numbers out of the
/// diagnostics overlay. Not compiled into release builds.
/// </summary>
internal static class SyntheticBoard
{
    public static BoardDocument Create(int shapeCount = 3000, int strokeCount = 400, int textCount = 200)
    {
        var document = new BoardDocument { Name = "Quadro sintético" };
        var random = new Random(20260811);

        for (var i = 0; i < shapeCount; i++)
        {
            var bounds = new RectD(
                random.NextDouble() * 12000 - 6000,
                random.NextDouble() * 8000 - 4000,
                40 + random.NextDouble() * 160,
                30 + random.NextDouble() * 120);

            var fill = new ColorRgba(
                (byte)random.Next(120, 255),
                (byte)random.Next(120, 255),
                (byte)random.Next(120, 255));

            document.AddElement(i % 2 == 0
                ? new RectangleElement(bounds) { Fill = fill, StrokeThickness = 2 }
                : new EllipseElement(bounds) { Fill = fill, StrokeThickness = 2 });
        }

        for (var i = 0; i < strokeCount; i++)
        {
            var origin = new PointD(random.NextDouble() * 12000 - 6000, random.NextDouble() * 8000 - 4000);
            var path = new PathElement(origin) { StrokeThickness = 3 };
            var point = origin;

            for (var step = 0; step < 120; step++)
            {
                point = new PointD(point.X + random.NextDouble() * 8 - 4, point.Y + random.NextDouble() * 8 - 4);
                path.AppendPoint(point);
            }

            document.AddElement(path);
        }

        for (var i = 0; i < textCount; i++)
        {
            var position = new PointD(random.NextDouble() * 12000 - 6000, random.NextDouble() * 8000 - 4000);
            var text = new TextElement(position, $"Elemento de teste {i}", 24);
            text.SetMeasuredSize(new SizeD(220, 30));
            document.AddElement(text);
        }

        return document;
    }
}
#endif
