using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Commands;

public class AddElementsCommandTests
{
    private static RectangleElement CreateRectangle(double x) => new(new RectD(x, 0, 20, 20));

    [Fact]
    public void Undo_RemovesEveryElementAndRedoRestoresTheOrder()
    {
        var document = new BoardDocument();
        var existing = CreateRectangle(0);
        document.AddElement(existing);

        var added = new[] { CreateRectangle(100), CreateRectangle(200), CreateRectangle(300) };
        var additions = added.Select((element, index) => new AddedElement(element, 1 + index)).ToList();
        var command = new AddElementsCommand(document, additions);

        command.Execute();
        Assert.Equal([existing, added[0], added[1], added[2]], document.Elements);

        command.Undo();
        Assert.Equal([existing], document.Elements);

        command.Execute();
        Assert.Equal([existing, added[0], added[1], added[2]], document.Elements);
    }

    [Fact]
    public void SingleHistoryEntry_CoversTheWholeBatch()
    {
        var document = new BoardDocument();
        var history = new CommandHistory();
        var added = new[] { CreateRectangle(0), CreateRectangle(50) };

        history.Execute(new AddElementsCommand(
            document,
            added.Select((element, index) => new AddedElement(element, index))));

        Assert.Equal(1, history.Count);
        Assert.Equal(2, document.Elements.Count);

        history.Undo();
        Assert.Empty(document.Elements);
    }

    [Fact]
    public void EmptyBatch_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new AddElementsCommand(new BoardDocument(), []));
    }
}
