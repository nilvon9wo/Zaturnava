using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// A Horse that jumps diagonally, its chains played jump by jump, on the 8 × 8 board, cell = row × 8 + column:
/// Black's Horse on b2 (9), enemy Boats on c3 (18) and e5 (36), so that it may jump to d4 (27), then to f6 (45).
/// </summary>
public sealed class JumpByJumpTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Enemy = new(1);
    private static readonly CellId B2 = new(9);
    private static readonly CellId D4 = new(27);
    private static readonly CellId F6 = new(45);

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with
        {
            Kinds = Chaturaji2027Variant.Standard.Kinds.SetItem(
                Chaturaji2027Pieces.Horse,
                new PieceKind(Chaturaji2027Pieces.Horse, [MovementRule.Jump(DirectionSets.Diagonal)])
            ),
            JumpByJumpEdition = 0,
        }
    );

    [Fact]
    public void Apply_WhenAJumpCanGoOn_OffersOnlyToStopOrJumpOnUntilTheChainEnds()
    {
        // Arrange
        GameState state = this.Chain();

        // Act
        GameState jumped = this._rules.Apply(state, new MoveAction(Black, B2, D4)).State;
        ImmutableList<GameAction> choices = this._rules.LegalActions(jumped);
        GameState ended = this._rules.Apply(jumped, new MoveAction(Black, D4, F6)).State;

        // Assert
        Assert.Equal([new PassAction(Black), new MoveAction(Black, D4, F6)], choices);
        Assert.Equal(
            ((CellId?)D4, (CellId?)null, 1, (Piece?)null),
            (jumped.Jumping, ended.Jumping, jumped.Dice.Count, ended.Position.PieceAt(new CellId(36)))
        );
    }

    [Fact]
    public void Apply_WhenThePlayerStopsJumping_GoesOnWithTheDiceLeft()
    {
        // Arrange
        GameState jumped = this._rules.Apply(this.Chain(), new MoveAction(Black, B2, D4)).State;

        // Act
        GameState stopped = this._rules.Apply(jumped, new PassAction(Black)).State;

        // Assert
        Assert.Equal(((CellId?)null, (SeatId?)Black, 1), (stopped.Jumping, stopped.DecidingSeat, stopped.Dice.Count));
    }

    [Fact]
    public void ChancesOf_WhileAPieceIsJumping_AreNone()
    {
        // Arrange
        GameState jumped = this._rules.Apply(this.Chain(), new MoveAction(Black, B2, D4)).State;

        // Act
        ImmutableList<GameAction> chances = this._rules.ChancesOf(jumped, Enemy);

        // Assert
        Assert.Empty(chances);
    }

    [Fact]
    public void Kinds_WhenRead_AreEveryKindTheVariantPlays()
    {
        // Arrange - nothing beyond the rules

        // Act
        ImmutableList<PieceKindId> kinds = this._rules.Kinds;

        // Assert
        Assert.Equal(
            [.. Chaturaji2027Variant.Standard.Kinds.Keys.OrderBy(kind => kind.Value)],
            kinds
        );
    }

    /// <summary>
    /// Black to move, on two Horse dice, its Horse on b2 before the chain; each army's Raja standing far off, and the
    /// others far ahead, so that no capture ends the game.
    /// </summary>
    private GameState Chain()
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
                .Place(B2, new Piece(new PieceId(1), Chaturaji2027Pieces.Horse, Black))
                .Place(new CellId(18), new Piece(new PieceId(2), Chaturaji2027Pieces.Boat, Enemy))
                .Place(new CellId(36), new Piece(new PieceId(3), Chaturaji2027Pieces.Boat, Enemy))
                .Place(new CellId(0), new Piece(new PieceId(4), Chaturaji2027Pieces.Raja, Black))
                .Place(new CellId(63), new Piece(new PieceId(5), Chaturaji2027Pieces.Raja, Enemy))
                .Place(new CellId(56), new Piece(new PieceId(6), Chaturaji2027Pieces.Raja, new SeatId(2)))
                .Place(new CellId(7), new Piece(new PieceId(7), Chaturaji2027Pieces.Raja, new SeatId(3))),
        };
    }
}
