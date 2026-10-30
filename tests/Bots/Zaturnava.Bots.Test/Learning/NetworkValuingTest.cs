using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test.Learning;

public sealed class NetworkValuingTest
{
    private static readonly SeatId Ally = new(2);
    private static readonly LineRules Rules = new([]);
    private static readonly DecisionRequest Request = LineRules.Request([], (0, LineRules.Pawn, LineRules.Seat));

    [Theory]
    [InlineData("won", 1 + (0.1 * 15 / 30))]
    [InlineData("drawn", 0.1 * 15 / 30)]
    [InlineData("lost", -1 + (0.1 * 15 / 30))]
    public void ValueOf_WhenTheGameIsOver_JudgesItByHowItEndedAndALittleByTheLead(string result, double expected)
    {
        // Arrange - seat 0's ally 20 points to the enemy's 5
        ImmutableList<SeatId> winners = result switch
        {
            "won" => [Ally],
            "drawn" => [],
            _ => [LineRules.Enemy],
        };
        GameState state = Scored(20, 5) with { Outcome = new GameOutcome(winners, "test", "Over.") };

        // Act
        double value = Valuing(Untrained()).ValueOf(Request, state);

        // Assert
        Assert.Equal(expected, value, 1e-9);
    }

    [Fact]
    public void ValueOf_WhenTheGameGoesOn_IsWhatTheNetworkJudges()
    {
        // Arrange
        ValueNetwork network = Untrained();
        GameState state = Scored(20, 5);
        (double outcome, double margin) =
            network.Evaluate(new PositionEncoder(Rules).Encode(state, LineRules.Seat));

        // Act
        double value = Valuing(network).ValueOf(Request, state);

        // Assert
        Assert.Equal(outcome + (NetworkValuing.MarginWeight * margin), value, 1e-9);
    }

    [Theory]
    [InlineData(20, 5, true)]
    [InlineData(5, 20, false)]
    public void IsBetterEnded_WhenPlayingOnIsJudgedEven_EndsItOnlyIfItsTeamLeads(int ours, int theirs, bool ends)
    {
        // Arrange
        GameState state = Scored(ours, theirs);
        ValueNetwork network = ValueNetwork.From(Taught.Network(Rules, (state, LineRules.Seat, 0)));

        // Act
        bool ended = Valuing(network).IsBetterEnded(Request with { State = state });

        // Assert
        Assert.Equal(ends, ended);
    }

    private static GameState Scored(int ours, int theirs) =>
        Request.State with
        {
            Scores = ImmutableDictionary<SeatId, int>.Empty.Add(Ally, ours).Add(LineRules.Enemy, theirs),
        };

    private static ValueNetwork Untrained() =>
        ValueNetwork.Untrained(new PositionEncoder(Rules).Size, 4, new Random(7));

    private static NetworkValuing Valuing(ValueNetwork network) => new(new PositionEncoder(Rules), network);
}
