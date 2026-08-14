namespace BaruBoard.Core.Editing;

/// <summary>
/// Editor configuration for the grid. <see cref="LogicalStep"/> is the only step
/// snapping ever uses; the renderer may draw multiples of it to keep a readable
/// density, which must never change where snapping lands.
/// </summary>
public sealed class GridSettings
{
    public const double DefaultStep = 80.0;

    private double _logicalStep = DefaultStep;

    public double LogicalStep
    {
        get => _logicalStep;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _logicalStep = value;
        }
    }

    public bool IsVisible { get; set; } = true;

    public bool SnapEnabled { get; set; }
}
