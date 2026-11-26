using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

/// <summary>
/// Pieces whose restrictions were bought off, on the medium board, 8 × 8, cell = row × 8 + column: Black's piece on d4
/// (27); every seat's King far off, in a corner.
/// </summary>
public sealed class LiftedMovesTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly CellId D4 = new(27);
    private static readonly PieceId Mover = new(1);

    private readonly Chaturaji2027Rules _rules = GrandLegionsRules.RulesOf(QuickGame.Medium);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void LegalActions_WhenAHorsesLegIsBlocked_LetsItLeapOverOnlyOnceItsRestrictionIsBoughtOff(
        bool isLifted,
        bool expected
    )
    {
        // Arrange - a piece of Black's on d5 (35) blocks the Horse's leg up to c6 (42)
        GameState state = this.With(
            GrandLegionsPieces.Horse,
            2,
            isLifted ? LiftKind.Movement : null,
            (new CellId(35), new Piece(new PieceId(9), GrandLegionsPieces.Rook, Black))
        );

        // Act
        bool leaps = this._rules.LegalActions(state).Contains(new MoveAction(Black, D4, new CellId(42)));

        // Assert
        Assert.Equal(expected, leaps);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(LiftKind.Area, 2)]
    [InlineData(LiftKind.Movement, 1)]
    public void LegalActions_ForAJanggiSoldierWithALiftBought_AddsItsDiagonalsOrItsStepBack(
        LiftKind? lift,
        int expected
    )
    {
        // Arrange - in open ground, outside every palace
        GameState plain = this.With(GrandLegionsPieces.JanggiSoldier, 5, null);
        GameState lifted = this.With(GrandLegionsPieces.JanggiSoldier, 5, lift);

        // Act
        int gained = this.MovesFrom(lifted) - this.MovesFrom(plain);

        // Assert
        Assert.Equal(expected, gained);
    }

    [Fact]
    public void LegalActions_ForARookWithALiftItHasNone_MovesAsARook()
    {
        // Arrange - a lift the Rook has no move for, set by hand
        GameState plain = this.With(GrandLegionsPieces.Rook, 3, null);
        GameState lifted = this.With(GrandLegionsPieces.Rook, 3, LiftKind.Movement);

        // Act
        int moves = this.MovesFrom(lifted);

        // Assert
        Assert.Equal(this.MovesFrom(plain), moves);
    }

    [Fact]
    public void Apply_WhenAPieceWithALiftIsTaken_ScoresItsValueAndWhatItsLiftCost()
    {
        // Arrange - Black's Rook on d4 takes Yellow's Horse on d6 (43), its movement restriction bought off (2)
        GameState state = this.With(GrandLegionsPieces.Rook, 3, null) with
        {
            Lifts = [new Lift(new PieceId(9), GrandLegionsPieces.Horse, LiftKind.Movement)],
        };
        state = state with
        {
            Position = state.Position.Place(
                new CellId(43),
                new Piece(new PieceId(9), GrandLegionsPieces.Horse, Yellow)
            ),
        };

        // Act
        Transition taken = this._rules.Apply(state, new MoveAction(Black, D4, new CellId(43)));

        // Assert
        Assert.Equal(
            this._rules.WorthOf(GrandLegionsPieces.Horse) + 2,
            taken.Events.OfType<PieceCaptured>().Single().Points
        );
    }

    private int MovesFrom(GameState state) =>
        this._rules.LegalActions(state).OfType<MoveAction>().Count(move => move.From == D4);

    /// <summary>
    /// Black to move a <paramref name="kind"/> on d4, with a <paramref name="die"/> twice, its
    /// <paramref name="lift"/> bought off, if any; and the pieces given.
    /// </summary>
    private GameState With(
        PieceKindId kind,
        int die,
        LiftKind? lift,
        params (CellId Cell, Piece Piece)[] others
    )
    {
        GameState initial = this._rules.CreateInitialState();
        Position position = Position.Empty
            .Place(D4, new Piece(Mover, kind, Black))
            .Place(new CellId(63), new Piece(new PieceId(4), GrandLegionsPieces.WesternKing, Yellow))
            .Place(new CellId(56), new Piece(new PieceId(5), GrandLegionsPieces.WesternKing, new SeatId(2)))
            .Place(new CellId(7), new Piece(new PieceId(6), GrandLegionsPieces.WesternKing, new SeatId(3)));
        return initial with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            TurnNumber = 2,
            Dice = [die, die],
            Palaces = null,
            Position = others.Aggregate(position, (placed, other) => placed.Place(other.Cell, other.Piece)),
            Lifts = lift is { } restriction ? [new Lift(Mover, kind, restriction)] : [],
        };
    }
}
