using System.IO.Compression;
using System.Text;
using BaruBoard.Core.Boards;

namespace BaruBoard.Storage.Serialization;

/// <summary>
/// The .baru container: a zip holding board.json plus the referenced assets.
/// Reading treats the archive as untrusted input — entries are looked up by exact
/// name, sizes are capped and nothing is ever written to the filesystem.
/// </summary>
public static class BoardContainer
{
    public const string BoardEntryName = "board.json";

    public const string AssetsPrefix = "assets/";

    private static readonly byte[] Signature = [0x50, 0x4B, 0x03, 0x04];

    public static bool HasSignature(ReadOnlySpan<byte> header) =>
        header.Length >= Signature.Length && header[..Signature.Length].SequenceEqual(Signature);

    public static BoardLoadResult Read(Stream stream)
    {
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException exception)
        {
            throw new BoardFormatException("The file is not a readable board container.", exception);
        }

        using (archive)
        {
            var boardEntry = FindBoardEntry(archive);
            var board = BoardSerializer.ReadSnapshot(
                Encoding.UTF8.GetString(ReadEntry(boardEntry, BoardFormatLimits.MaxBoardJsonBytes)));

            return BoardSerializer.ToBoard(board, ReadAssets(archive, board));
        }
    }

    public static void Write(Stream stream, BoardSnapshot snapshot)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        var boardEntry = archive.CreateEntry(BoardEntryName, CompressionLevel.Optimal);
        using (var boardStream = boardEntry.Open())
        using (var writer = new StreamWriter(boardStream, new UTF8Encoding(false)))
        {
            writer.Write(BoardSerializer.Serialize(snapshot.Board));
        }

        foreach (var asset in snapshot.Assets)
        {
            // Image payloads are already compressed; deflating them again only burns CPU.
            var entry = archive.CreateEntry(GetAssetEntryName(asset.Id, asset.MediaType), CompressionLevel.NoCompression);
            using var entryStream = entry.Open();
            entryStream.Write(asset.Data.Span);
        }
    }

    public static string GetAssetEntryName(string assetId, string mediaType) =>
        $"{AssetsPrefix}{assetId}{AssetMediaTypes.GetExtension(mediaType)}";

    private static ZipArchiveEntry FindBoardEntry(ZipArchive archive)
    {
        if (archive.Entries.Count > BoardFormatLimits.MaxContainerEntries)
            throw new BoardFormatException("The container declares too many entries.");

        long declaredTotal = 0;
        ZipArchiveEntry? boardEntry = null;

        foreach (var entry in archive.Entries)
        {
            if (IsSuspiciousName(entry.FullName))
                throw new BoardFormatException($"The container has a suspicious entry name '{entry.FullName}'.");

            declaredTotal += entry.Length;
            if (declaredTotal > BoardFormatLimits.MaxTotalUncompressedBytes)
                throw new BoardFormatException("The container declares more content than allowed.");

            if (!string.Equals(entry.FullName, BoardEntryName, StringComparison.Ordinal))
                continue;

            if (boardEntry is not null)
                throw new BoardFormatException($"The container has more than one '{BoardEntryName}'.");

            boardEntry = entry;
        }

        return boardEntry ?? throw new BoardFormatException($"The container has no '{BoardEntryName}'.");
    }

    private static List<BoardAsset> ReadAssets(ZipArchive archive, BoardFileDto board)
    {
        var manifest = board.Assets ?? [];
        if (manifest.Count > BoardFormatLimits.MaxAssets)
            throw new BoardFormatException("The board declares too many assets.");

        var assets = new List<BoardAsset>(manifest.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var assetDto in manifest)
        {
            BoardAssetReader.ValidateManifestEntry(assetDto);
            if (!seen.Add(assetDto.Id))
                throw new BoardFormatException($"Duplicated asset '{assetDto.Id}' in the manifest.");

            var entryName = GetAssetEntryName(assetDto.Id, assetDto.MediaType);
            var entry = archive.GetEntry(entryName)
                ?? throw new BoardFormatException($"The container is missing asset entry '{entryName}'.");

            assets.Add(BoardAssetReader.Create(
                assetDto.Id,
                assetDto.MediaType,
                ReadEntry(entry, BoardFormatLimits.MaxAssetBytes)));
        }

        return assets;
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry, int maxBytes)
    {
        if (entry.Length > maxBytes)
            throw new BoardFormatException($"Entry '{entry.FullName}' is larger than allowed.");

        using var entryStream = entry.Open();
        using var buffer = new MemoryStream();

        // The declared length is not trusted: copying is capped independently.
        var chunk = new byte[81920];
        int read;
        while ((read = entryStream.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + read > maxBytes)
                throw new BoardFormatException($"Entry '{entry.FullName}' is larger than allowed.");

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static bool IsSuspiciousName(string name) =>
        name.Length == 0 ||
        name.Contains("..", StringComparison.Ordinal) ||
        name.StartsWith('/') ||
        name.StartsWith('\\') ||
        name.Contains('\\', StringComparison.Ordinal) ||
        Path.IsPathRooted(name);
}
