using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Actions;

public sealed class IllegalActionExceptionTest
{
    [Fact]
    public void Message_WhenCreated_NamesTheKindOfAction()
    {
        // Arrange
        PassAction pass = new(new SeatId(2));

        // Act
        IllegalActionException exception = new(pass);

        // Assert
        Assert.Equal("That pass is not allowed now.", exception.Message);
    }
}
