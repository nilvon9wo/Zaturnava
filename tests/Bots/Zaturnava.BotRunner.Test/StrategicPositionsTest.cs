using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

/// <summary>
/// The strategic positions of the bots' strategy plan (docs/bots/strategy-plan.md), played by Sage on Chaturaji
/// 2027's own board, without dice, so that every move is Sage's to choose.
/// </summary>
public sealed class StrategicPositionsTest
{
    private const int Side = 8;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);

    private readonly Chaturaji2027Rules _rules = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sage_WhenItsPawnIsHeldBackByAnAllysBackToBack_SwapsThemRatherThanShuffle()
    {
        // Arrange - Black's Pawn on d4 faces Green's, its ally's, on d5, neither able to step on; Black's Raja holds
        // Red's throne, which any move of it would give up; Yellow's six Pawns, far off in the a8 corner, keep the game
        // going, and keep it from being an endgame, so that Sage judges its own turn only
        CellId pawn = CellAt(3, 3);
        CellId ahead = CellAt(3, 4);
        GameState state = this.BlackDeciding(
            [
                (pawn, new Piece(new PieceId(1), Chaturaji2027Pieces.Pawn, Black)),
                (ahead, new Piece(new PieceId(2), Chaturaji2027Pieces.Pawn, Green)),
                (this._rules.Thrones[Red], new Piece(new PieceId(3), Chaturaji2027Pieces.Raja, Black)),
                .. new[] { (0, 7), (1, 7), (2, 7), (0, 6), (1, 6), (0, 5) }.Select((cell, index) => (
                    CellAt(cell.Item1, cell.Item2),
                    new Piece(new PieceId(10 + index), Chaturaji2027Pieces.Pawn, Yellow)
                )),
            ]
        );
        DecisionRequest request = new(this._rules, state, Black, this._rules.LegalActions(state));

        // Act
        GameAction action = await new SageBot(new Random(7)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new MoveAction(Black, pawn, ahead), action);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Sage_WhenAPromotionIntoAnAllysRajaWouldStandAttackedOrNot_PromotesOnlyIntoWhatSurvives(
        bool isAttacked
    )
    {
        // Arrange - plan step 2, "promotion with eyes open" (seen 2026-11-25: a Pawn promoted into a Raja under
        // attack, taken at once): Black's Pawn on d8, its far edge, may become Green's Raja or Black's own Elephant
        // (the Rook), both held by Red; Red's Elephant on h8 attacks d8 along the empty row; Black's Raja holds Red's
        // throne, and Yellow's
        // Pawns, far off, keep it from being an endgame
        CellId pawn = CellAt(3, 7);
        Piece greenRaja = new(new PieceId(4), Chaturaji2027Pieces.Raja, Green);
        Piece blackRook = new(new PieceId(5), Chaturaji2027Pieces.Elephant, Black);
        GameState state = this.BlackDeciding(
            [
                (pawn, new Piece(new PieceId(1), Chaturaji2027Pieces.Pawn, Black)),
                (CellAt(7, isAttacked ? 7 : 6), new Piece(new PieceId(2), Chaturaji2027Pieces.Elephant, Red)),
                (this._rules.Thrones[Red], new Piece(new PieceId(3), Chaturaji2027Pieces.Raja, Black)),
                (CellAt(7, 1), new Piece(new PieceId(6), Chaturaji2027Pieces.Raja, Red)),
                .. new[] { (0, 2), (1, 2), (0, 1), (1, 1), (0, 0), (1, 0) }.Select((cell, index) => (
                    CellAt(cell.Item1, cell.Item2),
                    new Piece(new PieceId(10 + index), Chaturaji2027Pieces.Pawn, Yellow)
                )),
            ]
        ) with
        {
            Prisoners = [new Prisoner(greenRaja, Red), new Prisoner(blackRook, Red)],
        };
        DecisionRequest request = new(this._rules, state, Black, this._rules.LegalActions(state));

        // Act
        GameAction action = await new SageBot(new Random(7)).Decide(request, this._cancellation);

        // Assert - offered either way; the Raja not brought back where it would be taken at once, but promoted into
        // when safe, an ally's leader back on the board (the Elephant on h7 attacks the seventh row, not d8)
        Assert.Equal(
            (true, !isAttacked),
            (
                request.LegalActions.Contains(new PromoteAction(Black, pawn, greenRaja.Id)),
                action == new PromoteAction(Black, pawn, greenRaja.Id)
            )
        );
    }

    [Fact]
    public async Task Sage_WhenTwoElephantsFaceALoneRaja_PlaysItToTheEndAndWins()
    {
        // Arrange - the endgame plan's test (docs/bots/strategy-plan.md, "Endgame tables"): Black's two Elephants (the
        // Rooks) and its Raja against Red's Raja alone in the middle, no one else on the board; Sage plays every side,
        // so the lone Raja flees as well as Sage can
        GameState state = this.BlackDeciding(
            (CellAt(0, 0), new Piece(new PieceId(1), Chaturaji2027Pieces.Elephant, Black)),
            (CellAt(7, 0), new Piece(new PieceId(2), Chaturaji2027Pieces.Elephant, Black)),
            (CellAt(4, 0), new Piece(new PieceId(3), Chaturaji2027Pieces.Raja, Black)),
            (CellAt(3, 4), new Piece(new PieceId(4), Chaturaji2027Pieces.Raja, Red))
        );
        SageBot sage = new(new Random(7));

        // Act
        int decisions = 0;
        while (state.Outcome is null && decisions < MostEndgameDecisions)
        {
            SeatId seat = state.DecidingSeat!.Value;
            DecisionRequest request = new(this._rules, state, seat, this._rules.LegalActions(state));
            state = this._rules.Apply(state, await sage.Decide(request, this._cancellation)).State;
            decisions++;
        }

        // Assert
        Assert.Equal(
            (true, true),
            (state.Outcome is not null, state.Outcome?.Winners.Contains(Black) == true)
        );
    }

    /// <summary>How many decisions, every seat's, the endgame test allows before calling it not converted.</summary>
    private const int MostEndgameDecisions = 120;

    private static CellId CellAt(int column, int row) => new((row * Side) + column);

    /// <summary>Black to decide, without dice, with only these pieces on the board.</summary>
    private GameState BlackDeciding(params (CellId Cell, Piece Piece)[] pieces) =>
        this._rules.WithoutDice(
            this._rules.CreateInitialState() with
            {
                RollOff = [],
                AwaitedDice = null,
                DecidingSeat = Black,
                Position = pieces.Aggregate(Position.Empty, (board, placed) => board.Place(placed.Cell, placed.Piece)),
            }
        );
}
