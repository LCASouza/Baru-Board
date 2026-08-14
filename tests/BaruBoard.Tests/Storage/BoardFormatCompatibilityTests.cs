using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;
using BaruBoard.Storage.Serialization;

namespace BaruBoard.Tests.Storage;

public class BoardFormatCompatibilityTests
{
    private const double Tolerance = 1e-9;

    // Hand written contract of format version 1. It must keep loading unchanged:
    // if a code change breaks this test, it breaks every file already saved.
    private const string CanonicalV1 = """
    {
      "formatVersion": 1,
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "Quadro canonico",
      "viewport": { "x": 154.5, "y": -220.25, "zoom": 0.85 },
      "elements": [
        {
          "type": "rectangle",
          "id": "22222222-2222-2222-2222-222222222222",
          "zIndex": 0,
          "bounds": { "x": 10, "y": 20, "width": 100, "height": 50 },
          "fill": "#FFFFFFFF",
          "stroke": "#37474FFF",
          "strokeThickness": 2
        },
        {
          "type": "ellipse",
          "id": "33333333-3333-3333-3333-333333333333",
          "zIndex": 1,
          "bounds": { "x": -80, "y": -40, "width": 60, "height": 30 },
          "fill": "#90CAF9FF",
          "stroke": "#1565C0FF",
          "strokeThickness": 1.5
        },
        {
          "type": "line",
          "id": "44444444-4444-4444-4444-444444444444",
          "zIndex": 2,
          "start": { "x": 0, "y": 0 },
          "end": { "x": 120, "y": 60 },
          "stroke": "#000000FF",
          "strokeThickness": 3
        },
        {
          "type": "arrow",
          "id": "55555555-5555-5555-5555-555555555555",
          "zIndex": 3,
          "start": { "x": -10, "y": -20 },
          "end": { "x": 90, "y": -20 },
          "stroke": "#00838FFF",
          "strokeThickness": 3
        },
        {
          "type": "text",
          "id": "66666666-6666-6666-6666-666666666666",
          "zIndex": 4,
          "position": { "x": 5, "y": 6 },
          "text": "ola",
          "fontSize": 24,
          "foreground": "#212121FF",
          "size": { "width": 120, "height": 30 }
        },
        {
          "type": "path",
          "id": "77777777-7777-7777-7777-777777777777",
          "zIndex": 5,
          "points": [0, 0, 10, 10, 20, 0],
          "stroke": "#212121FF",
          "strokeThickness": 3
        }
      ]
    }
    """;

    private static BoardLoadResult Load(string json) => BoardSerializer.ToBoard(BoardSerializer.ReadSnapshot(json), []);

    private static string CanonicalWith(string original, string replacement) =>
        CanonicalV1.Replace(original, replacement, StringComparison.Ordinal);

