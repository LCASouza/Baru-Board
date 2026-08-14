using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Boards;

public sealed class ImageElement : BoardElement
{
    public ImageElement(RectD bounds, string assetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetId);
        Bounds = bounds;
        AssetId = assetId;
    }

    public string AssetId { get; }

    public override ElementResizeMode ResizeMode => ElementResizeMode.ProportionalCorners;

    public override IEnumerable<string> RequiredAssetIds => [AssetId];

    public override BoardElement CreateCopy() => new ImageElement(Bounds, AssetId)
    {
        ZIndex = ZIndex,
    };
}
