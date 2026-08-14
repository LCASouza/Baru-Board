using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Tools;

/// <summary>
/// A pointer-driven editor tool. Positions arrive in screen space (DIPs); tools
/// convert to world space through the viewport as needed. Each handler returns
/// whether the visual state changed and the canvas needs a repaint.
/// </summary>
public interface ITool
{
    EditorCursor Cursor { get; }

    bool PointerPressed(PointD screenPoint);

    bool PointerMoved(PointD screenPoint);

    bool PointerReleased(PointD screenPoint);
}
