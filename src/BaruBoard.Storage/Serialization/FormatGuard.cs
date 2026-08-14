using BaruBoard.Core.Geometry;

namespace BaruBoard.Storage.Serialization;

// Everything coming from a file is untrusted: structurally valid JSON must still
// be rejected when it would build an inconsistent document.
internal static class FormatGuard
{
    public static double Finite(double value, string field)
    {
        if (!double.IsFinite(value))
            throw new BoardFormatException($"'{field}' must be a finite number.");

        return value;
    }

    public static double NonNegative(double value, string field)
    {
        Finite(value, field);
        if (value < 0)
            throw new BoardFormatException($"'{field}' must not be negative.");

        return value;
    }

    public static double Positive(double value, string field)
    {
        Finite(value, field);
        if (value <= 0)
            throw new BoardFormatException($"'{field}' must be greater than zero.");

        return value;
    }

    public static T NotNull<T>(T? value, string field)
        where T : class
    {
        if (value is null)
            throw new BoardFormatException($"'{field}' is missing.");

        return value;
    }

    public static ColorRgba Color(string? value, string field)
    {
        if (!ColorHex.TryParse(value, out var color))
            throw new BoardFormatException($"'{field}' is not a valid #RRGGBBAA color.");

        return color;
    }

    public static PointD Point(PointDto? dto, string field)
    {
        NotNull(dto, field);
        return new PointD(Finite(dto!.X, $"{field}.x"), Finite(dto.Y, $"{field}.y"));
    }

    public static RectD Rect(RectDto? dto, string field)
    {
        NotNull(dto, field);
        return new RectD(
            Finite(dto!.X, $"{field}.x"),
            Finite(dto.Y, $"{field}.y"),
            NonNegative(dto.Width, $"{field}.width"),
            NonNegative(dto.Height, $"{field}.height"));
    }

    public static SizeD Size(SizeDto? dto, string field)
    {
        NotNull(dto, field);
        return new SizeD(
            NonNegative(dto!.Width, $"{field}.width"),
            NonNegative(dto.Height, $"{field}.height"));
    }
}
