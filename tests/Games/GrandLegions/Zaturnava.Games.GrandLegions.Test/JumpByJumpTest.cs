using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

/// <summary>
/// A Checkers King's chain of jumps on the medium board, 8 × 8, cell = row × 8 + column: Black's King on b2 (9), an
/// enemy Knight on c3 (18) and Rook on e5 (36), so that it may jump to d4 (27), then on to f6 (45).
/// </summary>
public sealed class JumpByJumpTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Enemy = new(1);
    private static readonly CellId B2 = new(9);
    private static readonly CellId C3 = new(18);
    private static readonly CellId D4 = new(27);
    private static readonly CellId E5 = new(36);
    private static readonly CellId F6 = new(45);

    private readonly Chaturaji2027Rules _rules = GrandLegionsRules.RulesOf(QuickGame.Medium);

    [Fact]
    public void LegalActions_BeforeJumpByJump_OffersTheWholeChainAsOneMove()
    {
        // Arrange
        GameState state = this.Chain(GrandLegionsRules.JumpByJumpEdition - 1);

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Contains(new MoveAction(Black, B2, F6), actions);
    }

    [Fact]
    public void LegalActions_FromJumpByJump_OffersOnlyTheFirstJump()
    {
        // Arrange
        GameState state = this.Chain(GrandLegionsRules.JumpByJumpEdition);

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Contains(new MoveAction(Black, B2, D4), actions);
        Assert.DoesNotContain(new MoveAction(Black, B2, F6), actions);
    }

    [Fact]
    public void Apply_WhenAJumpCanGoOn_TakesTheJumpedAndOffersOnlyToStopOrJumpOnWithTheSameDieLeft()
    {
        // Arrange
        GameState state = this.Chain(GrandLegionsRules.JumpByJumpEdition);

        // Act
        GameState jumped = this._rules.Apply(state, new MoveAction(Black, B2, D4)).State;

        // Assert
        Assert.Equal(
            ((CellId?)D4, (Piece?)null, "5", (SeatId?)Black),
            (jumped.Jumping, jumped.Position.PieceAt(C3), string.Join(",", jumped.Dice), jumped.DecidingSeat)
        );
        Assert.Equal([new PassAction(Black), new MoveAction(Black, D4, F6)], this._rules.LegalActions(jumped));
    }

    [Fact]
    public void Apply_WhenTheChainGoesOnToItsEnd_TakesTheNextUsingNoDieAndStops()
    {
        // Arrange
        GameState jumped = this._rules.Apply(
            this.Chain(GrandLegionsRules.JumpByJumpEdition),
            new MoveAction(Black, B2, D4)
        ).State;

        // Act
        GameState ended = this._rules.Apply(jumped, new MoveAction(Black, D4, F6)).State;

        // Assert
        Assert.Equal(
            ((CellId?)null, (Piece?)null, "5", (SeatId?)Black),
            (ended.Jumping, ended.Position.PieceAt(E5), string.Join(",", ended.Dice), ended.DecidingSeat)
        );
    }

    [Fact]
    public void Apply_WhenThePlayerStopsJumping_LeavesTheRestStandingAndTheTurnGoingOn()
    {
        // Arrange
        GameState jumped = this._rules.Apply(
            this.Chain(GrandLegionsRules.JumpByJumpEdition),
            new MoveAction(Black, B2, D4)
        ).State;

        // Act
        GameState stopped = this._rules.Apply(jumped, new PassAction(Black)).State;

        // Assert
        Assert.Equal(
            ((CellId?)null, GrandLegionsPieces.Rook, "5", (SeatId?)Black),
            (stopped.Jumping, stopped.Position.PieceAt(E5)!.Kind, string.Join(",", stopped.Dice), stopped.DecidingSeat)
        );
    }

    [Fact]
    public void ChancesOf_WhileAPieceIsJumping_AreNone()
    {
        // Arrange
        GameState jumped = this._rules.Apply(
            this.Chain(GrandLegionsRules.JumpByJumpEdition),
            new MoveAction(Black, B2, D4)
        ).State;

        // Act
        ImmutableList<GameAction> chances = this._rules.ChancesOf(jumped, Enemy);

        // Assert
        Assert.Empty(chances);
    }

    /// <summary>
    /// Black to move, on two leader's dice, its Checkers King on b2 before the chain; each army's leader standing far
    /// off, and the others far ahead, so that no capture ends the game.
    /// </summary>
    private GameState Chain(int edition)
    {
        GameState initial = this._rules.CreateInitialState();
        return initial with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            TurnNumber = 2,
            Edition = edition,
            Dice = [5, 5],
            Scores = initial.Scores.SetItem(Enemy, 5000).SetItem(new SeatId(2), 5000).SetItem(new SeatId(3), 5000),
            Position = Position.Empty
                .Place(B2, new Piece(new PieceId(1), GrandLegionsPieces.CheckersKing, Black))
                .Place(C3, new Piece(new PieceId(2), GrandLegionsPieces.Knight, Enemy))
                .Place(E5, new Piece(new PieceId(3), GrandLegionsPieces.Rook, Enemy))
                .Place(new CellId(63), new Piece(new PieceId(4), GrandLegionsPieces.WesternKing, Enemy))
                .Place(new CellId(56), new Piece(new PieceId(5), GrandLegionsPieces.WesternKing, new SeatId(2)))
                .Place(new CellId(7), new Piece(new PieceId(6), GrandLegionsPieces.WesternKing, new SeatId(3)))
                .Place(new CellId(55), new Piece(new PieceId(7), GrandLegionsPieces.Rook, Enemy)),
        };
    }
}
