using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// Castling, given here to Chaturaji 2027's Raja, with its Elephants: Black's Raja on d1, its Elephants on a1 and
/// h1; Yellow's pieces are its enemies, Green's its allies.
/// </summary>
public sealed class CastlingTest
{
    private const int RajaDie = 5;
    private const int ElephantDie = 3;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

    private static readonly Piece BlackRaja = new(new PieceId(1), Chaturaji2027Pieces.Raja, Black);
    private static readonly Piece LeftElephant = new(new PieceId(2), Chaturaji2027Pieces.Elephant, Black);
    private static readonly Piece RightElephant = new(new PieceId(3), Chaturaji2027Pieces.Elephant, Black);
    private static readonly Piece BlackPawn = new(new PieceId(4), Chaturaji2027Pieces.Pawn, Black);
    private static readonly Piece YellowElephant = new(new PieceId(5), Chaturaji2027Pieces.Elephant, Yellow);
    private static readonly Piece GreenElephant = new(new PieceId(6), Chaturaji2027Pieces.Elephant, Green);
    private static readonly Piece NearElephant = new(new PieceId(7), Chaturaji2027Pieces.Elephant, Black);

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with
        {
            Castlers = [Chaturaji2027Pieces.Raja],
            CastlingRooks = [Chaturaji2027Pieces.Elephant],
        }
    );

    [Fact]
    public void LegalActions_WhenNothingStandsBetweenTheCastlerAndItsRooks_MayCastleEachWay()
    {
        // Arrange
        GameState state = BlackToMove(RajaDie);

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Contains(new MoveAction(Black, Cell(3, 0), Cell(1, 0)), actions);
        Assert.Contains(new MoveAction(Black, Cell(3, 0), Cell(5, 0)), actions);
    }

    [Fact]
    public void LegalActions_WhenTheRulesGiveNoCastlers_OffersNoCastling()
    {
        // Arrange
        Chaturaji2027Rules rules = new();
        GameState state = BlackToMove(RajaDie);

        // Act
        ImmutableList<GameAction> actions = rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Cell(3, 0), Cell(5, 0)), actions);
    }

    public static TheoryData<string, int, int, Piece> NoCastlings =>
        new()
        {
            { "a piece between", 6, 0, BlackPawn },
            { "the rook too near", 5, 0, NearElephant },
            { "an enemy's rook", 7, 0, YellowElephant },
            { "the start attacked", 3, 5, YellowElephant },
            { "the cell passed attacked", 4, 5, YellowElephant },
            { "the landing attacked", 5, 5, YellowElephant },
        };

    [Theory]
    [MemberData(nameof(NoCastlings), DisableDiscoveryEnumeration = true)]
    public void LegalActions_WhenBlockedOrAttackedOrNoRookOfItsOwn_OffersNoCastlingThatWay(
        string why,
        int column,
        int row,
        Piece piece
    )
    {
        // Arrange
        GameState start = BlackToMove(RajaDie);
        GameState state = start with { Position = start.Position.Place(Cell(column, row), piece) };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Cell(3, 0), Cell(5, 0)), actions);
        Assert.NotEmpty(why);
    }

    [Fact]
    public void LegalActions_WhenOnlyAnAllyCouldCaptureOnTheCellsCrossed_MayCastle()
    {
        // Arrange
        GameState start = BlackToMove(RajaDie);
        GameState state = start with { Position = start.Position.Place(Cell(4, 5), GreenElephant) };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Contains(new MoveAction(Black, Cell(3, 0), Cell(5, 0)), actions);
    }

    [Theory]
    [InlineData(1, 2, 0)]
    [InlineData(5, 4, 7)]
    public void Apply_WhenTheCastlerCastles_MovesTheRookOntoTheCellItPassed(int to, int rookTo, int rookFrom)
    {
        // Arrange
        GameState state = BlackToMove(RajaDie);

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(3, 0), Cell(to, 0)));

        // Assert
        Position position = transition.State.Position;
        Piece rook = rookFrom == 0 ? LeftElephant : RightElephant;
        Assert.Equal(
            (BlackRaja, rook, null),
            (position.PieceAt(Cell(to, 0)), position.PieceAt(Cell(rookTo, 0)), position.PieceAt(Cell(rookFrom, 0)))
        );
        Assert.Contains(new PieceMoved(rook, Cell(rookFrom, 0), Cell(rookTo, 0), Black), transition.Events);
    }

    [Fact]
    public void Apply_WhenTheCastlerSteps_MovesNoRook()
    {
        // Arrange
        GameState state = BlackToMove(RajaDie);

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(3, 0), Cell(4, 0)));

        // Assert
        Assert.Equal(
            (BlackRaja, RightElephant),
            (transition.State.Position.PieceAt(Cell(4, 0)), transition.State.Position.PieceAt(Cell(7, 0)))
        );
        _ = Assert.Single(transition.Events.OfType<PieceMoved>());
    }

    [Fact]
    public void Apply_WhenTheCastlerTakes_MovesNoRook()
    {
        // Arrange - Yellow's Elephant on e1, beside the Raja
        GameState start = BlackToMove(RajaDie);
        GameState state = start with
        {
            Position = start.Position.Place(Cell(4, 0), YellowElephant),
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(3, 0), Cell(4, 0)));

        // Assert
        Assert.Equal(BlackRaja, transition.State.Position.PieceAt(Cell(4, 0)));
        _ = Assert.Single(transition.Events.OfType<PieceMoved>());
    }

    [Fact]
    public void Apply_WhenARookMoves_MovesNoCastler()
    {
        // Arrange
        GameState state = BlackToMove(ElephantDie);

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(7, 0), Cell(5, 0)));

        // Assert
        Assert.Equal(
            (BlackRaja, RightElephant),
            (transition.State.Position.PieceAt(Cell(3, 0)), transition.State.Position.PieceAt(Cell(5, 0)))
        );
    }

    private static CellId Cell(int column, int row) => Grid.CellAt(column, row);

    /// <summary>Black to move with <paramref name="die"/>, its Raja on d1 between its Elephants on a1 and h1.</summary>
    private static GameState BlackToMove(int die) =>
        new Chaturaji2027Rules().CreateInitialState() with
        {
            TurnNumber = 2,
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Dice = [die],
            Position = Position.Empty
                .Place(Cell(3, 0), BlackRaja)
                .Place(Cell(0, 0), LeftElephant)
                .Place(Cell(7, 0), RightElephant),
        };
}
