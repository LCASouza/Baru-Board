using System.IO.Compression;
using System.Text;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;
using BaruBoard.Storage.Files;
using BaruBoard.Storage.Serialization;

namespace BaruBoard.Tests.Storage;

public class BoardContainerTests : IDisposable
{
    private const double Tolerance = 1e-9;

    private readonly string _directory = Directory.CreateTempSubdirectory("whiteboard-container").FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private static readonly byte[] ImageBytes = Encoding.UTF8.GetBytes("bytes-de-imagem");

    private static Viewport CreateViewport() => new()
    {
        Position = new PointD(12, -34),
        Zoom = 1.25,
        ViewportSize = new SizeD(1200, 800),
    };

    private static BoardSnapshot CreateSnapshotWithImage(out BoardDocument document, out BoardAsset asset)
    {
        document = new BoardDocument { Name = "Com imagem" };
        asset = document.AddAsset(BoardAsset.Create(ImageBytes, AssetMediaTypes.Png));
        document.AddElement(new RectangleElement(new RectD(0, 0, 50, 50)));
        document.AddElement(new ImageElement(new RectD(10, 20, 300, 150), asset.Id));
        return BoardSerializer.CreateSnapshot(document, CreateViewport());
    }

    private static BoardLoadResult ReadContainer(byte[] container)
    {
        using var stream = new MemoryStream(container);
        return BoardContainer.Read(stream);
    }

    private static byte[] WriteContainer(BoardSnapshot snapshot)
    {
        using var stream = new MemoryStream();
        BoardContainer.Write(stream, snapshot);
        return stream.ToArray();
    }

    private string PathFor(string name) => Path.Combine(_directory, name);

    [Fact]
    public void ContainerRoundTrip_KeepsElementsAndAssets()
    {
        var snapshot = CreateSnapshotWithImage(out var document, out var asset);

        var loaded = ReadContainer(WriteContainer(snapshot));

        Assert.Equal(document.Id, loaded.Document.Id);
        Assert.Equal(2, loaded.Document.Elements.Count);
        var image = Assert.IsType<ImageElement>(loaded.Document.Elements[1]);
        Assert.Equal(asset.Id, image.AssetId);
        Assert.Equal(new RectD(10, 20, 300, 150), image.Bounds);
        Assert.True(loaded.Document.TryGetAsset(asset.Id, out var loadedAsset));
        Assert.Equal(ImageBytes, loadedAsset.Data.ToArray());
        Assert.Equal(1.25, loaded.Zoom, Tolerance);
    }

    [Fact]
    public void WrittenContainer_HasTheExpectedLayout()
    {
        var snapshot = CreateSnapshotWithImage(out _, out var asset);

        using var stream = new MemoryStream(WriteContainer(snapshot));
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.NotNull(archive.GetEntry("board.json"));
        Assert.NotNull(archive.GetEntry($"assets/{asset.Id}.png"));
    }

    [Fact]
    public void OrphanAssets_AreNotWritten()
    {
        var document = new BoardDocument();
        document.AddAsset(BoardAsset.Create(ImageBytes, AssetMediaTypes.Png));
        var snapshot = BoardSerializer.CreateSnapshot(document, CreateViewport());

        using var stream = new MemoryStream(WriteContainer(snapshot));
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Single(archive.Entries);
        Assert.Equal("board.json", archive.Entries[0].FullName);
    }

