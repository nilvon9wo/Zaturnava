using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Events;

public sealed class DiceRolledTest
{
    private static readonly SeatId Roller = new(1);

    [Fact]
    public void Equals_WhenTheValuesAreTheSameInTheSameOrder_IsTrue()
    {
        // Arrange
        DiceRolled rolled = new(Roller, [5, 3]);

        // Act
        bool isEqual = rolled.Equals(new DiceRolled(Roller, [5, 3]));

        // Assert
        Assert.Equal((true, rolled.GetHashCode()), (isEqual, new DiceRolled(Roller, [5, 3]).GetHashCode()));
    }

    [Fact]
    public void Equals_WhenComparedWithNothing_IsFalse()
    {
        // Arrange
        DiceRolled rolled = new(Roller, [5, 3]);

        // Act
        bool isEqual = rolled.Equals(null);

        // Assert
        Assert.False(isEqual);
    }

    [Theory]
    [MemberData(nameof(Others), DisableDiscoveryEnumeration = true)]
    public void Equals_WhenAnythingDiffers_IsFalse(DiceRolled other)
    {
        // Arrange
        DiceRolled rolled = new(Roller, [5, 3]);

        // Act
        bool isEqual = rolled.Equals(other);

        // Assert
        Assert.False(isEqual);
    }

    public static TheoryData<DiceRolled> Others =>
    [
        new DiceRolled(new SeatId(2), [5, 3]),
        new DiceRolled(Roller, [3, 5]),
    ];
}
