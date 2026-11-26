using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// Restrictions bought off, and how a piece then moves and scores (specification §7), on the 8 × 8 board, cell = row ×
/// 8 + column: Black's Horse on d4 (27), on two Horse dice. Lifted, a Horse's movement makes it step orthogonally in
/// place of leaping, for 3; its area lets it step diagonally too, for 1. Each army's Raja stands far off.
/// </summary>
public sealed class LiftedMovementTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Enemy = new(1);
    private static readonly CellId D4 = new(27);
    private static readonly PieceId Mover = new(1);

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with
        {
            MovementLifted = ImmutableDictionary<PieceKindId, ImmutableList<MovementRule>>.Empty.Add(
                Chaturaji2027Pieces.Horse,
                [MovementRule.Step(DirectionSets.Orthogonal)]
            ),
            AreaLiftAdds = ImmutableDictionary<PieceKindId, ImmutableList<MovementRule>>.Empty.Add(
                Chaturaji2027Pieces.Horse,
                [MovementRule.Step(DirectionSets.Diagonal)]
            ),
            Drafting = new Drafting(6, [], (_, _) => [], _ => new ArmyFilled([], new ArmySquare(0, 0)))
            {
                LiftPrice = (kind, restriction) =>
                    kind != Chaturaji2027Pieces.Horse ? null
                    : restriction == LiftKind.Movement ? 3
                    : 1,
            },
        }
    );

    [Theory]
    [InlineData(false, false, 8)]
    [InlineData(true, false, 4)]
    [InlineData(false, true, 12)]
    [InlineData(true, true, 8)]
    public void LegalActions_ForAHorseWithLiftsBought_MovesAsItsLiftsSay(bool isFreed, bool isLoosed, int expected)
    {
        // Arrange
        ImmutableHashSet<Lift> lifts =
        [
            .. isFreed ? [new Lift(Mover, Chaturaji2027Pieces.Horse, LiftKind.Movement)] : Array.Empty<Lift>(),
            .. isLoosed ? [new Lift(Mover, Chaturaji2027Pieces.Horse, LiftKind.Area)] : Array.Empty<Lift>(),
        ];
        GameState state = this.Horse() with { Lifts = lifts };

        // Act
        int moves = this._rules.LegalActions(state).OfType<MoveAction>().Count(move => move.From == D4);

        // Assert
        Assert.Equal(expected, moves);
    }

    [Theory]
    [InlineData(false, false, -1)]
    [InlineData(true, false, 4)]
    [InlineData(false, true, 12)]
    [InlineData(true, true, 8)]
    public void LiftedDiagramAt_ForAHorseWithLiftsBought_DrawsHowItThenMoves(bool isFreed, bool isLoosed, int expected)
    {
        // Arrange
        ImmutableHashSet<Lift> lifts =
        [
            .. isFreed ? [new Lift(Mover, Chaturaji2027Pieces.Horse, LiftKind.Movement)] : Array.Empty<Lift>(),
            .. isLoosed ? [new Lift(Mover, Chaturaji2027Pieces.Horse, LiftKind.Area)] : Array.Empty<Lift>(),
        ];
        GameState state = this.Horse() with { Lifts = lifts };

        // Act
        MoveDiagram? diagram = this._rules.LiftedDiagramAt(state, D4);

        // Assert - none, without a lift
        Assert.Equal(expected, diagram?.Moves.Count ?? -1);
    }

    [Theory]
    [InlineData(27)]
    [InlineData(28)]
    public void LiftedDiagramAt_WhereNoPieceWithAMovementLiftTheVariantTellsOfStands_DrawsNothing(int cell)
    {
        // Arrange - a Raja's movement lifted, which the variant gives no move for; and an empty square
        GameState state = this.Horse() with
        {
            Lifts = [new Lift(new PieceId(4), Chaturaji2027Pieces.Raja, LiftKind.Movement)],
        };
        state = state with
        {
            Position = state.Position.Place(D4, new Piece(new PieceId(4), Chaturaji2027Pieces.Raja, Black)),
        };

        // Act
        MoveDiagram? diagram = this._rules.LiftedDiagramAt(state, new CellId(cell));

        // Assert
        Assert.Null(diagram);
    }

    [Fact]
    public void Apply_WhenAPieceWithALiftIsTaken_ScoresItsWorthAndWhatItsLiftCost()
    {
        // Arrange - the enemy's Horse on e6 (44), its movement restriction bought off (3) and nothing else
        GameState state = this.Horse() with
        {
            Lifts = [new Lift(new PieceId(9), Chaturaji2027Pieces.Horse, LiftKind.Movement)],
        };
        state = state with
        {
            Position = state.Position.Place(
                new CellId(44),
                new Piece(new PieceId(9), Chaturaji2027Pieces.Horse, Enemy)
            ),
        };

        // Act
        Transition taken = this._rules.Apply(state, new MoveAction(Black, D4, new CellId(44)));

        // Assert
        Assert.Equal(
            this._rules.WorthOf(Chaturaji2027Pieces.Horse) + 3,
            taken.Events.OfType<PieceCaptured>().Single().Points
        );
    }

    /// <summary>Black to move its Horse on d4, on two Horse dice; each army's Raja in a corner.</summary>
    private GameState Horse()
    {
        GameState initial = this._rules.CreateInitialState();
        return initial with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            TurnNumber = 2,
            Dice = [2, 2],
            Scores = initial.Scores.SetItem(Enemy, 500).SetItem(new SeatId(2), 500).SetItem(new SeatId(3), 500),
            Position = Position.Empty
                .Place(D4, new Piece(Mover, Chaturaji2027Pieces.Horse, Black))
                .Place(new CellId(0), new Piece(new PieceId(4), Chaturaji2027Pieces.Raja, Black))
                .Place(new CellId(63), new Piece(new PieceId(5), Chaturaji2027Pieces.Raja, Enemy))
                .Place(new CellId(56), new Piece(new PieceId(6), Chaturaji2027Pieces.Raja, new SeatId(2)))
                .Place(new CellId(7), new Piece(new PieceId(7), Chaturaji2027Pieces.Raja, new SeatId(3))),
        };
    }
}
