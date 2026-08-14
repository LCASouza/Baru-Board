using BaruBoard.Core.Boards;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Boards;

public class ImageElementTests
{
    private const double Tolerance = 1e-9;

    private const string AssetId = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static ImageElement CreateImage() => new(new RectD(100, 50, 200, 100), AssetId);

    [Fact]
    public void UsesProportionalCornerResizeOnly()
    {
        var image = CreateImage();

        Assert.Equal(ElementResizeMode.ProportionalCorners, image.ResizeMode);
        Assert.True(image.CanResize);
        Assert.Equal(SelectionGeometry.CornerHandles, SelectionGeometry.GetHandles(image.ResizeMode));
    }

    [Fact]
    public void RequiresItsAsset()
    {
        Assert.Equal([AssetId], CreateImage().RequiredAssetIds);
    }

    [Fact]
    public void MoveTo_ShiftsBoundsKeepingSize()
    {
        var image = CreateImage();

        image.MoveTo(new PointD(-40, -30));

        Assert.Equal(new RectD(-40, -30, 200, 100), image.Bounds);
    }

    [Fact]
    public void Contains_UsesTheBoundingBox()
    {
        var image = CreateImage();

        Assert.True(image.Contains(new PointD(150, 80)));
        Assert.False(image.Contains(new PointD(400, 80)));
    }

    [Fact]
    public void CreateCopy_SharesTheAssetButNotTheIdentity()
    {
        var image = CreateImage();
        image.ZIndex = 4;

        var copy = Assert.IsType<ImageElement>(image.CreateCopy());
        copy.MoveTo(new PointD(999, 999));

        Assert.Equal(AssetId, copy.AssetId);
        Assert.NotEqual(image.Id, copy.Id);
        Assert.Equal(4, copy.ZIndex);
        Assert.Equal(100, image.Bounds.X, Tolerance);
    }

    [Fact]
    public void Constructor_RejectsAnEmptyAssetId()
    {
        Assert.Throws<ArgumentException>(() => new ImageElement(new RectD(0, 0, 10, 10), " "));
    }
}