    [Fact]
    public void MissingBoardEntry_IsRejected()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("outra-coisa.json");
        }

        Assert.Throws<BoardFormatException>(() => ReadContainer(stream.ToArray()));
    }

    [Fact]
    public void MissingAssetEntry_IsRejected()
    {
        var snapshot = CreateSnapshotWithImage(out _, out var asset);
        var container = WriteContainer(snapshot);

        using var stripped = new MemoryStream();
        using (var source = new MemoryStream(container))
        using (var sourceArchive = new ZipArchive(source, ZipArchiveMode.Read))
        using (var target = new ZipArchive(stripped, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in sourceArchive.Entries.Where(e => !e.FullName.StartsWith("assets/", StringComparison.Ordinal)))
            {
                var copy = target.CreateEntry(entry.FullName);
                using var input = entry.Open();
                using var output = copy.Open();
                input.CopyTo(output);
            }
        }

        var exception = Assert.Throws<BoardFormatException>(() => ReadContainer(stripped.ToArray()));
        Assert.Contains(asset.Id, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssetWithTamperedContent_IsRejected()
    {
        var snapshot = CreateSnapshotWithImage(out _, out var asset);
        var container = WriteContainer(snapshot);

        using var tampered = new MemoryStream();
        using (var source = new MemoryStream(container))
        using (var sourceArchive = new ZipArchive(source, ZipArchiveMode.Read))
        using (var target = new ZipArchive(tampered, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in sourceArchive.Entries)
            {
                var copy = target.CreateEntry(entry.FullName);
                using var output = copy.Open();
                if (entry.FullName.StartsWith("assets/", StringComparison.Ordinal))
                {
                    output.Write("conteudo-adulterado"u8);
                    continue;
                }

                using var input = entry.Open();
                input.CopyTo(output);
            }
        }

        var exception = Assert.Throws<BoardFormatException>(() => ReadContainer(tampered.ToArray()));
        Assert.Contains("hash", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ElementReferencingAnUndeclaredAsset_IsRejected()
    {
        var document = new BoardDocument();
        document.AddElement(new ImageElement(
            new RectD(0, 0, 10, 10),
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"));
        var snapshot = BoardSerializer.CreateSnapshot(document, CreateViewport());

        Assert.Throws<BoardFormatException>(() => ReadContainer(WriteContainer(snapshot)));
    }

    [Fact]
    public void SuspiciousEntryNames_AreRejected()
    {
        var snapshot = CreateSnapshotWithImage(out _, out _);
        var container = WriteContainer(snapshot);

        using var hostile = new MemoryStream();
        using (var source = new MemoryStream(container))
        using (var sourceArchive = new ZipArchive(source, ZipArchiveMode.Read))
        using (var target = new ZipArchive(hostile, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in sourceArchive.Entries)
            {
                var copy = target.CreateEntry(entry.FullName);
                using var input = entry.Open();
                using var output = copy.Open();
                input.CopyTo(output);
            }

            target.CreateEntry("../escapou.txt");
        }

        Assert.Throws<BoardFormatException>(() => ReadContainer(hostile.ToArray()));
    }

    [Fact]
    public void NotAContainer_IsDetectedBySignature()
    {
        Assert.False(BoardContainer.HasSignature("{ \"f\""u8));
        Assert.True(BoardContainer.HasSignature([0x50, 0x4B, 0x03, 0x04]));
    }

    [Fact]
    public async Task LegacyVersion1File_StillOpens()
    {
        var service = new BoardFileService();
        var path = PathFor("legado.baru");
        await File.WriteAllTextAsync(path, LegacyV1Json);

        var loaded = await service.OpenAsync(path);

        Assert.Equal("Quadro legado", loaded.Document.Name);
        Assert.Single(loaded.Document.Elements);
        Assert.Empty(loaded.Document.Assets);
    }

    [Fact]
    public async Task LegacyFile_SavedAgain_BecomesAContainerWithTheSameContent()
    {
        var service = new BoardFileService();
        var path = PathFor("legado.baru");
        await File.WriteAllTextAsync(path, LegacyV1Json);

        var loaded = await service.OpenAsync(path);
        var snapshot = BoardSerializer.CreateSnapshot(loaded.Document, new Viewport
        {
            Position = loaded.ViewportPosition,
            Zoom = loaded.Zoom,
            ViewportSize = new SizeD(800, 600),
        });
        await service.SaveAsync(path, snapshot);

        var header = new byte[4];
        await using (var stream = File.OpenRead(path))
        {
            await stream.ReadExactlyAsync(header);
        }

        Assert.True(BoardContainer.HasSignature(header));

        var reopened = await service.OpenAsync(path);
        Assert.Equal(loaded.Document.Id, reopened.Document.Id);
        Assert.Equal("Quadro legado", reopened.Document.Name);
        Assert.Equal(loaded.Document.Elements.Count, reopened.Document.Elements.Count);
        Assert.Equal(loaded.Document.Elements[0].Bounds, reopened.Document.Elements[0].Bounds);
        Assert.Equal(loaded.Zoom, reopened.Zoom, Tolerance);
    }

    [Fact]
    public async Task ContainerRoundTripThroughTheFileService_PreservesImages()
    {
        var service = new BoardFileService();
        var path = PathFor("com-imagem.baru");
        var snapshot = CreateSnapshotWithImage(out _, out var asset);

        await service.SaveAsync(path, snapshot);
        var loaded = await service.OpenAsync(path);

        Assert.True(loaded.Document.ContainsAsset(asset.Id));
        Assert.Contains(loaded.Document.Elements, element => element is ImageElement);
        Assert.Equal([path], Directory.GetFiles(_directory));
    }

    private const string LegacyV1Json = """
    {
      "formatVersion": 1,
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "Quadro legado",
      "viewport": { "x": 154.5, "y": -220.25, "zoom": 0.85 },
      "elements": [
        {
          "type": "rectangle",
          "id": "22222222-2222-2222-2222-222222222222",
          "zIndex": 0,
          "bounds": { "x": 10, "y": 20, "width": 100, "height": 50 },
          "fill": "#FFFFFFFF",
          "stroke": "#37474FFF",
          "strokeThickness": 2
        }
      ]
    }
    """;
}
