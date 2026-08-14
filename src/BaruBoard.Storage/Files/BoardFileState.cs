using BaruBoard.Core.Commands;

namespace BaruBoard.Storage.Files;

/// <summary>
/// Which file the open board belongs to and which history position was last
/// written to disk. A null checkpoint means no saved state matches the document,
/// which is exactly the situation of a recovered board.
/// </summary>
public sealed class BoardFileState
{
    public string? FilePath { get; private set; }

    public long? SavedStateId { get; private set; }

    public bool IsDirty(CommandHistory history) =>
        SavedStateId is not { } saved || saved != history.CurrentStateId;

    public void MarkNewDocument()
    {
        FilePath = null;
        SavedStateId = 0;
    }

    public void MarkOpened(string path)
    {
        FilePath = path;
        SavedStateId = 0;
    }

    public void MarkRecovered(string? originalPath)
    {
        FilePath = originalPath;
        SavedStateId = null;
    }

    public void MarkSaved(string path, long stateId)
    {
        FilePath = path;
        SavedStateId = stateId;
    }
}
