using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Boards;

public abstract class BoardElement
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public int ZIndex { get; set; }

    // World-space axis-aligned bounds. Each element owns the invariant between
    // Bounds and its actual geometry, so outside code transforms elements only
    // through MoveTo/ResizeTo.
    public RectD Bounds { get; protected set; }

    public virtual ElementResizeMode ResizeMode => ElementResizeMode.Free;

    public bool CanResize => ResizeMode != ElementResizeMode.None;

    /// <summary>
    /// Assets this element cannot be rendered or copied without.
    /// </summary>
    public virtual IEnumerable<string> RequiredAssetIds => [];

    public virtual void MoveTo(PointD position) => Bounds = new RectD(position, Bounds.Size);

    public virtual void ResizeTo(RectD bounds) => Bounds = bounds;

    public virtual bool Contains(PointD worldPoint, double worldTolerance = 0.0) =>
        Bounds.Contains(worldPoint);

    /// <summary>
    /// Deep copy carrying a new identity. Implementations must not share mutable
    /// state with the original.
    /// </summary>
    public abstract BoardElement CreateCopy();
}
