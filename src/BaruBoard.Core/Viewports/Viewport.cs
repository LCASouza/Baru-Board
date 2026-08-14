using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Viewports;

/// <summary>
/// Camera over the board's world space.
/// <see cref="Position"/> is the world coordinate visible at the screen origin (top-left corner).
/// <see cref="Zoom"/> is expressed in device-independent pixels per world unit; OS DPI scaling
/// is handled entirely by the UI layer and never enters this math.
/// </summary>
public sealed class Viewport
{
    private double _zoom = 1.0;

    public Viewport()
        : this(new ViewportOptions())
    {
    }

    public Viewport(ViewportOptions options)
    {
        if (options.MinZoom <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MinZoom must be positive.");
        if (options.MaxZoom < options.MinZoom)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxZoom must be greater than or equal to MinZoom.");
        if (options.ZoomStepFactor <= 1)
            throw new ArgumentOutOfRangeException(nameof(options), "ZoomStepFactor must be greater than 1.");

        Options = options;
    }

    public ViewportOptions Options { get; }

    public PointD Position { get; set; }

    public SizeD ViewportSize { get; set; }

    public double Zoom
    {
        get => _zoom;
        set => _zoom = Math.Clamp(value, Options.MinZoom, Options.MaxZoom);
    }

    public RectD VisibleWorldBounds =>
        new(Position.X, Position.Y, ViewportSize.Width / _zoom, ViewportSize.Height / _zoom);

    public PointD WorldToScreen(PointD world) =>
        new((world.X - Position.X) * _zoom, (world.Y - Position.Y) * _zoom);

    public PointD ScreenToWorld(PointD screen) =>
        new(screen.X / _zoom + Position.X, screen.Y / _zoom + Position.Y);

    /// <summary>
    /// Moves the camera so the content follows a pointer drag of <paramref name="screenDelta"/>.
    /// </summary>
    public void Pan(VectorD screenDelta) => Position -= screenDelta / _zoom;

    /// <summary>
    /// Applies the new zoom while keeping the world point under <paramref name="screenPoint"/>
    /// stationary on screen.
    /// </summary>
    public void ZoomAt(PointD screenPoint, double newZoom)
    {
        var clamped = Math.Clamp(newZoom, Options.MinZoom, Options.MaxZoom);
        if (clamped == _zoom)
            return;

        var worldUnderCursor = ScreenToWorld(screenPoint);
        _zoom = clamped;
        Position = new PointD(
            worldUnderCursor.X - screenPoint.X / _zoom,
            worldUnderCursor.Y - screenPoint.Y / _zoom);
    }

    /// <summary>
    /// Zooms by a number of wheel steps, positive to zoom in, using <see cref="ViewportOptions.ZoomStepFactor"/>.
    /// Fractional steps support trackpads that report smooth scrolling deltas.
    /// </summary>
    public void ZoomBy(PointD screenPoint, double steps) =>
        ZoomAt(screenPoint, _zoom * Math.Pow(Options.ZoomStepFactor, steps));
}
