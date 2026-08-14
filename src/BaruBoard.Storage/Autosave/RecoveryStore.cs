using System.Text.Json;
using BaruBoard.Core.Boards;
using BaruBoard.Storage.Files;
using BaruBoard.Storage.Serialization;

namespace BaruBoard.Storage.Autosave;

public sealed record RecoveryEntry(Guid DocumentId, string? OriginalPath, DateTimeOffset SavedAt, string Directory);

/// <summary>
/// Holds autosaved copies outside the user's own files, one directory per
/// document. Asset bytes are content addressed and therefore written once, so a
/// repeated autosave only rewrites the small board json.
/// </summary>
public sealed class RecoveryStore
{
    private const string BoardFileName = "board.json";
    private const string MetadataFileName = "metadata.json";
    private const string AssetsFolder = "assets";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _root;

    public RecoveryStore(string root) => _root = root;

    /// <summary>
    /// Assets are flushed before the board that references them, so an interrupted
    /// autosave can leave orphan assets but never a board pointing at missing bytes.
    /// </summary>
    public async Task SaveAsync(
        string? originalPath, BoardSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var directory = DirectoryFor(snapshot.Board.Id);
        var assetsDirectory = Path.Combine(directory, AssetsFolder);
        Directory.CreateDirectory(assetsDirectory);

        foreach (var asset in snapshot.Assets)
        {
            var assetPath = Path.Combine(assetsDirectory, AssetFileName(asset.Id, asset.MediaType));
            if (File.Exists(assetPath))
                continue;

            await AtomicFile.WriteAllBytesAsync(assetPath, asset.Data, cancellationToken).ConfigureAwait(false);
        }

        var json = await Task.Run(() => BoardSerializer.Serialize(snapshot.Board), cancellationToken).ConfigureAwait(false);
        await AtomicFile.WriteAllTextAsync(Path.Combine(directory, BoardFileName), json, cancellationToken)
            .ConfigureAwait(false);

        var metadata = new RecoveryMetadataDto
        {
            DocumentId = snapshot.Board.Id,
            OriginalPath = originalPath,
            SavedAt = DateTimeOffset.Now,
        };

        await AtomicFile.WriteAllTextAsync(
            Path.Combine(directory, MetadataFileName),
            JsonSerializer.Serialize(metadata, Options),
            cancellationToken).ConfigureAwait(false);
    }

    // A damaged recovery entry must never keep the application from starting.
    public IReadOnlyList<RecoveryEntry> List()
    {
        if (!Directory.Exists(_root))
            return [];

        string[] directories;
        try
        {
            directories = Directory.GetDirectories(_root);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        var entries = new List<RecoveryEntry>();
        foreach (var directory in directories)
        {
            RecoveryMetadataDto? metadata;
            try
            {
                var metadataPath = Path.Combine(directory, MetadataFileName);
                if (!File.Exists(metadataPath) || !File.Exists(Path.Combine(directory, BoardFileName)))
                    continue;

                metadata = JsonSerializer.Deserialize<RecoveryMetadataDto>(File.ReadAllText(metadataPath), Options);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                continue;
            }

            if (metadata is null || metadata.DocumentId == Guid.Empty)
                continue;

            entries.Add(new RecoveryEntry(metadata.DocumentId, metadata.OriginalPath, metadata.SavedAt, directory));
        }

        return [.. entries.OrderByDescending(entry => entry.SavedAt)];
    }

    public async Task<BoardLoadResult> LoadAsync(RecoveryEntry entry, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(entry.Directory, BoardFileName), cancellationToken)
            .ConfigureAwait(false);
        var board = BoardSerializer.ReadSnapshot(json);

        var assets = new List<BoardAsset>();
        foreach (var assetDto in board.Assets ?? [])
        {
            BoardAssetReader.ValidateManifestEntry(assetDto);
            var assetPath = Path.Combine(entry.Directory, AssetsFolder, AssetFileName(assetDto.Id, assetDto.MediaType));
            if (!File.Exists(assetPath))
                throw new BoardFormatException($"The recovery copy is missing asset '{assetDto.Id}'.");

            var bytes = await File.ReadAllBytesAsync(assetPath, cancellationToken).ConfigureAwait(false);
            assets.Add(BoardAssetReader.Create(assetDto.Id, assetDto.MediaType, bytes));
        }

        return BoardSerializer.ToBoard(board, assets);
    }

    public void Remove(Guid documentId)
    {
        try
        {
            var directory = DirectoryFor(documentId);
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    public void Clear()
    {
        foreach (var entry in List())
            Remove(entry.DocumentId);
    }

    private string DirectoryFor(Guid documentId) => Path.Combine(_root, documentId.ToString("N"));

    private static string AssetFileName(string assetId, string mediaType) =>
        assetId + AssetMediaTypes.GetExtension(mediaType);

    private sealed class RecoveryMetadataDto
    {
        public Guid DocumentId { get; init; }

        public string? OriginalPath { get; init; }

        public DateTimeOffset SavedAt { get; init; }
    }
}
