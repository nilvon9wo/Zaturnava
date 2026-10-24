using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Actions;

public sealed class RollActionTest
{
    [Fact]
    public void Describe_WhenCalled_NamesOnlyTheActor()
    {
        // Arrange
        RollAction roll = new(new SeatId(3));

        // Act
        ActionDescriptor descriptor = roll.Describe();

        // Assert
        Assert.Equal(("roll", "3", 1), (descriptor.Kind, descriptor.Arguments["actor"], descriptor.Arguments.Count));
    }
}
