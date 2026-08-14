using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Editing;

public enum AlignmentMode
{
    Left,
    HorizontalCenter,
    Right,
    Top,
    VerticalCenter,
    Bottom,
}

public enum DistributionMode
{
    Horizontal,
    Vertical,
}

/// <summary>
/// Position math for aligning and distributing a selection. Everything is
/// returned as moves so a whole operation becomes a single history entry.
/// </summary>
public static class ElementArrangement
{
    public const int MinimumForAlignment = 2;

    public const int MinimumForDistribution = 3;

    public static IReadOnlyList<ElementMove> Align(IReadOnlyList<BoardElement> elements, AlignmentMode mode)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (elements.Count < MinimumForAlignment)
            return [];

        var bounds = GetUnionBounds(elements);
        var moves = new List<ElementMove>();

        foreach (var element in elements)
        {
            var position = element.Bounds.Position;
            var target = mode switch
            {
                AlignmentMode.Left => new PointD(bounds.Left, position.Y),
                AlignmentMode.HorizontalCenter => new PointD(
                    bounds.Left + (bounds.Width - element.Bounds.Width) / 2, position.Y),
                AlignmentMode.Right => new PointD(bounds.Right - element.Bounds.Width, position.Y),
                AlignmentMode.Top => new PointD(position.X, bounds.Top),
                AlignmentMode.VerticalCenter => new PointD(
                    position.X, bounds.Top + (bounds.Height - element.Bounds.Height) / 2),
                AlignmentMode.Bottom => new PointD(position.X, bounds.Bottom - element.Bounds.Height),
                _ => position,
            };

            if (target != position)
                moves.Add(new ElementMove(element, position, target));
        }

        return moves;
    }

    /// <summary>
    /// Spreads the inner elements so the gaps between consecutive bounds are
    /// equal. The outermost elements stay where they are.
    /// </summary>
    public static IReadOnlyList<ElementMove> Distribute(IReadOnlyList<BoardElement> elements, DistributionMode mode)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (elements.Count < MinimumForDistribution)
            return [];

        var horizontal = mode == DistributionMode.Horizontal;
        var ordered = elements
            .OrderBy(element => horizontal ? element.Bounds.Left : element.Bounds.Top)
            .ToList();

        var first = ordered[0].Bounds;
        var last = ordered[^1].Bounds;
        var span = horizontal ? last.Right - first.Left : last.Bottom - first.Top;

        var occupied = 0.0;
        foreach (var element in ordered)
            occupied += horizontal ? element.Bounds.Width : element.Bounds.Height;

        var gap = (span - occupied) / (ordered.Count - 1);
        var cursor = horizontal ? first.Right : first.Bottom;

        var moves = new List<ElementMove>();
        for (var i = 1; i < ordered.Count - 1; i++)
        {
            var element = ordered[i];
            var position = element.Bounds.Position;
            cursor += gap;

            var target = horizontal
                ? new PointD(cursor, position.Y)
                : new PointD(position.X, cursor);

            if (target != position)
                moves.Add(new ElementMove(element, position, target));

            cursor += horizontal ? element.Bounds.Width : element.Bounds.Height;
        }

        return moves;
    }

    private static RectD GetUnionBounds(IReadOnlyList<BoardElement> elements)
    {
        var left = double.MaxValue;
        var top = double.MaxValue;
        var right = double.MinValue;
        var bottom = double.MinValue;

        foreach (var element in elements)
        {
            left = Math.Min(left, element.Bounds.Left);
            top = Math.Min(top, element.Bounds.Top);
            right = Math.Max(right, element.Bounds.Right);
            bottom = Math.Max(bottom, element.Bounds.Bottom);
        }

        return new RectD(left, top, right - left, bottom - top);
    }
}
