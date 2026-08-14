using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;
using BaruBoard.Storage.Autosave;
using BaruBoard.Storage.Serialization;

namespace BaruBoard.Tests.Storage;

public class RecoveryStoreTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("whiteboard-recovery").FullName;

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

    private static BoardSnapshot CreateSnapshot(string name, out BoardDocument document)
    {
        document = new BoardDocument { Name = name };
        document.AddElement(new RectangleElement(new RectD(1, 2, 30, 40)));
        return BoardSerializer.CreateSnapshot(document, new Viewport
        {
            Position = new PointD(5, 6),
            Zoom = 1.5,
            ViewportSize = new SizeD(800, 600),
        });
    }

    [Fact]
    public async Task SaveAsync_CreatesARecoverableEntry()
    {
        var store = new RecoveryStore(_directory);
        var snapshot = CreateSnapshot("Quadro", out var document);

        await store.SaveAsync("/home/user/board.baru", snapshot);

        var entry = Assert.Single(store.List());
        Assert.Equal(document.Id, entry.DocumentId);
        Assert.Equal("/home/user/board.baru", entry.OriginalPath);

        var loaded = await store.LoadAsync(entry);
        Assert.Equal("Quadro", loaded.Document.Name);
        Assert.Single(loaded.Document.Elements);
        Assert.Equal(1.5, loaded.Zoom, 1e-9);
    }

    [Fact]
    public async Task SaveAsync_WithoutOriginalPath_KeepsNullPath()
    {
        var store = new RecoveryStore(_directory);

        await store.SaveAsync(null, CreateSnapshot("Sem título", out _));

        Assert.Null(Assert.Single(store.List()).OriginalPath);
    }

    [Fact]
    public async Task SaveAsync_Twice_KeepsASingleEntryPerDocument()
    {
        var store = new RecoveryStore(_directory);
        var snapshot = CreateSnapshot("Quadro", out _);

        await store.SaveAsync(null, snapshot);
        await store.SaveAsync(null, snapshot);

        Assert.Single(store.List());
    }

    [Fact]
    public async Task Remove_DeletesTheWholeRecoveryDirectory()
    {
        var store = new RecoveryStore(_directory);
        var snapshot = CreateSnapshot("Quadro", out var document);
        await store.SaveAsync(null, snapshot);

        store.Remove(document.Id);

        Assert.Empty(store.List());
        Assert.Empty(Directory.GetDirectories(_directory));
    }

    [Fact]
    public void List_OnMissingDirectory_ReturnsEmpty()
    {
        var store = new RecoveryStore(Path.Combine(_directory, "nunca-criado"));

        Assert.Empty(store.List());
    }

    [Fact]
    public async Task List_SkipsCorruptEntries()
    {
        var store = new RecoveryStore(_directory);
        await store.SaveAsync(null, CreateSnapshot("Bom", out _));
        var broken = Directory.CreateDirectory(Path.Combine(_directory, "quebrado"));
        await File.WriteAllTextAsync(Path.Combine(broken.FullName, "board.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(broken.FullName, "metadata.json"), "{ inválido");

        Assert.Single(store.List());
    }

    [Fact]
    public async Task List_SkipsEntriesWhoseBoardIsMissing()
    {
        var store = new RecoveryStore(_directory);
        var snapshot = CreateSnapshot("Quadro", out var document);
        await store.SaveAsync(null, snapshot);

        File.Delete(Path.Combine(_directory, document.Id.ToString("N"), "board.json"));

        Assert.Empty(store.List());
    }

    [Fact]
    public async Task SaveAsync_WritesAssetsOnceAndKeepsTheBoardConsistent()
    {
        var store = new RecoveryStore(_directory);
        var document = new BoardDocument { Name = "Com imagem" };
        var asset = document.AddAsset(BoardAsset.Create("conteudo-de-imagem"u8, AssetMediaTypes.Png));
        document.AddElement(new ImageElement(new RectD(0, 0, 100, 80), asset.Id));
        var snapshot = BoardSerializer.CreateSnapshot(document, new Viewport
        {
            Position = new PointD(0, 0),
            Zoom = 1,
            ViewportSize = new SizeD(800, 600),
        });

        await store.SaveAsync(null, snapshot);
        var assetPath = Path.Combine(_directory, document.Id.ToString("N"), "assets", $"{asset.Id}.png");
        Assert.True(File.Exists(assetPath));

        // A second autosave must not rewrite immutable asset bytes.
        var marker = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(assetPath, marker);
        await store.SaveAsync(null, snapshot);
        Assert.Equal(marker, File.GetLastWriteTimeUtc(assetPath));

        var loaded = await store.LoadAsync(Assert.Single(store.List()));
        var image = Assert.IsType<ImageElement>(Assert.Single(loaded.Document.Elements));
        Assert.Equal(asset.Id, image.AssetId);
        Assert.True(loaded.Document.ContainsAsset(asset.Id));
    }

    [Fact]
    public async Task LoadAsync_WithMissingAssetFile_Fails()
    {
        var store = new RecoveryStore(_directory);
        var document = new BoardDocument();
        var asset = document.AddAsset(BoardAsset.Create("imagem"u8, AssetMediaTypes.Png));
        document.AddElement(new ImageElement(new RectD(0, 0, 10, 10), asset.Id));
        var snapshot = BoardSerializer.CreateSnapshot(document, new Viewport
        {
            Position = new PointD(0, 0),
            Zoom = 1,
            ViewportSize = new SizeD(800, 600),
        });

        await store.SaveAsync(null, snapshot);
        File.Delete(Path.Combine(_directory, document.Id.ToString("N"), "assets", $"{asset.Id}.png"));

        await Assert.ThrowsAsync<BoardFormatException>(() => store.LoadAsync(Assert.Single(store.List())));
    }

    [Fact]
    public async Task Clear_RemovesEveryEntry()
    {
        var store = new RecoveryStore(_directory);
        await store.SaveAsync(null, CreateSnapshot("Um", out _));
        await store.SaveAsync(null, CreateSnapshot("Dois", out _));
        Assert.Equal(2, store.List().Count);

        store.Clear();

        Assert.Empty(store.List());
    }
}
