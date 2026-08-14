using System.Text;
using BaruBoard.Storage.Serialization;

namespace BaruBoard.Storage.Files;

public sealed class BoardFileService
{
    public const string FileExtension = ".baru";

    // Explicit saves and autosaves share this lock so two writes never interleave.
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task SaveAsync(string path, BoardSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await AtomicFile.WriteAsync(
                path,
                (stream, token) => Task.Run(() => BoardContainer.Write(stream, snapshot), token),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Opens a board, accepting both the container written since format 2 and the
    /// plain json of format 1.
    /// </summary>
    public async Task<BoardLoadResult> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        var header = new byte[4];
        var read = await stream.ReadAsync(header, cancellationToken).ConfigureAwait(false);
        stream.Position = 0;

        if (read == header.Length && BoardContainer.HasSignature(header))
            return await Task.Run(() => BoardContainer.Read(stream), cancellationToken).ConfigureAwait(false);

        if (stream.Length > BoardFormatLimits.MaxBoardJsonBytes)
            throw new BoardFormatException("The board file is larger than allowed.");

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return await Task.Run(
            () => BoardSerializer.ToBoard(BoardSerializer.ReadSnapshot(json), []),
            cancellationToken).ConfigureAwait(false);
    }
}
