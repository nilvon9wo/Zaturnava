namespace Net.NowhereAtAll.Zaturnava.Contracts.Test;

public sealed class GameRequestRefusedExceptionTest
{
    [Fact]
    public void Constructor_WhenGivenARefusal_KeepsItAndTheMessage()
    {
        // Arrange - nothing beyond the arguments

        // Act
        GameRequestRefusedException exception = new(Refusal.Conflict, "Seat 1 is taken.");

        // Assert
        Assert.Equal((Refusal.Conflict, "Seat 1 is taken."), (exception.Refusal, exception.Message));
    }
}
