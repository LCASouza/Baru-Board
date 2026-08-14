using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;
using BaruBoard.Storage.Serialization;

namespace BaruBoard.Tests.Storage;

public class BoardSerializerTests
{
    private const double Tolerance = 1e-9;

    private static Viewport CreateViewport(double x = 154.5, double y = -220.25, double zoom = 0.85) => new()
    {
        Position = new PointD(x, y),
        Zoom = zoom,
        ViewportSize = new SizeD(1200, 800),
    };

    private static BoardLoadResult RoundTrip(BoardDocument document, Viewport? viewport = null)
    {
        var snapshot = BoardSerializer.CreateSnapshot(document, viewport ?? CreateViewport());
        var json = BoardSerializer.Serialize(snapshot.Board);
        return BoardSerializer.ToBoard(BoardSerializer.ReadSnapshot(json), snapshot.Assets);
    }

    [Fact]
    public void EmptyDocument_SurvivesRoundTrip()
    {
        var document = new BoardDocument { Name = "Vazio" };

        var result = RoundTrip(document);

        Assert.Empty(result.Document.Elements);
        Assert.Equal(document.Id, result.Document.Id);
        Assert.Equal("Vazio", result.Document.Name);
    }

    [Fact]
    public void Viewport_SurvivesRoundTrip()
    {
        var result = RoundTrip(new BoardDocument(), CreateViewport(-1234.5, 987.25, 2.5));

        Assert.Equal(-1234.5, result.ViewportPosition.X, Tolerance);
        Assert.Equal(987.25, result.ViewportPosition.Y, Tolerance);
        Assert.Equal(2.5, result.Zoom, Tolerance);
    }

    [Fact]
    public void Rectangle_SurvivesRoundTrip()
    {
        var document = new BoardDocument();
        var original = new RectangleElement(new RectD(-10.5, 20.25, 100, 50))
        {
            Fill = new ColorRgba(0x11, 0x22, 0x33, 0x44),
            Stroke = new ColorRgba(0xAA, 0xBB, 0xCC),
            StrokeThickness = 2.5,
            ZIndex = 7,
        };
        document.AddElement(original);

        var loaded = Assert.IsType<RectangleElement>(Assert.Single(RoundTrip(document).Document.Elements));

        Assert.Equal(original.Id, loaded.Id);
        Assert.Equal(original.Bounds, loaded.Bounds);
        Assert.Equal(original.Fill, loaded.Fill);
        Assert.Equal(original.Stroke, loaded.Stroke);
        Assert.Equal(2.5, loaded.StrokeThickness, Tolerance);
        Assert.Equal(7, loaded.ZIndex);
    }

    [Fact]
    public void Ellipse_SurvivesRoundTrip()
    {
        var document = new BoardDocument();
        document.AddElement(new EllipseElement(new RectD(0, 0, 80, 40)) { StrokeThickness = 1 });

        var loaded = Assert.IsType<EllipseElement>(Assert.Single(RoundTrip(document).Document.Elements));

        Assert.Equal(80, loaded.Bounds.Width, Tolerance);
        Assert.Equal(40, loaded.Bounds.Height, Tolerance);
    }

    [Fact]
    public void LineAndArrow_KeepTheirOwnTypes()
    {
        var document = new BoardDocument();
        document.AddElement(new LineElement(new PointD(0, 0), new PointD(100, 50)) { StrokeThickness = 3 });
        document.AddElement(new ArrowElement(new PointD(-50, -60), new PointD(10, 20)) { StrokeThickness = 4 });

        var elements = RoundTrip(document).Document.Elements;

        var line = Assert.IsType<LineElement>(elements[0]);
        var arrow = Assert.IsType<ArrowElement>(elements[1]);
        Assert.Equal(new PointD(0, 0), line.Start);
        Assert.Equal(new PointD(100, 50), line.End);
        Assert.Equal(new PointD(-50, -60), arrow.Start);
        Assert.Equal(new PointD(10, 20), arrow.End);
        Assert.Equal(4, arrow.StrokeThickness, Tolerance);
    }

