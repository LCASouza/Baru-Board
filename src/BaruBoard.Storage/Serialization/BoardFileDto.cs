using System.Text.Json.Serialization;

namespace BaruBoard.Storage.Serialization;

/// <summary>
/// Detached snapshot of a board, and the on-disk contract of the .baru format.
/// It is deliberately independent from the domain model so refactoring elements
/// never changes the meaning of files already written.
/// </summary>
public sealed class BoardFileDto
{
    public required int FormatVersion { get; init; }

    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required ViewportDto Viewport { get; init; }

    public required IReadOnlyList<ElementDto> Elements { get; init; }

    // Absent in version 1 files, which predate assets.
    public IReadOnlyList<AssetDto>? Assets { get; init; }
}

public sealed class AssetDto
{
    public required string Id { get; init; }

    public required string MediaType { get; init; }
}

public sealed class ViewportDto
{
    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Zoom { get; init; }
}

public sealed class PointDto
{
    public required double X { get; init; }

    public required double Y { get; init; }
}

public sealed class SizeDto
{
    public required double Width { get; init; }

    public required double Height { get; init; }
}

public sealed class RectDto
{
    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RectangleElementDto), "rectangle")]
[JsonDerivedType(typeof(EllipseElementDto), "ellipse")]
[JsonDerivedType(typeof(LineElementDto), "line")]
[JsonDerivedType(typeof(ArrowElementDto), "arrow")]
[JsonDerivedType(typeof(TextElementDto), "text")]
[JsonDerivedType(typeof(PathElementDto), "path")]
[JsonDerivedType(typeof(ImageElementDto), "image")]
public abstract class ElementDto
{
    public required Guid Id { get; init; }

    public required int ZIndex { get; init; }
}

public abstract class ShapeElementDto : ElementDto
{
    public required RectDto Bounds { get; init; }

    public required string Fill { get; init; }

    public required string Stroke { get; init; }

    public required double StrokeThickness { get; init; }
}

public sealed class RectangleElementDto : ShapeElementDto;

public sealed class EllipseElementDto : ShapeElementDto;

public abstract class SegmentElementDto : ElementDto
{
    public required PointDto Start { get; init; }

    public required PointDto End { get; init; }

    public required string Stroke { get; init; }

    public required double StrokeThickness { get; init; }
}

public sealed class LineElementDto : SegmentElementDto;

public sealed class ArrowElementDto : SegmentElementDto;

public sealed class TextElementDto : ElementDto
{
    public required PointDto Position { get; init; }

    public required string Text { get; init; }

    public required double FontSize { get; init; }

    public required string Foreground { get; init; }

    // Layout hint only: the UI remeasures text after loading because fonts differ
    // between machines.
    public required SizeDto Size { get; init; }
}

public sealed class ImageElementDto : ElementDto
{
    public required RectDto Bounds { get; init; }

    public required string AssetId { get; init; }
}

public sealed class PathElementDto : ElementDto
{
    // Flat [x0, y0, x1, y1, ...] pairs; strokes carry hundreds of points and the
    // object form nearly doubles the file size.
    public required IReadOnlyList<double> Points { get; init; }

    public required string Stroke { get; init; }

    public required double StrokeThickness { get; init; }
}
