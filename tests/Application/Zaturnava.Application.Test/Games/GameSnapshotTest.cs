using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Games;

public sealed class GameSnapshotTest
{
    private static readonly GameRecord Passed =
        new(GameId.New(), TinyRules.RulesetId, [], [new PassAction(new SeatId(0)).Describe()], 1);

    [Theory]
    [InlineData("illegal")]
    [InlineData("malformed")]
    [InlineData("unknown")]
    [InlineData("out of range")]
    [InlineData("invalid")]
    public void TryOf_WhenTheRulesNoLongerReplayTheGame_IsNone(string why)
    {
        // Arrange
        Exception failure = why switch
        {
            "illegal" => new IllegalActionException(new PassAction(new SeatId(0))),
            "malformed" => new MalformedActionException("There is no 'pass' action."),
            "unknown" => new KeyNotFoundException(),
            "out of range" => new ArgumentOutOfRangeException(nameof(why)),
            _ => new InvalidOperationException(),
        };
        TinyRules rules = new(6, failure: failure);

        // Act
        GameSnapshot? snapshot = GameSnapshot.TryOf(Passed, rules);

        // Assert
        Assert.Null(snapshot);
    }

    [Fact]
    public void TryOf_WhenSomethingElseGoesWrong_LetsItBeKnown()
    {
        // Arrange
        TinyRules rules = new(6, failure: new NotSupportedException());

        // Act
        GameSnapshot? Replaying() => GameSnapshot.TryOf(Passed, rules);

        // Assert
        _ = Assert.Throws<NotSupportedException>(Replaying);
    }

    [Fact]
    public void TryOf_WhenTheRulesReplayTheGame_IsWhereItStands()
    {
        // Arrange
        TinyRules rules = new(6);

        // Act
        GameSnapshot? snapshot = GameSnapshot.TryOf(Passed, rules);

        // Assert
        Assert.Equal(new SeatId(1), snapshot?.State.DecidingSeat);
    }
}