    [Fact]
    public void Text_SurvivesRoundTripIncludingSizeHint()
    {
        var document = new BoardDocument();
        var original = new TextElement(new PointD(5, -6), "olá mundo", 24) { ZIndex = 2 };
        original.SetMeasuredSize(new SizeD(120, 30));
        document.AddElement(original);

        var loaded = Assert.IsType<TextElement>(Assert.Single(RoundTrip(document).Document.Elements));

        Assert.Equal("olá mundo", loaded.Text);
        Assert.Equal(24, loaded.FontSize, Tolerance);
        Assert.Equal(new PointD(5, -6), loaded.Bounds.Position);
        Assert.Equal(120, loaded.Bounds.Width, Tolerance);
        Assert.Equal(30, loaded.Bounds.Height, Tolerance);
    }

    [Fact]
    public void Path_SurvivesRoundTripWithAllPoints()
    {
        var document = new BoardDocument();
        var original = new PathElement(new PointD(0, 0)) { StrokeThickness = 3 };
        for (var i = 1; i <= 200; i++)
            original.AppendPoint(new PointD(i * 1.5, -i * 0.25));
        document.AddElement(original);

        var loaded = Assert.IsType<PathElement>(Assert.Single(RoundTrip(document).Document.Elements));

        Assert.Equal(original.Points.Count, loaded.Points.Count);
        Assert.Equal(original.Points[0], loaded.Points[0]);
        Assert.Equal(original.Points[^1], loaded.Points[^1]);
        Assert.Equal(original.Bounds, loaded.Bounds);
    }

    [Fact]
    public void SinglePointPath_SurvivesRoundTrip()
    {
        var document = new BoardDocument();
        document.AddElement(new PathElement(new PointD(42, -42)));

        var loaded = Assert.IsType<PathElement>(Assert.Single(RoundTrip(document).Document.Elements));

        Assert.Single(loaded.Points);
        Assert.Equal(new PointD(42, -42), loaded.Points[0]);
    }

    [Fact]
    public void ElementOrder_IsPreserved()
    {
        var document = new BoardDocument();
        document.AddElement(new RectangleElement(new RectD(0, 0, 10, 10)));
        document.AddElement(new EllipseElement(new RectD(0, 0, 10, 10)));
        document.AddElement(new PathElement(new PointD(0, 0)));
        document.AddElement(new TextElement(new PointD(0, 0), "x", 12));

        var elements = RoundTrip(document).Document.Elements;

        Assert.Collection(
            elements,
            e => Assert.IsType<RectangleElement>(e),
            e => Assert.IsType<EllipseElement>(e),
            e => Assert.IsType<PathElement>(e),
            e => Assert.IsType<TextElement>(e));
    }

    [Fact]
    public void NegativeCoordinates_SurviveRoundTrip()
    {
        var document = new BoardDocument();
        document.AddElement(new RectangleElement(new RectD(-2000.75, -1400.5, 500, 350)));

        var loaded = Assert.Single(RoundTrip(document, CreateViewport(-8000, -9000, 0.1)).Document.Elements);

        Assert.Equal(-2000.75, loaded.Bounds.X, Tolerance);
        Assert.Equal(-1400.5, loaded.Bounds.Y, Tolerance);
    }

    [Fact]
    public void Snapshot_IsDetachedFromTheLiveDocument()
    {
        var document = new BoardDocument();
        var element = new RectangleElement(new RectD(0, 0, 100, 100));
        document.AddElement(element);
        var snapshot = BoardSerializer.CreateSnapshot(document, CreateViewport());

        element.MoveTo(new PointD(9999, 9999));
        document.AddElement(new EllipseElement(new RectD(0, 0, 10, 10)));

        var loaded = Assert.Single(BoardSerializer.ToBoard(snapshot.Board, snapshot.Assets).Document.Elements);
        Assert.Equal(0, loaded.Bounds.X, Tolerance);
    }

    [Fact]
    public void PathSnapshot_DoesNotSharePointsWithTheLiveElement()
    {
        var document = new BoardDocument();
        var path = new PathElement(new PointD(0, 0));
        path.AppendPoint(new PointD(10, 10));
        document.AddElement(path);
        var snapshot = BoardSerializer.CreateSnapshot(document, CreateViewport());

        path.AppendPoint(new PointD(500, 500));

        var loaded = Assert.IsType<PathElement>(Assert.Single(BoardSerializer.ToBoard(snapshot.Board, snapshot.Assets).Document.Elements));
        Assert.Equal(2, loaded.Points.Count);
    }
}
