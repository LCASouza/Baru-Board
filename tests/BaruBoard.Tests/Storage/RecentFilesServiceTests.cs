using BaruBoard.Storage.RecentFiles;

namespace BaruBoard.Tests.Storage;

public class RecentFilesServiceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("whiteboard-recent").FullName;

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

    private string CreateBoardFile(string name)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, "{}");
        return path;
    }

    private RecentFilesService CreateService(int maxEntries = RecentFilesService.MaxEntries) =>
        new(Path.Combine(_directory, "recent.json"), maxEntries);

    [Fact]
    public void Load_WithoutIndex_ReturnsEmpty()
    {
        Assert.Empty(CreateService().Load());
    }

    [Fact]
    public void Add_PutsTheMostRecentFirst()
    {
        var service = CreateService();
        var first = CreateBoardFile("a.baru");
        var second = CreateBoardFile("b.baru");

        service.Add(first);
        service.Add(second);

        Assert.Equal([second, first], service.Load());
    }

    [Fact]
    public void Add_ExistingEntry_MovesItToTheFrontWithoutDuplicating()
    {
        var service = CreateService();
        var first = CreateBoardFile("a.baru");
        var second = CreateBoardFile("b.baru");
        service.Add(first);
        service.Add(second);

        service.Add(first);

        Assert.Equal([first, second], service.Load());
    }

    [Fact]
    public void Add_RespectsTheEntryLimit()
    {
        var service = CreateService(maxEntries: 3);
        var paths = Enumerable.Range(0, 5).Select(i => CreateBoardFile($"board{i}.baru")).ToList();

        foreach (var path in paths)
            service.Add(path);

        Assert.Equal([paths[4], paths[3], paths[2]], service.Load());
    }

    [Fact]
    public void Load_PrunesFilesThatNoLongerExist()
    {
        var service = CreateService();
        var kept = CreateBoardFile("kept.baru");
        var removed = CreateBoardFile("removed.baru");
        service.Add(kept);
        service.Add(removed);

        File.Delete(removed);

        Assert.Equal([kept], service.Load());
    }

    [Fact]
    public void Load_WithCorruptIndex_ReturnsEmptyInsteadOfThrowing()
    {
        File.WriteAllText(Path.Combine(_directory, "recent.json"), "isto não é json");

        Assert.Empty(CreateService().Load());
    }

    [Fact]
    public void Add_NormalizesEquivalentPaths()
    {
        var service = CreateService();
        var path = CreateBoardFile("board.baru");
        var equivalent = Path.Combine(_directory, ".", "board.baru");

        service.Add(path);
        service.Add(equivalent);

        Assert.Single(service.Load());
    }
}
