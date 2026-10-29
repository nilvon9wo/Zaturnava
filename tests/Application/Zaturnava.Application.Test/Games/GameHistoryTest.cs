using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Games;

public sealed class GameHistoryTest
{
    private static readonly TinyRules Rules = new(5);
    private static readonly ActionDescriptor Pass = new PassAction(new SeatId(0)).Describe();

    [Fact]
    public void Of_WhenAHostedGameGoesOn_TellsItsSeatsAndActionsButNoOutcome()
    {
        // Arrange
        GameRecord record = new(
            new GameId("g1"),
            TinyRules.RulesetId,
            [new HumanSeat("HASH"), new OpenSeat()],
            [Pass],
            3
        );

        // Act
        GameHistory history = GameHistory.Of(GameSnapshot.Of(record, Rules));

        // Assert
        Assert.Equal(
            (1, "g1", TinyRules.RulesetId, Pass, null),
            (history.Format, history.GameId, history.RulesetId, Assert.Single(history.Actions), history.Outcome)
        );
        Assert.Equal(
            [new HistorySeat(0, "Seat 0", 0, GameHistory.Person, 0), new HistorySeat(1, "Seat 1", 1, "open", 0)],
            history.Seats
        );
    }

    [Fact]
    public void Of_WhenTheGameHasEnded_TellsHowWithTheScores()
    {
        // Arrange
        GameState state = Rules.CreateInitialState() with
        {
            Scores = ImmutableDictionary<SeatId, int>.Empty.Add(new SeatId(0), 7).Add(new SeatId(1), 3),
            Outcome = new GameOutcome([new SeatId(0)], "agreement", "every player agreed"),
        };

        // Act
        GameHistory history = GameHistory.Of(Rules, state, ["sage", "argus"], [Pass]);

        // Assert
        Assert.Equal(
            (null, 7, "argus", 0, "agreement", "every player agreed"),
            (
                history.GameId,
                history.Seats[0].Score,
                history.Seats[1].Player,
                Assert.Single(history.Outcome!.Winners),
                history.Outcome.Ending,
                history.Outcome.Reason
            )
        );
    }
}
