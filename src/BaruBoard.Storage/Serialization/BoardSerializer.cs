using System.Text.Json;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Storage.Serialization;

public static class BoardSerializer
{
    public const int FormatVersion = 2;

    public const int MinimumSupportedVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Captures the live document and viewport as a detached snapshot. Must run
    /// where the document is owned; everything afterwards is free of the domain.
    /// </summary>
    public static BoardSnapshot CreateSnapshot(BoardDocument document, Viewport viewport)
    {
        var elements = new List<ElementDto>(document.Elements.Count);
        foreach (var element in document.Elements)
            elements.Add(ToDto(element));

        // Orphan assets stay in memory for undo but are not worth persisting.
        var assets = document.GetReferencedAssets();

        var board = new BoardFileDto
        {
            FormatVersion = FormatVersion,
            Id = document.Id,
            Name = document.Name,
            Viewport = new ViewportDto
            {
                X = viewport.Position.X,
                Y = viewport.Position.Y,
                Zoom = viewport.Zoom,
            },
            Elements = elements,
            Assets = [.. assets.Select(asset => new AssetDto { Id = asset.Id, MediaType = asset.MediaType })],
        };

        return new BoardSnapshot(board, assets);
    }

    public static string Serialize(BoardFileDto snapshot) => JsonSerializer.Serialize(snapshot, Options);

    public static BoardFileDto ReadSnapshot(string json)
    {
        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new BoardFormatException("The file is not valid JSON.", exception);
        }

