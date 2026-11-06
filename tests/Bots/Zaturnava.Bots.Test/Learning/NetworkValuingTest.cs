using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test.Learning;

public sealed class NetworkValuingTest
{
    private static readonly SeatId Ally = new(2);
    private static readonly LineRules Rules = new([]);
    private static readonly DecisionRequest Request = LineRules.Request([], (0, LineRules.Pawn, LineRules.Seat));
    private static readonly SageWeights Unweighed = new(0, 0, 0, 0, 0, 0, 0);

    [Theory]
    [InlineData("won", 10 + (0.1 * 15 / 30))]
    [InlineData("drawn", 0.1 * 15 / 30)]
    [InlineData("lost", -10 + (0.1 * 15 / 30))]
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
    public void ValueOf_WhenTheGameGoesOn_IsHowTheNetworkExpectsItToEndLeavingItsMarginAside()
    {
        // Arrange
        ValueNetwork network = Untrained();
        GameState state = Scored(20, 5);
        double outcome = network.Evaluate(new PositionEncoder(Rules).Encode(state, LineRules.Seat)).Outcome;

        // Act
        double value = Valuing(network).ValueOf(Request, state);

        // Assert
        Assert.Equal(outcome, value, 1e-9);
    }

    [Fact]
    public void ValueOf_WhenAnEnemyCouldTakeAPieceNext_IsWorseByWhatItWouldWin()
    {
        // Arrange - seat 0's Pawn beside an enemy Horse, no one guarding it
        ValueNetwork network = Untrained();
        DecisionRequest request = LineRules.Request(
            [],
            (0, LineRules.Pawn, LineRules.Seat),
            (1, LineRules.Horse, LineRules.Enemy)
        );
        double outcome = network.Evaluate(new PositionEncoder(Rules).Encode(request.State, LineRules.Seat)).Outcome;

        // Act
        double value = Valuing(network).ValueOf(request, request.State);

        // Assert - the Pawn, worth 1, is what the Horse would win
        Assert.Equal(outcome - NetworkValuing.Wariness, value, 1e-9);
    }

    [Fact]
    public void ValueOf_WhenTheGameGoesOn_CountsSagesAppraisalALittle()
    {
        // Arrange - seat 0's lone Pawn, worth 1, weighed by its worth alone
        ValueNetwork network = Untrained();
        SageWeights material = Unweighed with { Material = 1 };
        NetworkValuing steadying = new(new PositionEncoder(Rules), network, material);

        // Act
        double steadied = steadying.ValueOf(Request, Request.State);

        // Assert
        Assert.Equal(Valuing(network).ValueOf(Request, Request.State) + NetworkValuing.Steadiness, steadied, 1e-9);
    }

    [Theory]
    [InlineData("pawn", 0, 3, 1)]
    [InlineData("raja", 0, 3, 0)]
    [InlineData("pawn", 0, 5, 0)]
    [InlineData("pawn", 1, 3, 0)]
    [InlineData("pawn", 0, 0, 0)]
    public void ValueOf_WhenItsPieceNotALeaderStandsOnAnEnemyThrone_IsWorseForKeepingItsLeadersOff(
        string kind,
        int owner,
        int cell,
        int blocked
    )
    {
        // Arrange - seat 1's throne on cell 3, seat 0's on cell 0; the one piece on cell 6 or 7 out of reach of
        // any other
        ValueNetwork network = Untrained();
        DecisionRequest request = LineRules.Request(
            [],
            (cell, new PieceKindId(kind), new SeatId(owner)),
            (owner == 0 ? 7 : 6, LineRules.Pawn, LineRules.Seat)
        );
        GameState state = request.State with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty
                .Add(LineRules.Seat, LineRules.Throne)
                .Add(LineRules.Enemy, new CellId(3)),
        };
        double outcome = network.Evaluate(new PositionEncoder(Rules).Encode(state, LineRules.Seat)).Outcome;

        // Act
        double value = Valuing(network).ValueOf(request, state);

        // Assert
        Assert.Equal(outcome - (NetworkValuing.Blocking * blocked), value, 1e-9);
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

    /// <summary>The network's judgement, steadied by nothing: Sage weighing nothing.</summary>
    private static NetworkValuing Valuing(ValueNetwork network) => new(new PositionEncoder(Rules), network, Unweighed);
}
