using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;
using BaruBoard.Storage.Files;
using BaruBoard.Storage.Serialization;

namespace BaruBoard.Tests.Storage;

public class BoardFileServiceTests : IDisposable
{
    private const double Tolerance = 1e-9;

    private readonly string _directory = Directory.CreateTempSubdirectory("whiteboard-tests").FullName;

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

    private static Viewport CreateViewport(double zoom = 1.0) => new()
    {
        Position = new PointD(10, -20),
        Zoom = zoom,
        ViewportSize = new SizeD(1200, 800),
    };

    private static BoardSnapshot CreateSnapshot(string name, params BoardElement[] elements)
    {
        var document = new BoardDocument { Name = name };
        foreach (var element in elements)
            document.AddElement(element);

        return BoardSerializer.CreateSnapshot(document, CreateViewport());
    }

    private string PathFor(string fileName) => Path.Combine(_directory, fileName);

    [Fact]
    public async Task SaveAndOpen_RoundTripsTheBoard()
    {
        var service = new BoardFileService();
        var path = PathFor("board.baru");
        var snapshot = CreateSnapshot("Meu quadro", new RectangleElement(new RectD(1, 2, 30, 40)));

        await service.SaveAsync(path, snapshot);
        var loaded = await service.OpenAsync(path);

        Assert.True(File.Exists(path));
        Assert.Equal("Meu quadro", loaded.Document.Name);
        var element = Assert.Single(loaded.Document.Elements);
        Assert.Equal(new RectD(1, 2, 30, 40), element.Bounds);
    }

    [Fact]
    public async Task Save_OverwritesExistingFileWithNewContent()
    {
        var service = new BoardFileService();
        var path = PathFor("board.baru");

        await service.SaveAsync(path, CreateSnapshot("Primeiro", new RectangleElement(new RectD(0, 0, 10, 10))));
        await service.SaveAsync(path, CreateSnapshot("Segundo"));

        var loaded = await service.OpenAsync(path);
        Assert.Equal("Segundo", loaded.Document.Name);
        Assert.Empty(loaded.Document.Elements);
    }

    [Fact]
    public async Task Save_LeavesNoTemporaryFilesBehind()
    {
        var service = new BoardFileService();
        var path = PathFor("board.baru");

        await service.SaveAsync(path, CreateSnapshot("Quadro"));

        Assert.Equal([path], Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task Save_ToMissingDirectory_FailsWithoutLeavingFiles()
    {
        var service = new BoardFileService();
        var path = Path.Combine(_directory, "missing", "board.baru");

        await Assert.ThrowsAnyAsync<IOException>(() => service.SaveAsync(path, CreateSnapshot("Quadro")));

        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task FailedSave_KeepsThePreviousFileIntact()
    {
        var service = new BoardFileService();
        var path = PathFor("board.baru");
        await service.SaveAsync(path, CreateSnapshot("Original"));
        var originalContent = await File.ReadAllTextAsync(path);

        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.SaveAsync(path, CreateSnapshot("Nunca gravado"), cancelled.Token));

        Assert.Equal(originalContent, await File.ReadAllTextAsync(path));
        Assert.Equal([path], Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task Open_CorruptFile_ThrowsAndKeepsTheFile()
    {
        var service = new BoardFileService();
        var path = PathFor("corrupt.baru");
        await File.WriteAllTextAsync(path, "{ isto não é um quadro }");

        await Assert.ThrowsAsync<BoardFormatException>(() => service.OpenAsync(path));

        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task Open_MissingFile_ThrowsFileNotFound()
    {
        var service = new BoardFileService();

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.OpenAsync(PathFor("ausente.baru")));
    }

    [Fact]
    public async Task ConcurrentSaves_ProduceAConsistentFile()
    {
        var service = new BoardFileService();
        var path = PathFor("board.baru");

        var saves = Enumerable.Range(0, 8)
            .Select(i => service.SaveAsync(path, CreateSnapshot($"Quadro {i}")))
            .ToArray();
        await Task.WhenAll(saves);

        var loaded = await service.OpenAsync(path);
        Assert.StartsWith("Quadro ", loaded.Document.Name, StringComparison.Ordinal);
        Assert.Equal([path], Directory.GetFiles(_directory));
    }
}
