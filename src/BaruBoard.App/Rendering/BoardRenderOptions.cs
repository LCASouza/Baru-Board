using BaruBoard.Core.Boards;

namespace BaruBoard.App.Rendering;

/// <summary>
/// What a single render pass should include. It is a value so nothing leaks
/// between an interactive frame and an export.
/// </summary>
public readonly record struct BoardRenderOptions
{
    public static BoardRenderOptions Interactive { get; } = new()
    {
        DrawGrid = true,
        DrawBackground = true,
    };

    public bool DrawGrid { get; init; }

    public bool DrawBackground { get; init; }

    /// <summary>
    /// Element hidden from this pass, used while it is being edited in place.
    /// </summary>
    public BoardElement? SkipElement { get; init; }

    /// <summary>
    /// When set, only these elements are drawn. Exporting a selection means
    /// exporting those elements, not everything that overlaps their bounds.
    /// </summary>
    public IReadOnlyCollection<BoardElement>? ElementFilter { get; init; }
}
