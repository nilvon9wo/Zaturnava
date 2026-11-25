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
