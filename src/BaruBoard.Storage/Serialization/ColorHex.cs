using System.Globalization;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Storage.Serialization;

// Colors travel as #RRGGBBAA. Six digit values are accepted on read so hand
// edited files stay usable, but writing always emits the explicit alpha.
public static class ColorHex
{
    public static string ToHex(ColorRgba color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

    public static bool TryParse(string? text, out ColorRgba color)
    {
        color = default;
        if (text is null || text.Length is not (7 or 9) || text[0] != '#')
            return false;

        var digits = text.AsSpan(1);
        if (!TryParseByte(digits[..2], out var r) ||
            !TryParseByte(digits[2..4], out var g) ||
            !TryParseByte(digits[4..6], out var b))
        {
            return false;
        }

        byte a = 255;
        if (digits.Length == 8 && !TryParseByte(digits[6..8], out a))
            return false;

        color = new ColorRgba(r, g, b, a);
        return true;
    }

    private static bool TryParseByte(ReadOnlySpan<char> text, out byte value) =>
        byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
}
