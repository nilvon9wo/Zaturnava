using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Rules;

public sealed class GameOutcomeTest
{
    private const string Ending = "one-team-left";
    private const string Reason = "only one team has pieces left";
    private static readonly SeatId First = new(0);
    private static readonly SeatId Second = new(2);

    [Fact]
    public void Equals_WhenTheSameSeatsWonTheSameWay_IsTrue()
    {
        // Arrange
        GameOutcome outcome = new([First, Second], Ending, Reason);

        // Act
        bool isEqual = outcome.Equals(new GameOutcome([First, Second], Ending, Reason));

        // Assert
        Assert.Equal(
            (true, outcome.GetHashCode()),
            (isEqual, new GameOutcome([First, Second], Ending, Reason).GetHashCode())
        );
    }

    [Fact]
    public void Equals_WhenComparedWithNothing_IsFalse()
    {
        // Arrange
        GameOutcome outcome = new([First, Second], Ending, Reason);

        // Act
        bool isEqual = outcome.Equals(null);

        // Assert
        Assert.False(isEqual);
    }

    [Theory]
    [MemberData(nameof(Others), DisableDiscoveryEnumeration = true)]
    public void Equals_WhenAnythingDiffers_IsFalse(GameOutcome other)
    {
        // Arrange
        GameOutcome outcome = new([First, Second], Ending, Reason);

        // Act
        bool isEqual = outcome.Equals(other);

        // Assert
        Assert.False(isEqual);
    }

    public static TheoryData<GameOutcome> Others =>
    [
        new GameOutcome([First, Second], Ending, "points decide"),
        new GameOutcome([First, Second], "agreement", Reason),
        new GameOutcome([First], Ending, Reason),
    ];

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void IsDraw_WhenRead_IsWhetherNoOneWon(bool hasWinners, bool expected)
    {
        // Arrange
        GameOutcome outcome = new(hasWinners ? [First] : [], Ending, Reason);

        // Act
        bool isDraw = outcome.IsDraw;

        // Assert
        Assert.Equal(expected, isDraw);
    }
}