        using (parsed)
        {
            var root = parsed.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new BoardFormatException("The file does not contain a board object.");

            if (!root.TryGetProperty("formatVersion", out var versionProperty) ||
                versionProperty.ValueKind != JsonValueKind.Number ||
                !versionProperty.TryGetInt32(out var version))
            {
                throw new BoardFormatException("'formatVersion' is missing or invalid.");
            }

            if (version > FormatVersion)
                throw new BoardFormatException($"The file was created by a newer version (format {version}).");

            if (version < MinimumSupportedVersion)
                throw new BoardFormatException($"Unsupported format version {version}.");

            try
            {
                return root.Deserialize<BoardFileDto>(Options)
                    ?? throw new BoardFormatException("The file does not contain a board object.");
            }
            catch (JsonException exception)
            {
                throw new BoardFormatException($"The board could not be read: {exception.Message}", exception);
            }
        }
    }

    public static BoardLoadResult ToBoard(BoardFileDto snapshot, IReadOnlyList<BoardAsset> assets)
    {
        FormatGuard.NotNull(snapshot.Viewport, "viewport");
        FormatGuard.NotNull(snapshot.Elements, "elements");
        FormatGuard.NotNull(snapshot.Name, "name");

        if (snapshot.Elements.Count > BoardFormatLimits.MaxElements)
            throw new BoardFormatException($"The board declares more than {BoardFormatLimits.MaxElements} elements.");

        var document = new BoardDocument
        {
            Id = snapshot.Id,
            Name = snapshot.Name,
        };

        foreach (var asset in assets)
            document.AddAsset(asset);

        var seenIds = new HashSet<Guid>();
        foreach (var elementDto in snapshot.Elements)
        {
            FormatGuard.NotNull(elementDto, "elements[]");
            if (elementDto.Id == Guid.Empty)
                throw new BoardFormatException("An element has an empty id.");

            if (!seenIds.Add(elementDto.Id))
                throw new BoardFormatException($"Duplicated element id '{elementDto.Id}'.");

            var element = ToElement(elementDto);
            foreach (var assetId in element.RequiredAssetIds)
            {
                if (!document.ContainsAsset(assetId))
                    throw new BoardFormatException($"Element '{element.Id}' references missing asset '{assetId}'.");
            }

            document.AddElement(element);
        }

        var position = new PointD(
            FormatGuard.Finite(snapshot.Viewport.X, "viewport.x"),
            FormatGuard.Finite(snapshot.Viewport.Y, "viewport.y"));
        var zoom = FormatGuard.Positive(snapshot.Viewport.Zoom, "viewport.zoom");

        return new BoardLoadResult(document, position, zoom);
    }

    private static ElementDto ToDto(BoardElement element) => element switch
    {
        // ArrowElement extends LineElement, so it has to be matched first.
        ArrowElement arrow => new ArrowElementDto
        {
            Id = arrow.Id,
            ZIndex = arrow.ZIndex,
            Start = ToDto(arrow.Start),
            End = ToDto(arrow.End),
            Stroke = ColorHex.ToHex(arrow.Stroke),
            StrokeThickness = arrow.StrokeThickness,
        },
        LineElement line => new LineElementDto
        {
            Id = line.Id,
            ZIndex = line.ZIndex,
            Start = ToDto(line.Start),
            End = ToDto(line.End),
            Stroke = ColorHex.ToHex(line.Stroke),
            StrokeThickness = line.StrokeThickness,
        },
        RectangleElement rectangle => new RectangleElementDto
        {
            Id = rectangle.Id,
            ZIndex = rectangle.ZIndex,
            Bounds = ToDto(rectangle.Bounds),
            Fill = ColorHex.ToHex(rectangle.Fill),
            Stroke = ColorHex.ToHex(rectangle.Stroke),
            StrokeThickness = rectangle.StrokeThickness,
        },
        EllipseElement ellipse => new EllipseElementDto
        {
            Id = ellipse.Id,
            ZIndex = ellipse.ZIndex,
            Bounds = ToDto(ellipse.Bounds),
            Fill = ColorHex.ToHex(ellipse.Fill),
            Stroke = ColorHex.ToHex(ellipse.Stroke),
            StrokeThickness = ellipse.StrokeThickness,
        },
        TextElement text => new TextElementDto
        {
            Id = text.Id,
            ZIndex = text.ZIndex,
            Position = ToDto(text.Bounds.Position),
            Text = text.Text,
            FontSize = text.FontSize,
            Foreground = ColorHex.ToHex(text.Foreground),
            Size = new SizeDto { Width = text.Bounds.Width, Height = text.Bounds.Height },
        },
        ImageElement image => new ImageElementDto
        {
            Id = image.Id,
            ZIndex = image.ZIndex,
            Bounds = ToDto(image.Bounds),
            AssetId = image.AssetId,
        },
        PathElement path => new PathElementDto
        {
            Id = path.Id,
            ZIndex = path.ZIndex,
            Points = Flatten(path.Points),
            Stroke = ColorHex.ToHex(path.Stroke),
            StrokeThickness = path.StrokeThickness,
        },
        _ => throw new BoardFormatException($"Element type '{element.GetType().Name}' cannot be serialized."),
    };

    private static BoardElement ToElement(ElementDto dto) => dto switch
    {
        RectangleElementDto rectangle => new RectangleElement(FormatGuard.Rect(rectangle.Bounds, "bounds"))
        {
            Id = rectangle.Id,
            ZIndex = rectangle.ZIndex,
            Fill = FormatGuard.Color(rectangle.Fill, "fill"),
            Stroke = FormatGuard.Color(rectangle.Stroke, "stroke"),
            StrokeThickness = FormatGuard.NonNegative(rectangle.StrokeThickness, "strokeThickness"),
        },
        EllipseElementDto ellipse => new EllipseElement(FormatGuard.Rect(ellipse.Bounds, "bounds"))
        {
            Id = ellipse.Id,
            ZIndex = ellipse.ZIndex,
            Fill = FormatGuard.Color(ellipse.Fill, "fill"),
            Stroke = FormatGuard.Color(ellipse.Stroke, "stroke"),
            StrokeThickness = FormatGuard.NonNegative(ellipse.StrokeThickness, "strokeThickness"),
        },
        ArrowElementDto arrow => new ArrowElement(
            FormatGuard.Point(arrow.Start, "start"),
            FormatGuard.Point(arrow.End, "end"))
        {
            Id = arrow.Id,
            ZIndex = arrow.ZIndex,
            Stroke = FormatGuard.Color(arrow.Stroke, "stroke"),
            StrokeThickness = FormatGuard.NonNegative(arrow.StrokeThickness, "strokeThickness"),
        },
        LineElementDto line => new LineElement(
            FormatGuard.Point(line.Start, "start"),
            FormatGuard.Point(line.End, "end"))
        {
            Id = line.Id,
            ZIndex = line.ZIndex,
            Stroke = FormatGuard.Color(line.Stroke, "stroke"),
            StrokeThickness = FormatGuard.NonNegative(line.StrokeThickness, "strokeThickness"),
        },
        ImageElementDto image => new ImageElement(
            FormatGuard.Rect(image.Bounds, "bounds"),
            ValidAssetId(image.AssetId))
        {
            Id = image.Id,
            ZIndex = image.ZIndex,
        },
        TextElementDto text => ToTextElement(text),
        PathElementDto path => ToPathElement(path),
        _ => throw new BoardFormatException($"Unknown element type '{dto.GetType().Name}'."),
    };

    private static string ValidAssetId(string? assetId) =>
        BoardAsset.IsValidId(assetId)
            ? assetId!
            : throw new BoardFormatException($"'assetId' value '{assetId}' is not a valid asset id.");

    private static TextElement ToTextElement(TextElementDto dto)
    {
        var element = new TextElement(
            FormatGuard.Point(dto.Position, "position"),
            FormatGuard.NotNull(dto.Text, "text"),
            FormatGuard.Positive(dto.FontSize, "fontSize"))
        {
            Id = dto.Id,
            ZIndex = dto.ZIndex,
            Foreground = FormatGuard.Color(dto.Foreground, "foreground"),
        };

        element.SetMeasuredSize(FormatGuard.Size(dto.Size, "size"));
        return element;
    }

    private static PathElement ToPathElement(PathElementDto dto)
    {
        FormatGuard.NotNull(dto.Points, "points");
        if (dto.Points.Count < 2)
            throw new BoardFormatException("'points' needs at least one x/y pair.");

        if (dto.Points.Count % 2 != 0)
            throw new BoardFormatException("'points' must contain an even number of coordinates.");

        var points = new List<PointD>(dto.Points.Count / 2);
        for (var i = 0; i < dto.Points.Count; i += 2)
        {
            points.Add(new PointD(
                FormatGuard.Finite(dto.Points[i], "points[]"),
                FormatGuard.Finite(dto.Points[i + 1], "points[]")));
        }

        var element = new PathElement(points[0])
        {
            Id = dto.Id,
            ZIndex = dto.ZIndex,
            Stroke = FormatGuard.Color(dto.Stroke, "stroke"),
            StrokeThickness = FormatGuard.NonNegative(dto.StrokeThickness, "strokeThickness"),
        };

        element.SetPoints(points);
        return element;
    }

    private static PointDto ToDto(PointD point) => new() { X = point.X, Y = point.Y };

    private static RectDto ToDto(RectD rect) => new()
    {
        X = rect.X,
        Y = rect.Y,
        Width = rect.Width,
        Height = rect.Height,
    };

    private static double[] Flatten(IReadOnlyList<PointD> points)
    {
        var values = new double[points.Count * 2];
        for (var i = 0; i < points.Count; i++)
        {
            values[i * 2] = points[i].X;
            values[i * 2 + 1] = points[i].Y;
        }

        return values;
    }
}
