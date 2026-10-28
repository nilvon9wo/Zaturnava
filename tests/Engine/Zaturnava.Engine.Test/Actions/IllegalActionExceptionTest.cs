using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Actions;

public sealed class IllegalActionExceptionTest
{
    [Fact]
    public void Message_WhenCreated_NamesTheAction()
    {
        // Arrange
        PassAction pass = new(new SeatId(2));

        // Act
        IllegalActionException exception = new(pass);

        // Assert
        Assert.Equal("PassAction { Actor = SeatId { Value = 2 } } is not legal now.", exception.Message);
    }
}
