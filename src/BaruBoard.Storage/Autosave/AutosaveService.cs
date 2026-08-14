namespace BaruBoard.Storage.Autosave;

/// <summary>
/// Debounces autosave requests: changes only reach disk once the user pauses.
/// Every pending request belongs to the session that scheduled it and is dropped
/// by <see cref="Cancel"/> when the document is replaced.
/// </summary>
public sealed class AutosaveService : IDisposable
{
    private readonly TimeSpan _debounce;
    private readonly Func<CancellationToken, Task> _save;
    private CancellationTokenSource? _pending;

    public AutosaveService(TimeSpan debounce, Func<CancellationToken, Task> save)
    {
        _debounce = debounce;
        _save = save;
    }

    public void Notify()
    {
        Cancel();
        var pending = new CancellationTokenSource();
        _pending = pending;
        _ = RunAsync(pending.Token);
    }

    public void Cancel()
    {
        var pending = _pending;
        _pending = null;
        if (pending is null)
            return;

        pending.Cancel();
        pending.Dispose();
    }

    public void Dispose() => Cancel();

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_debounce, cancellationToken).ConfigureAwait(false);
            await _save(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Autosave is best effort: a failed recovery copy must not disturb editing.
        }
    }
}