    [Fact]
    public void CanonicalVersion1_LoadsWithExpectedContent()
    {
        var result = Load(CanonicalV1);

        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), result.Document.Id);
        Assert.Equal("Quadro canonico", result.Document.Name);
        Assert.Equal(154.5, result.ViewportPosition.X, Tolerance);
        Assert.Equal(-220.25, result.ViewportPosition.Y, Tolerance);
        Assert.Equal(0.85, result.Zoom, Tolerance);
        Assert.Equal(6, result.Document.Elements.Count);
    }

    [Fact]
    public void CanonicalVersion1_MapsEveryElementType()
    {
        var elements = Load(CanonicalV1).Document.Elements;

        var rectangle = Assert.IsType<RectangleElement>(elements[0]);
        Assert.Equal(new RectD(10, 20, 100, 50), rectangle.Bounds);
        Assert.Equal(new ColorRgba(0x37, 0x47, 0x4F), rectangle.Stroke);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), rectangle.Id);

        var ellipse = Assert.IsType<EllipseElement>(elements[1]);
        Assert.Equal(new RectD(-80, -40, 60, 30), ellipse.Bounds);
        Assert.Equal(1, ellipse.ZIndex);

        var line = Assert.IsType<LineElement>(elements[2]);
        Assert.Equal(new PointD(120, 60), line.End);

        var arrow = Assert.IsType<ArrowElement>(elements[3]);
        Assert.Equal(new PointD(-10, -20), arrow.Start);

        var text = Assert.IsType<TextElement>(elements[4]);
        Assert.Equal("ola", text.Text);
        Assert.Equal(24, text.FontSize, Tolerance);
        Assert.Equal(120, text.Bounds.Width, Tolerance);

        var path = Assert.IsType<PathElement>(elements[5]);
        Assert.Equal(3, path.Points.Count);
        Assert.Equal(new PointD(20, 0), path.Points[^1]);
    }

    [Fact]
    public void NewerFormatVersion_IsRejected()
    {
        var exception = Assert.Throws<BoardFormatException>(
            () => Load(CanonicalWith("\"formatVersion\": 1", "\"formatVersion\": 99")));

        Assert.Contains("newer version", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingOrInvalidFormatVersion_IsRejected()
    {
        Assert.Throws<BoardFormatException>(() => Load("""{ "name": "x", "elements": [] }"""));
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith("\"formatVersion\": 1", "\"formatVersion\": 0")));
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith("\"formatVersion\": 1", "\"formatVersion\": \"um\"")));
    }

    [Fact]
    public void MalformedJson_IsRejected()
    {
        Assert.Throws<BoardFormatException>(() => Load("{ not json"));
        Assert.Throws<BoardFormatException>(() => Load("[]"));
    }

    [Fact]
    public void UnknownElementType_IsRejected()
    {
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith("\"type\": \"ellipse\"", "\"type\": \"hexagon\"")));
    }

    [Fact]
    public void MissingRequiredField_IsRejected()
    {
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith("\"fontSize\": 24,", string.Empty)));
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith("\"viewport\": { \"x\": 154.5, \"y\": -220.25, \"zoom\": 0.85 },", string.Empty)));
    }

    [Fact]
    public void DuplicatedElementId_IsRejected()
    {
        var duplicated = CanonicalWith(
            "\"id\": \"33333333-3333-3333-3333-333333333333\"",
            "\"id\": \"22222222-2222-2222-2222-222222222222\"");

        var exception = Assert.Throws<BoardFormatException>(() => Load(duplicated));

        Assert.Contains("Duplicated", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmptyElementId_IsRejected()
    {
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith(
            "\"id\": \"44444444-4444-4444-4444-444444444444\"",
            "\"id\": \"00000000-0000-0000-0000-000000000000\"")));
    }

    [Fact]
    public void InvalidGuid_IsRejected()
    {
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith(
            "\"id\": \"44444444-4444-4444-4444-444444444444\"",
            "\"id\": \"not-a-guid\"")));
    }

    [Theory]
    [InlineData("\"zoom\": 0.85", "\"zoom\": 0")]
    [InlineData("\"zoom\": 0.85", "\"zoom\": -1")]
    [InlineData("\"fontSize\": 24", "\"fontSize\": 0")]
    [InlineData("\"strokeThickness\": 2", "\"strokeThickness\": -3")]
    [InlineData("\"width\": 100", "\"width\": -5")]
    public void OutOfRangeNumbers_AreRejected(string original, string replacement)
    {
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith(original, replacement)));
    }

    [Fact]
    public void NonFiniteNumbers_AreRejected()
    {
        // JSON has no NaN literal, so a runaway value arrives as a huge number.
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith("\"x\": 154.5", "\"x\": 1e400")));
    }

    [Fact]
    public void InvalidColor_IsRejected()
    {
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith("\"fill\": \"#FFFFFFFF\"", "\"fill\": \"branco\"")));
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith("\"fill\": \"#FFFFFFFF\"", "\"fill\": \"#GGGGGGGG\"")));
    }

    [Fact]
    public void SixDigitColor_IsAcceptedAsOpaque()
    {
        var result = Load(CanonicalWith("\"fill\": \"#FFFFFFFF\"", "\"fill\": \"#102030\""));

        var rectangle = Assert.IsType<RectangleElement>(result.Document.Elements[0]);
        Assert.Equal(new ColorRgba(0x10, 0x20, 0x30, 255), rectangle.Fill);
    }

    [Theory]
    [InlineData("[0, 0, 10, 10, 20, 0]", "[0, 0, 10]")]
    [InlineData("[0, 0, 10, 10, 20, 0]", "[]")]
    [InlineData("[0, 0, 10, 10, 20, 0]", "[5]")]
    public void InvalidPathPoints_AreRejected(string original, string replacement)
    {
        Assert.Throws<BoardFormatException>(() => Load(CanonicalWith(original, replacement)));
    }

    [Fact]
    public void SerializedOutput_LoadsBackAsTheSameBoard()
    {
        var loaded = Load(CanonicalV1);
        var reserialized = BoardSerializer.Serialize(
            BoardSerializer.CreateSnapshot(loaded.Document, new Core.Viewports.Viewport
            {
                Position = loaded.ViewportPosition,
                Zoom = loaded.Zoom,
                ViewportSize = new SizeD(800, 600),
            }).Board);

        var second = Load(reserialized);

        Assert.Equal(loaded.Document.Elements.Count, second.Document.Elements.Count);
        Assert.Equal(loaded.Document.Id, second.Document.Id);
        Assert.Equal(loaded.Zoom, second.Zoom, Tolerance);
    }
}
