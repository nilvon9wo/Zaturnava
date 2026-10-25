using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Events;

public sealed class RollOffTiedTest
{
    private static readonly SeatId First = new(0);
    private static readonly SeatId Second = new(2);

    [Fact]
    public void Equals_WhenTheSameSeatsTiedInTheSameOrder_IsTrue()
    {
        // Arrange
        RollOffTied tied = new([First, Second]);

        // Act
        bool isEqual = tied.Equals(new RollOffTied([First, Second]));

        // Assert
        Assert.Equal((true, tied.GetHashCode()), (isEqual, new RollOffTied([First, Second]).GetHashCode()));
    }

    [Fact]
    public void Equals_WhenComparedWithNothing_IsFalse()
    {
        // Arrange
        RollOffTied tied = new([First, Second]);

        // Act
        bool isEqual = tied.Equals(null);

        // Assert
        Assert.False(isEqual);
    }

    [Fact]
    public void Equals_WhenOtherSeatsTied_IsFalse()
    {
        // Arrange
        RollOffTied tied = new([First, Second]);

        // Act
        bool isEqual = tied.Equals(new RollOffTied([Second, First]));

        // Assert
        Assert.False(isEqual);
    }
}
