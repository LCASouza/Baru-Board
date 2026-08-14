using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Boards;

public class BoardAssetTests
{
    private static BoardAsset CreateAsset(string content = "conteudo") =>
        BoardAsset.Create(System.Text.Encoding.UTF8.GetBytes(content), AssetMediaTypes.Png);

    [Fact]
    public void Id_IsTheLowercaseHexHashOfTheContent()
    {
        var asset = CreateAsset();

        Assert.Equal(64, asset.Id.Length);
        Assert.Equal(asset.Id, BoardAsset.ComputeId(asset.Data.Span));
        Assert.Equal(asset.Id.ToLowerInvariant(), asset.Id);
        Assert.True(BoardAsset.IsValidId(asset.Id));
    }

    [Fact]
    public void SameContent_ProducesTheSameId()
    {
        Assert.Equal(CreateAsset().Id, CreateAsset().Id);
        Assert.NotEqual(CreateAsset("a").Id, CreateAsset("b").Id);
    }

    [Fact]
    public void MutatingTheSourceBuffer_DoesNotAffectTheAsset()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var asset = BoardAsset.Create(bytes, AssetMediaTypes.Png);

        bytes[0] = 99;

        Assert.Equal(1, asset.Data.Span[0]);
        Assert.Equal(asset.Id, BoardAsset.ComputeId(asset.Data.Span));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABCDEF")]
    [InlineData("zz45f0e2c1b3a4d5e6f70819202a2b3c4d5e6f708192a2b3c4d5e6f708192a2b")]
    public void IsValidId_RejectsAnythingButLowercaseSha256Hex(string? id)
    {
        Assert.False(BoardAsset.IsValidId(id));
    }

    [Fact]
    public void AddAsset_DeduplicatesByContent()
    {
        var document = new BoardDocument();

        var first = document.AddAsset(CreateAsset());
        var second = document.AddAsset(CreateAsset());

        Assert.Same(first, second);
        Assert.Single(document.Assets);
    }

    [Fact]
    public void GetReferencedAssets_IgnoresOrphans()
    {
        var document = new BoardDocument();
        var used = document.AddAsset(CreateAsset("usada"));
        document.AddAsset(CreateAsset("orfa"));
        document.AddElement(new ImageElement(new RectD(0, 0, 10, 10), used.Id));

        var referenced = document.GetReferencedAssets();

        Assert.Equal(2, document.Assets.Count);
        Assert.Same(used, Assert.Single(referenced));
    }

    [Fact]
    public void GetReferencedAssets_ListsEachAssetOnce()
    {
        var document = new BoardDocument();
        var asset = document.AddAsset(CreateAsset());
        document.AddElement(new ImageElement(new RectD(0, 0, 10, 10), asset.Id));
        document.AddElement(new ImageElement(new RectD(50, 50, 10, 10), asset.Id));

        Assert.Single(document.GetReferencedAssets());
    }

    [Fact]
    public void DeletedImage_KeepsItsAssetInMemoryForUndo()
    {
        var document = new BoardDocument();
        var asset = document.AddAsset(CreateAsset());
        var element = new ImageElement(new RectD(0, 0, 10, 10), asset.Id);
        document.AddElement(element);

        document.RemoveElement(element);

        Assert.True(document.ContainsAsset(asset.Id));
        Assert.Empty(document.GetReferencedAssets());
    }
}
