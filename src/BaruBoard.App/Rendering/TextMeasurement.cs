using System.Globalization;
using Avalonia.Media;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.App.Rendering;

// Text bounds are derived layout state; they are remeasured here whenever the
// text changes, and must not be treated as authoritative across platforms.
internal static class TextMeasurement
{
    public static readonly Typeface Typeface = Typeface.Default;

    // Sizes stored in a file are only a hint: fonts differ between machines, so
    // every loaded text is measured again before the board is shown.
    public static void Remeasure(BoardDocument document)
    {
        foreach (var element in document.Elements)
        {
            if (element is TextElement text && text.Text.Length > 0)
                text.SetMeasuredSize(Measure(text.Text, text.FontSize));
        }
    }

    public static SizeD Measure(string text, double fontSize)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            Typeface,
            fontSize,
            null);

        return new SizeD(
            Math.Max(formatted.WidthIncludingTrailingWhitespace, fontSize / 2),
            Math.Max(formatted.Height, fontSize));
    }
}
