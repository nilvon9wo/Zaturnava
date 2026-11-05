using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// The flying-general capture, given here to Chaturaji 2027's Raja: Black's Raja on d1, Yellow's on d6, nothing
/// between them; Yellow's pieces are Black's enemies, Green's its allies.
/// </summary>
public sealed class FlyingCaptureTest
{
    private const int RajaDie = 5;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

    private static readonly Piece BlackRaja = new(new PieceId(1), Chaturaji2027Pieces.Raja, Black);
    private static readonly Piece YellowRaja = new(new PieceId(2), Chaturaji2027Pieces.Raja, Yellow);
    private static readonly Piece GreenRaja = new(new PieceId(3), Chaturaji2027Pieces.Raja, Green);
    private static readonly Piece YellowElephant = new(new PieceId(4), Chaturaji2027Pieces.Elephant, Yellow);
    private static readonly Piece BlackElephant = new(new PieceId(5), Chaturaji2027Pieces.Elephant, Black);

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with
        {
            FlyingCapturers = [Chaturaji2027Pieces.Raja],
            Castlers = [Chaturaji2027Pieces.Raja],
            CastlingRooks = [Chaturaji2027Pieces.Elephant],
        }
    );

    [Fact]
    public void LegalActions_WhenAnEnemyLeaderStandsAlongAnOpenLine_MayTakeItFlying()
    {
        // Arrange
        GameState state = BlackToMove((Cell(3, 5), YellowRaja));

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Contains(new MoveAction(Black, Cell(3, 0), Cell(3, 5)), actions);
    }

    [Fact]
    public void LegalActions_WhenTheRulesGiveNoFlyers_OffersNoFlyingCapture()
    {
        // Arrange
        GameState state = BlackToMove((Cell(3, 5), YellowRaja));

        // Act
        ImmutableList<GameAction> actions = new Chaturaji2027Rules().LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Cell(3, 0), Cell(3, 5)), actions);
    }

    public static TheoryData<string, Piece, Piece?> NoFlyingCaptures =>
        new()
        {
            { "a piece between", YellowRaja, YellowElephant },
            { "no leader", YellowElephant, null },
            { "an ally's leader", GreenRaja, null },
        };

    [Theory]
    [MemberData(nameof(NoFlyingCaptures), DisableDiscoveryEnumeration = true)]
    public void LegalActions_WhenTheLineIsBlockedOrEndsInNoEnemyLeader_OffersNoFlyingCapture(
        string why,
        Piece target,
        Piece? between
    )
    {
        // Arrange
        GameState start = BlackToMove((Cell(3, 5), target));
        GameState state = between is null
            ? start
            : start with { Position = start.Position.Place(Cell(3, 3), between) };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Cell(3, 0), Cell(3, 5)), actions);
        Assert.NotEmpty(why);
    }

    [Fact]
    public void Apply_WhenTheFlyerTakesFlying_TakesTheLeaderOnItsCell()
    {
        // Arrange
        GameState state = BlackToMove((Cell(3, 5), YellowRaja));

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(3, 0), Cell(3, 5)));

        // Assert
        Assert.Equal(BlackRaja, transition.State.Position.PieceAt(Cell(3, 5)));
        Assert.Contains(transition.Events, happened => happened is PieceCaptured { Piece: var piece }
            && piece == YellowRaja
        );
    }

    [Fact]
    public void AttacksOf_WhenAFlyerFacesALeaderAlongAnOpenLine_CountsItOnce()
    {
        // Arrange
        GameState state = BlackToMove((Cell(3, 5), YellowRaja));

        // Act
        ImmutableList<Attack> attacks = this._rules.AttacksOf(state, Yellow);

        // Assert
        _ = Assert.Single(attacks, attack => attack.Target == Cell(3, 0));
    }

    [Fact]
    public void LegalActions_WhenAnEnemyFlyerCouldTakeTheCastlerWhereItPasses_OffersNoCastlingThatWay()
    {
        // Arrange - Yellow's Raja on e6 looks down the open e-file onto e1, which Black's Raja would pass
        GameState start = BlackToMove((Cell(4, 5), YellowRaja));
        GameState state = start with
        {
            Position = start.Position.Place(Cell(0, 0), BlackElephant).Place(Cell(7, 0), BlackElephant),
        };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Cell(3, 0), Cell(5, 0)), actions);
        Assert.Contains(new MoveAction(Black, Cell(3, 0), Cell(1, 0)), actions);
    }

    private static CellId Cell(int column, int row) => Grid.CellAt(column, row);

    /// <summary>Black to move its Raja, on d1, with the pieces placed.</summary>
    private static GameState BlackToMove(params (CellId Cell, Piece Piece)[] placements) =>
        new Chaturaji2027Rules().CreateInitialState() with
        {
            TurnNumber = 2,
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Dice = [RajaDie],
            Position = placements.Aggregate(
                Position.Empty.Place(Cell(3, 0), BlackRaja),
                (position, placed) => position.Place(placed.Cell, placed.Piece)
            ),
        };
}
