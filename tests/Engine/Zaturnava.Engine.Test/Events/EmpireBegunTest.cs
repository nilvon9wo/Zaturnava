using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Events;

public sealed class EmpireBegunTest
{
    private static readonly SeatId First = new(0);
    private static readonly SeatId Second = new(2);

    [Fact]
    public void Equals_WhenTheSameSeatsRuleInTheSameOrder_IsTrue()
    {
        // Arrange
        EmpireBegun empire = new([First, Second]);

        // Act
        bool isEqual = empire.Equals(new EmpireBegun([First, Second]));

        // Assert
        Assert.Equal((true, empire.GetHashCode()), (isEqual, new EmpireBegun([First, Second]).GetHashCode()));
    }

    [Fact]
    public void Equals_WhenComparedWithNothing_IsFalse()
    {
        // Arrange
        EmpireBegun empire = new([First, Second]);

        // Act
        bool isEqual = empire.Equals(null);

        // Assert
        Assert.False(isEqual);
    }

    [Fact]
    public void Equals_WhenOtherSeatsRule_IsFalse()
    {
        // Arrange
        EmpireBegun empire = new([First, Second]);

        // Act
        bool isEqual = empire.Equals(new EmpireBegun([Second, First]));

        // Assert
        Assert.False(isEqual);
    }
}
