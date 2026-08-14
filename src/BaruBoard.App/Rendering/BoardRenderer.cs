using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.App.Rendering;

public sealed class BoardRenderer
{
    private static readonly ImmutableSolidColorBrush Background = new(Color.FromRgb(0xF2, 0xF2, 0xF2));
    private static readonly ImmutableSolidColorBrush MissingImageFill = new(Color.FromRgb(0xE0, 0xE0, 0xE0));
    private static readonly ImmutablePen MissingImagePen = new(new ImmutableSolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)), 1);

    // Translucent instead of solid: the grid should sit behind the content, not
    // compete with it.
    private static readonly ImmutableSolidColorBrush GridBrush = new(Color.FromArgb(0x0F, 0, 0, 0));
    private static readonly ImmutableSolidColorBrush GridMajorBrush = new(Color.FromArgb(0x1F, 0, 0, 0));

    // Geometry building and text shaping dominate the cost of a render pass, so
    // both are cached per element and rebuilt only when their source changes.
    private readonly Dictionary<ColorRgba, ImmutableSolidColorBrush> _brushes = [];
    private readonly Dictionary<(ColorRgba Color, double Thickness, bool Round), ImmutablePen> _pens = [];
    private readonly Dictionary<PathElement, (int Version, StreamGeometry Geometry)> _pathGeometries = [];
    private readonly Dictionary<TextElement, CachedText> _texts = [];

    private readonly IImageBitmapProvider _images;
    private readonly GridSettings _grid;

    public BoardRenderer(IImageBitmapProvider images, GridSettings grid)
    {
        _images = images;
        _grid = grid;
    }

    /// <summary>
    /// Drops every cached resource. Entries are keyed by element instance, so a
    /// new document must not keep the previous one's geometry alive.
    /// </summary>
    public void ClearCaches()
    {
        _brushes.Clear();
        _pens.Clear();
        _pathGeometries.Clear();
        _texts.Clear();
    }

    /// <summary>
    /// Draws board content only. Editor decorations such as selection handles,
    /// the marquee or diagnostics are not this renderer's business, which is why
    /// an export never picks them up.
    /// </summary>
    public void Render(DrawingContext context, BoardDocument document, Viewport viewport, BoardRenderOptions options)
    {
        var size = viewport.ViewportSize;
        if (size.Width <= 0 || size.Height <= 0)
            return;

        // A transparent export must keep the target untouched here.
        if (options.DrawBackground)
            context.FillRectangle(Background, new Rect(0, 0, size.Width, size.Height));

        var worldToScreen =
            Matrix.CreateTranslation(-viewport.Position.X, -viewport.Position.Y) *
            Matrix.CreateScale(viewport.Zoom, viewport.Zoom);

        using var transform = context.PushTransform(worldToScreen);

        if (options.DrawGrid)
            DrawGrid(context, viewport);

        var visibleElements = document
            .GetElementsIntersecting(viewport.VisibleWorldBounds)
            .OrderBy(element => element.ZIndex);

        foreach (var element in visibleElements)
        {
            if (ReferenceEquals(element, options.SkipElement))
                continue;

            if (options.ElementFilter is { } filter && !filter.Contains(element))
                continue;

            switch (element)
            {
                case RectangleElement rectangle:
                    DrawRectangle(context, rectangle);
                    break;
                case EllipseElement ellipse:
                    DrawEllipse(context, ellipse);
                    break;
                // ArrowElement extends LineElement, so it must be matched first.
                case ArrowElement arrow:
                    DrawArrow(context, arrow);
                    break;
                case LineElement line:
                    DrawLine(context, line);
                    break;
                case TextElement text:
                    DrawText(context, text);
                    break;
                case PathElement path:
                    DrawPath(context, path);
                    break;
                case ImageElement image:
                    DrawImage(context, image, document, viewport.Zoom);
                    break;
            }
        }
    }

    // Lines are placed from the visible world bounds, and their thickness is
    // divided by the zoom so they stay one device pixel wide at any scale.
    private void DrawGrid(DrawingContext context, Viewport viewport)
    {
        if (!_grid.IsVisible)
            return;

        var bounds = viewport.VisibleWorldBounds;
        var step = GridGeometry.GetDisplayStep(_grid.LogicalStep, viewport.Zoom);
        // Both levels stay one device pixel wide; only the opacity separates them.
        var thickness = 1 / viewport.Zoom;
        var minorPen = new ImmutablePen(GridBrush, thickness);
        var majorPen = new ImmutablePen(GridMajorBrush, thickness);

        foreach (var x in GridGeometry.GetLines(bounds.Left, bounds.Right, step))
        {
            var pen = GridGeometry.IsMajorLine(x, step) ? majorPen : minorPen;
            context.DrawLine(pen, new Point(x, bounds.Top), new Point(x, bounds.Bottom));
        }

        foreach (var y in GridGeometry.GetLines(bounds.Top, bounds.Bottom, step))
        {
            var pen = GridGeometry.IsMajorLine(y, step) ? majorPen : minorPen;
            context.DrawLine(pen, new Point(bounds.Left, y), new Point(bounds.Right, y));
        }
    }

    private void DrawRectangle(DrawingContext context, RectangleElement rectangle)
    {
        context.DrawRectangle(
            GetBrush(rectangle.Fill),
            GetPen(rectangle.Stroke, rectangle.StrokeThickness),
            rectangle.Bounds.ToAvalonia());
    }

    private void DrawEllipse(DrawingContext context, EllipseElement ellipse)
    {
        var bounds = ellipse.Bounds;
        var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        context.DrawEllipse(
            GetBrush(ellipse.Fill),
            GetPen(ellipse.Stroke, ellipse.StrokeThickness),
            center,
            bounds.Width / 2,
            bounds.Height / 2);
    }

    private void DrawLine(DrawingContext context, LineElement line)
    {
        if (GetPen(line.Stroke, line.StrokeThickness) is not { } pen)
            return;

        context.DrawLine(pen, line.Start.ToAvalonia(), line.End.ToAvalonia());
    }

    private void DrawArrow(DrawingContext context, ArrowElement arrow)
    {
        DrawLine(context, arrow);

        var (left, right) = ArrowGeometry.GetHeadPoints(
            arrow.Start, arrow.End, arrow.HeadLength, ArrowGeometry.DefaultHeadAngle);

        var head = new StreamGeometry();
        using (var geometry = head.Open())
        {
            geometry.BeginFigure(arrow.End.ToAvalonia(), isFilled: true);
            geometry.LineTo(left.ToAvalonia());
            geometry.LineTo(right.ToAvalonia());
            geometry.EndFigure(isClosed: true);
        }

        context.DrawGeometry(GetBrush(arrow.Stroke), null, head);
    }

    private void DrawText(DrawingContext context, TextElement text)
    {
        if (text.Text.Length == 0)
            return;

        context.DrawText(GetFormattedText(text), text.Bounds.Position.ToAvalonia());
    }

    private void DrawPath(DrawingContext context, PathElement path)
    {
        if (path.Points.Count == 1)
        {
            var radius = path.StrokeThickness / 2;
            context.DrawEllipse(GetBrush(path.Stroke), null, path.Points[0].ToAvalonia(), radius, radius);
            return;
        }

        var pen = GetPen(path.Stroke, path.StrokeThickness, round: true);
        if (pen is null)
            return;

        context.DrawGeometry(null, pen, GetPathGeometry(path));
    }

    private StreamGeometry GetPathGeometry(PathElement path)
    {
        if (_pathGeometries.TryGetValue(path, out var cached) && cached.Version == path.GeometryVersion)
            return cached.Geometry;

        var geometry = new StreamGeometry();
        using (var figure = geometry.Open())
        {
            figure.BeginFigure(path.Points[0].ToAvalonia(), isFilled: false);
            foreach (var segment in PathSmoothing.GetSegments(path.Points))
                figure.QuadraticBezierTo(segment.Control.ToAvalonia(), segment.End.ToAvalonia());
            figure.EndFigure(isClosed: false);
        }

        _pathGeometries[path] = (path.GeometryVersion, geometry);
        return geometry;
    }

    // Text layout is rebuilt only when something that affects it actually
    // changed, which is cheaper to check than to redo.
    private FormattedText GetFormattedText(TextElement text)
    {
        if (_texts.TryGetValue(text, out var cached) &&
            cached.FontSize.Equals(text.FontSize) &&
            cached.Foreground == text.Foreground &&
            string.Equals(cached.Text, text.Text, StringComparison.Ordinal))
        {
            return cached.Formatted;
        }

        var formatted = new FormattedText(
            text.Text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            TextMeasurement.Typeface,
            text.FontSize,
            GetBrush(text.Foreground));

        _texts[text] = new CachedText(text.Text, text.FontSize, text.Foreground, formatted);
        return formatted;
    }

    private ImmutableSolidColorBrush GetBrush(ColorRgba color)
    {
        if (_brushes.TryGetValue(color, out var brush))
            return brush;

        brush = new ImmutableSolidColorBrush(color.ToAvalonia());
        _brushes[color] = brush;
        return brush;
    }

    private ImmutablePen? GetPen(ColorRgba color, double thickness, bool round = false)
    {
        if (thickness <= 0)
            return null;

        var key = (color, thickness, round);
        if (_pens.TryGetValue(key, out var pen))
            return pen;

        pen = round
            ? new ImmutablePen(GetBrush(color), thickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round)
            : new ImmutablePen(GetBrush(color), thickness);

        _pens[key] = pen;
        return pen;
    }

    private readonly record struct CachedText(
        string Text, double FontSize, ColorRgba Foreground, FormattedText Formatted);

    // An asset that cannot be decoded still occupies its place on the board
    // instead of silently disappearing.
    private void DrawImage(DrawingContext context, ImageElement image, BoardDocument document, double outputScale)
    {
        var rect = image.Bounds.ToAvalonia();
        if (_images.GetImage(document, image, outputScale) is { } bitmap)
        {
            context.DrawImage(bitmap, new Rect(bitmap.Size), rect);
            return;
        }

        context.DrawRectangle(MissingImageFill, MissingImagePen, rect);
        context.DrawLine(MissingImagePen, rect.TopLeft, rect.BottomRight);
        context.DrawLine(MissingImagePen, rect.TopRight, rect.BottomLeft);
    }

    private static ImmutablePen? CreatePen(ColorRgba stroke, double thickness) =>
        thickness > 0
            ? new ImmutablePen(new ImmutableSolidColorBrush(stroke.ToAvalonia()), thickness)
            : null;
}
