using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Events;

public sealed class DiceForfeitedTest
{
    private static readonly SeatId Roller = new(1);

    [Fact]
    public void Equals_WhenTheValuesAreTheSameInTheSameOrder_IsTrue()
    {
        // Arrange
        DiceForfeited forfeited = new(Roller, [5, 3]);

        // Act
        bool isEqual = forfeited.Equals(new DiceForfeited(Roller, [5, 3]));

        // Assert
        Assert.Equal((true, forfeited.GetHashCode()), (isEqual, new DiceForfeited(Roller, [5, 3]).GetHashCode()));
    }

    [Fact]
    public void Equals_WhenComparedWithNothing_IsFalse()
    {
        // Arrange
        DiceForfeited forfeited = new(Roller, [5, 3]);

        // Act
        bool isEqual = forfeited.Equals(null);

        // Assert
        Assert.False(isEqual);
    }

    [Theory]
    [MemberData(nameof(Others), DisableDiscoveryEnumeration = true)]
    public void Equals_WhenAnythingDiffers_IsFalse(DiceForfeited other)
    {
        // Arrange
        DiceForfeited forfeited = new(Roller, [5, 3]);

        // Act
        bool isEqual = forfeited.Equals(other);

        // Assert
        Assert.False(isEqual);
    }

    public static TheoryData<DiceForfeited> Others =>
    [
        new DiceForfeited(new SeatId(2), [5, 3]),
        new DiceForfeited(Roller, [3, 5]),
    ];
}
