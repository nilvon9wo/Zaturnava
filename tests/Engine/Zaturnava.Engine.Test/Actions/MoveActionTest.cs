using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Actions;

public sealed class MoveActionTest
{
    [Fact]
    public void Describe_WhenCalled_NamesTheActorAndBothCells()
    {
        // Arrange
        MoveAction move = new(new SeatId(1), new CellId(8), new CellId(16));

        // Act
        ActionDescriptor descriptor = move.Describe();

        // Assert
        Assert.Equal(
            ("move", "1", "8", "16"),
            (descriptor.Kind, descriptor.Arguments["actor"], descriptor.Arguments["from"], descriptor.Arguments["to"])
        );
    }
}
