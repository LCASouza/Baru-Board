using System.Text.Json;
using BaruBoard.Storage.Files;

namespace BaruBoard.Storage.RecentFiles;

public sealed class RecentFilesService
{
    public const int MaxEntries = 10;

    private readonly string _indexPath;
    private readonly int _maxEntries;

    public RecentFilesService(string indexPath, int maxEntries = MaxEntries)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEntries);
        _indexPath = indexPath;
        _maxEntries = maxEntries;
    }

    /// <summary>
    /// Most recent first, without duplicates and without entries whose file no
    /// longer exists. A missing or damaged index simply yields an empty list.
    /// </summary>
    public IReadOnlyList<string> Load()
    {
        var stored = ReadIndex();
        var result = new List<string>(stored.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in stored)
        {
            if (!TryNormalize(entry, out var path) || !seen.Add(path) || !File.Exists(path))
                continue;

            result.Add(path);
            if (result.Count == _maxEntries)
                break;
        }

        return result;
    }

    public void Add(string path)
    {
        if (!TryNormalize(path, out var normalized))
            return;

        var entries = new List<string> { normalized };
        entries.AddRange(Load().Where(existing => !string.Equals(existing, normalized, StringComparison.Ordinal)));

        if (entries.Count > _maxEntries)
            entries.RemoveRange(_maxEntries, entries.Count - _maxEntries);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_indexPath))!);
            AtomicFile.WriteAllText(_indexPath, JsonSerializer.Serialize(entries));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private IReadOnlyList<string> ReadIndex()
    {
        try
        {
            if (!File.Exists(_indexPath))
                return [];

            return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_indexPath)) ?? [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    private static bool TryNormalize(string? path, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            normalized = Path.GetFullPath(path);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
