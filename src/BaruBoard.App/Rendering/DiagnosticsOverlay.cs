using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Viewports;

namespace BaruBoard.App.Rendering;

/// <summary>
/// Optional measurement overlay. What it times is the CPU cost of preparing and
/// issuing one board render pass — not the full frame that reaches the screen,
/// which Avalonia composes afterwards.
/// </summary>
public sealed class DiagnosticsOverlay
{
    private const int SampleCount = 30;

    private static readonly ImmutableSolidColorBrush PanelBrush = new(Color.FromArgb(0xC0, 0x21, 0x21, 0x21));
    private static readonly ImmutableSolidColorBrush TextBrush = new(Colors.White);
    private static readonly Typeface OverlayTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);

    private readonly double[] _samples = new double[SampleCount];
    private readonly Stopwatch _stopwatch = new();
    private int _sampleIndex;
    private int _sampleTotal;
    private int _visibleElements;

    public bool IsEnabled { get; set; }

    public void MeasureBoardRender(Action render)
    {
        _stopwatch.Restart();
        render();
        _stopwatch.Stop();

        _samples[_sampleIndex] = _stopwatch.Elapsed.TotalMilliseconds;
        _sampleIndex = (_sampleIndex + 1) % SampleCount;
        _sampleTotal = Math.Min(_sampleTotal + 1, SampleCount);
    }

    public void Render(DrawingContext context, BoardDocument document, Viewport viewport)
    {
        if (!IsEnabled)
            return;

        // Counting visible elements has a cost of its own, so it only runs while
        // the overlay is enabled.
        _visibleElements = 0;
        foreach (var _ in document.GetElementsIntersecting(viewport.VisibleWorldBounds))
            _visibleElements++;

        var text = new FormattedText(
            BuildReport(document),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            OverlayTypeface,
            12,
            TextBrush);

        var panel = new Rect(8, 8, text.Width + 20, text.Height + 16);
        context.DrawRectangle(PanelBrush, null, panel, 4, 4);
        context.DrawText(text, new Point(panel.X + 10, panel.Y + 8));
    }

    private string BuildReport(BoardDocument document)
    {
        var average = 0.0;
        for (var i = 0; i < _sampleTotal; i++)
            average += _samples[i];

        if (_sampleTotal > 0)
            average /= _sampleTotal;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"""
            Board render CPU: {average:F2} ms (média de {_sampleTotal})
            Elementos: {_visibleElements} visíveis / {document.Elements.Count} totais
            Assets: {document.Assets.Count}
            """);
    }
}
