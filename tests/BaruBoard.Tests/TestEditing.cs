using BaruBoard.Core.Editing;

namespace BaruBoard.Tests;

internal static class TestEditing
{
    public static SnapContext NoSnap() => new(new GridSettings(), new EditorInteractionState());

    public static SnapContext WithGrid(double step, EditorInteractionState? interaction = null) =>
        new(
            new GridSettings { LogicalStep = step, SnapEnabled = true },
            interaction ?? new EditorInteractionState());
}
