namespace BaruBoard.Storage.Files;

// Content is written to a sibling temporary file and only then swapped in, so a
// crash or a full disk can never leave a truncated board behind.
internal static class AtomicFile
{
    public static async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new IOException($"Invalid path '{path}'.");
        var temp = Path.Combine(directory, $"{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(temp, contents, cancellationToken).ConfigureAwait(false);
            File.Move(temp, fullPath, overwrite: true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    public static async Task WriteAsync(
        string path, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new IOException($"Invalid path '{path}'.");
        var temp = Path.Combine(directory, $"{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await write(stream, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temp, fullPath, overwrite: true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    public static async Task WriteAllBytesAsync(
        string path, ReadOnlyMemory<byte> contents, CancellationToken cancellationToken) =>
        await WriteAsync(
            path,
            async (stream, token) => await stream.WriteAsync(contents, token).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

    public static void WriteAllText(string path, string contents)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new IOException($"Invalid path '{path}'.");
        var temp = Path.Combine(directory, $"{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temp, contents);
            File.Move(temp, fullPath, overwrite: true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    public static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
