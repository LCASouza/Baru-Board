using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Exporting;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.App.Rendering;

/// <summary>
/// Renders a world region to a PNG using the very same renderer that paints the
/// screen, so what is exported cannot drift from what is displayed. Nothing here
/// captures the window.
/// </summary>
public static class BoardExporter
{
    // 96 DPI keeps one device-independent pixel equal to one output pixel, so the
    // file resolution never depends on the monitor's scaling.
    private static readonly Vector ExportDpi = new(96, 96);

    public static void ExportPng(
        string path,
        BoardDocument document,
        ExportPlan plan,
        bool transparentBackground,
        IReadOnlyCollection<BoardElement>? elementFilter = null)
    {
        using var images = new ExportImageProvider();
        var renderer = new BoardRenderer(images, new GridSettings { IsVisible = false });

        var viewport = new Viewport
        {
            Position = plan.WorldRegion.Position,
            Zoom = plan.EffectiveScale,
            ViewportSize = new SizeD(plan.PixelWidth, plan.PixelHeight),
        };

        var options = new BoardRenderOptions
        {
            DrawGrid = false,
            DrawBackground = !transparentBackground,
            ElementFilter = elementFilter,
        };

        using var bitmap = new RenderTargetBitmap(new PixelSize(plan.PixelWidth, plan.PixelHeight), ExportDpi);
        using (var context = bitmap.CreateDrawingContext())
        {
            // Subpixel antialiasing assumes a known opaque backdrop and smears
            // glyphs over a transparent one, so an exported file always uses
            // grayscale antialiasing. TextOptions is not an alternative here:
            // it is an attached property of a visual, and this pass renders
            // straight into a bitmap without a visual tree.
#pragma warning disable CS0618
            var renderOptions = new RenderOptions
            {
                TextRenderingMode = TextRenderingMode.Antialias,
                EdgeMode = EdgeMode.Antialias,
                BitmapInterpolationMode = BitmapInterpolationMode.HighQuality,
            };
#pragma warning restore CS0618

            using (context.PushRenderOptions(renderOptions))
            {
                renderer.Render(context, document, viewport, options);
            }
        }

        bitmap.Save(path, new PngBitmapEncoderOptions());
    }
}
