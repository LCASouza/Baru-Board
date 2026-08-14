using BaruBoard.Core.Boards;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Core.Tools;

public sealed class TextTool : ITool
{
    private readonly BoardDocument _document;
    private readonly Viewport _viewport;

    public TextTool(BoardDocument document, Viewport viewport)
    {
        _document = document;
        _viewport = viewport;
    }

    public event Action<TextElement>? EditRequested;

    public EditorCursor Cursor => EditorCursor.Text;

    public bool PointerPressed(PointD screenPoint)
    {
        var element = CreationDefaults.CreateText(_viewport.ScreenToWorld(screenPoint));
        _document.AddElement(element);
        EditRequested?.Invoke(element);
        return true;
    }

    public bool PointerMoved(PointD screenPoint) => false;

    public bool PointerReleased(PointD screenPoint) => false;
}
