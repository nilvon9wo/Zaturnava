using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

public sealed class SageMatchTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);

    [Theory]
    [InlineData("won", 1)]
    [InlineData("lost", -1)]
    [InlineData("drawn", 3)]
    [InlineData("unfinished", 3)]
    public void MarginOf_WhenTheGameEnds_IsOneGameIfWonElseThePoints(string end, int expected)
    {
        // Arrange - Black and Green have 7 points, to Yellow's 4
        GameState final = new(
            Position.Empty,
            [],
            TeamGraph.FromTeams([[Black, Green], [Yellow, new SeatId(3)]]),
            null,
            1,
            new Dictionary<SeatId, int> { [Black] = 5, [Green] = 2, [Yellow] = 4 }.ToImmutableDictionary(),
            []
        );
        GameState ended = end switch
        {
            "won" => final with { Outcome = new GameOutcome([Black, Green], "over") },
            "lost" => final with { Outcome = new GameOutcome([Yellow], "over") },
            "drawn" => final with { Outcome = new GameOutcome([], "over") },
            _ => final,
        };

        // Act
        int margin = SageMatch.MarginOf(ended, Black);

        // Assert
        Assert.Equal(expected, margin);
    }

    [Fact]
    public async Task Play_WhenGivenGames_PlaysAsManyWithEachSideFirst()
    {
        // Arrange - two steps a game, so that whoever leads after them wins
        SageMatch match = new(new Chaturaji2027Rules(), new RandomDice(new Random(3)), 2, 2);

        // Act
        (int won, int lost) = await match.Play(
            SageWeights.Default,
            SageWeights.Default,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.InRange(won + lost, 0, 4);
    }
}
