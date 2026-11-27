using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class EndgameTest
{
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(8, 1, true)]
    [InlineData(8, 2, false)]
    [InlineData(64, 8, true)]
    [InlineData(64, 9, false)]
    public void IsEndgame_WhenSoManyPiecesAreLeftOnABoardOfSoManyCells_IsSoOnlyForFew(
        int cells,
        int pieces,
        bool expected
    )
    {
        // Arrange - pieces one a cell, as many as said
        DecisionRequest request = OnBoardOf(
            cells,
            LineRules.Request(
                [],
                [.. Enumerable.Range(0, pieces).Select(cell => (cell, LineRules.Pawn, LineRules.Enemy))]
            )
        );

        // Act
        bool isEndgame = Endgame.IsEndgame(request.Rules, request.State);

        // Assert
        Assert.Equal(expected, isEndgame);
    }

    [Theory]
    [InlineData(4, -0.5)]
    [InlineData(7, -0.25)]
    public void Pressure_WhenTheEnemysLeaderIsFree_CountsEachCellItMayFleeTo(int raja, double expected)
    {
        // Arrange - the enemy's Raja alone, in the middle of the line or at its end
        DecisionRequest request = LineRules.Request([], (raja, LineRules.Raja, LineRules.Enemy));

        // Act
        double pressure = Pressure(request);

        // Assert
        Assert.Equal(expected, pressure);
    }

    [Fact]
    public void Pressure_WhenTheTeamAttacksTheLeaderAndOneOfItsEscapes_CountsBothForIt()
    {
        // Arrange - the Boat attacks up the line as far as the Raja, so its cell and the one below it
        DecisionRequest request = LineRules.Request(
            [],
            (0, LineRules.Boat, LineRules.Seat),
            (4, LineRules.Raja, LineRules.Enemy)
        );

        // Act
        double pressure = Pressure(request);

        // Assert - attacked, 0.5; the cell above it free, -0.25
        Assert.Equal(0.25, pressure);
    }

    [Fact]
    public async Task Decide_WhenTheEnemysLeaderIsAloneInACorner_TakesItsLastEscapeRatherThanWanderOff()
    {
        // Arrange - two Pawns against a lone Raja at the end of the line, on a board as large as Chaturaji 2027's:
        // one may step away, or up beside the cell the Raja could flee to, out of its reach
        DecisionRequest request = OnBoardOf(
            64,
            LineRules.Request(
                [LineRules.Move(3, 2), LineRules.Move(3, 5)],
                (3, LineRules.Pawn, LineRules.Seat),
                (0, LineRules.Pawn, LineRules.Seat),
                (7, LineRules.Raja, LineRules.Enemy)
            )
        );

        // Act
        GameAction action = await new SageBot(new Random(1), SageWeights.Default).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(LineRules.Move(3, 5), action);
    }

    [Fact]
    public async Task Decide_WhenTacticianIsInAnEndgame_TakesTheLeadersLastEscapeToo()
    {
        // Arrange - as Sage's, a budget too small to see past the first move but for the endgame's
        DecisionRequest request = OnBoardOf(
            64,
            LineRules.Request(
                [LineRules.Move(3, 2), LineRules.Move(3, 5)],
                (3, LineRules.Pawn, LineRules.Seat),
                (0, LineRules.Pawn, LineRules.Seat),
                (7, LineRules.Raja, LineRules.Enemy)
            )
        );

        // Act
        GameAction action = await new TacticianBot(new Random(1), 1, 1, SageWeights.Default)
            .Decide(request, this._cancellation);

        // Assert
        Assert.Equal(LineRules.Move(3, 5), action);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, -1.75)]
    public void Pressure_WhenAnEnemyHeavyIsFree_CountsItsEscapesOnlyIfHeaviesAreHunted(bool isHunted, double expected)
    {
        // Arrange - the enemy's Boat alone at the line's end, a heavy piece: it may flee to any of the 7 cells below
        DecisionRequest request = LineRules.Request([], (7, LineRules.Boat, LineRules.Enemy));
        request = request with
        {
            Rules = new LineRules([.. request.LegalActions]) { Heavies = [LineRules.Boat] },
        };

        // Act
        double pressure = Pressure(request, isHunted ? Hunting.OfHeavies : Hunting.OfLeaders);

        // Assert
        Assert.Equal(expected, pressure);
    }

    [Theory]
    [InlineData(1, false, -0.25)]
    [InlineData(0.5, false, -0.125)]
    [InlineData(1, true, 0.25)]
    public void Pressure_WhenOnlyOneEscapeCounts_CountsItAsLikelyAsThePieceMovesAndTheAttackBesides(
        double pace,
        bool isAttacked,
        double expected
    )
    {
        // Arrange - the enemy's Boat, a heavy piece, at the line's end, free to flee to 6 cells or more: one counts,
        // as likely as the dice let it move; attacked or not by our Boat at the line's start, which covers all between
        // (the user, 2026-11-27: "it only just needs one unguarded square")
        DecisionRequest request = LineRules.Request(
            [],
            [(7, LineRules.Boat, LineRules.Enemy), .. isAttacked ? [(0, LineRules.Boat, LineRules.Seat)] : Nothing]
        );
        request = request with
        {
            Rules = new LineRules([.. request.LegalActions]) { Heavies = [LineRules.Boat], Pace = pace },
        };
        Hunting hunting = Hunting.OfHeavies with { Escapes = 1 };

        // Act
        double pressure = Pressure(request, hunting);

        // Assert - attacked, the Boat on 0 covers every cell it could flee to
        Assert.Equal(expected, pressure);
    }

    private static readonly (int, PieceKindId, SeatId)[] Nothing = [];

    [Fact]
    public void Pressure_WhenHeaviesAreHunted_CountsALeaderAtItsShare()
    {
        // Arrange - the enemy's Raja alone at the line's end: one escape, at half its share
        DecisionRequest request = LineRules.Request([], (7, LineRules.Raja, LineRules.Enemy));

        // Act
        double pressure = Pressure(request, Hunting.OfHeavies);

        // Assert
        Assert.Equal(-0.125, pressure);
    }

    private static double Pressure(DecisionRequest request) => Pressure(request, Hunting.OfLeaders);

    private static double Pressure(DecisionRequest request, Hunting hunting) =>
        Endgame.Pressure(
            request,
            request.State,
            Careful.AttacksBy(request, request.State, isAllied: true),
            Endgame.ScaleOf(SageWeights.Default),
            hunting
        );

    /// <summary>The request, its line counted as a board of <paramref name="cells"/> cells.</summary>
    private static DecisionRequest OnBoardOf(int cells, DecisionRequest request) =>
        request with
        {
            Rules = new LineRules([.. request.LegalActions]) { Topology = new LineTopology(cells) },
        };
}
