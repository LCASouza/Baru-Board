using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Editing;

/// <summary>
/// Applies grid snapping on behalf of the tools, combining the configured grid
/// with the momentary suppression modifier.
/// </summary>
public sealed class SnapContext
{
    private readonly GridSettings _grid;
    private readonly EditorInteractionState _interaction;

    public SnapContext(GridSettings grid, EditorInteractionState interaction)
    {
        _grid = grid;
        _interaction = interaction;
    }

    public bool IsActive => _grid.SnapEnabled && !_interaction.IsSnapSuppressed;

    public PointD SnapPoint(PointD worldPoint) =>
        IsActive ? GridSnap.SnapPoint(worldPoint, _grid.LogicalStep) : worldPoint;

    public double SnapValue(double worldValue) =>
        IsActive ? GridSnap.SnapValue(worldValue, _grid.LogicalStep) : worldValue;
}
