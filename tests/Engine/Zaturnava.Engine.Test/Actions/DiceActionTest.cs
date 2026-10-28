using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Actions;

public sealed class DiceActionTest
{
    private static readonly SeatId Roller = new(1);

    [Fact]
    public void Describe_WhenCalled_NamesTheActorAndTheValuesInOrder()
    {
        // Arrange
        DiceAction dice = new(Roller, [5, 3]);

        // Act
        ActionDescriptor descriptor = dice.Describe();

        // Assert
        Assert.Equal(
            ("dice", "1", "5,3"),
            (descriptor.Kind, descriptor.Arguments["actor"], descriptor.Arguments["values"])
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("5,")]
    [InlineData("five")]
    public void Parse_WhenTheValuesAreNotIntegers_ThrowsMalformedActionException(string values)
    {
        // Arrange
        ActionDescriptor descriptor = new(
            DiceAction.Kind,
            ImmutableSortedDictionary<string, string>.Empty.Add("actor", "1").Add("values", values)
        );

        // Act
        MalformedActionException exception = Assert.Throws<MalformedActionException>(
            () => DiceAction.Parse(descriptor)
        );

        // Assert
        Assert.Equal("The 'dice' action needs integer 'values', such as 3,5.", exception.Message);
    }

    [Fact]
    public void Equals_WhenTheValuesAreTheSameInTheSameOrder_IsTrue()
    {
        // Arrange
        DiceAction dice = new(Roller, [5, 3]);

        // Act
        bool isEqual = dice.Equals(new DiceAction(Roller, [5, 3]));

        // Assert
        Assert.Equal((true, dice.GetHashCode()), (isEqual, new DiceAction(Roller, [5, 3]).GetHashCode()));
    }

    [Fact]
    public void Equals_WhenComparedWithNothing_IsFalse()
    {
        // Arrange
        DiceAction dice = new(Roller, [5, 3]);

        // Act
        bool isEqual = dice.Equals(null);

        // Assert
        Assert.False(isEqual);
    }

    [Theory]
    [MemberData(nameof(Others), DisableDiscoveryEnumeration = true)]
    public void Equals_WhenAnythingDiffers_IsFalse(DiceAction other)
    {
        // Arrange
        DiceAction dice = new(Roller, [5, 3]);

        // Act
        bool isEqual = dice.Equals(other);

        // Assert
        Assert.False(isEqual);
    }

    public static TheoryData<DiceAction> Others =>
    [
        new DiceAction(new SeatId(2), [5, 3]),
        new DiceAction(Roller, [3, 5]),
        new DiceAction(Roller, [5]),
    ];
}
