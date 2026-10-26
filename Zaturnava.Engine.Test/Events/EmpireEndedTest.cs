using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Events;

public sealed class EmpireEndedTest
{
    private static readonly SeatId First = new(0);
    private static readonly SeatId Second = new(2);

    [Fact]
    public void Equals_WhenTheSameSeatsRuleInTheSameOrder_IsTrue()
    {
        // Arrange
        EmpireEnded empire = new([First, Second]);

        // Act
        bool isEqual = empire.Equals(new EmpireEnded([First, Second]));

        // Assert
        Assert.Equal((true, empire.GetHashCode()), (isEqual, new EmpireEnded([First, Second]).GetHashCode()));
    }

    [Fact]
    public void Equals_WhenComparedWithNothing_IsFalse()
    {
        // Arrange
        EmpireEnded empire = new([First, Second]);

        // Act
        bool isEqual = empire.Equals(null);

        // Assert
        Assert.False(isEqual);
    }

    [Fact]
    public void Equals_WhenOtherSeatsRule_IsFalse()
    {
        // Arrange
        EmpireEnded empire = new([First, Second]);

        // Act
        bool isEqual = empire.Equals(new EmpireEnded([Second, First]));

        // Assert
        Assert.False(isEqual);
    }
}
