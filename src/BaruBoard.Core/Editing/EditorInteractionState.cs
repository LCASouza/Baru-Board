namespace BaruBoard.Core.Editing;

/// <summary>
/// Momentary input state, kept apart from settings: these flags describe what is
/// held down right now and must be reset whenever the canvas can miss a key up.
/// </summary>
public sealed class EditorInteractionState
{
    public bool IsSnapSuppressed { get; set; }

    public bool IsMultiSelectModifierDown { get; set; }

    public void Reset()
    {
        IsSnapSuppressed = false;
        IsMultiSelectModifierDown = false;
    }
}
