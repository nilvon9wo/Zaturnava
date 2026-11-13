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
/// Taking by jumping and by custody, given here to Chaturaji 2027's Boat (which steps diagonally and jumps as a piece
/// of Checkers does) and Elephant (which also takes by custody): Black moves; Yellow's pieces are its enemies, Green's
/// its allies.
/// </summary>
public sealed class JumpAndCustodyTest
{
    private const int BoatDie = 1;
    private const int ElephantDie = 3;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

    private static readonly Piece BlackRaja = new(new PieceId(1), Chaturaji2027Pieces.Raja, Black);
    private static readonly Piece YellowRaja = new(new PieceId(2), Chaturaji2027Pieces.Raja, Yellow);
    private static readonly Piece BlackBoat = new(new PieceId(3), Chaturaji2027Pieces.Boat, Black);
    private static readonly Piece BlackElephant = new(new PieceId(4), Chaturaji2027Pieces.Elephant, Black);
    private static readonly Piece BlackPawn = new(new PieceId(5), Chaturaji2027Pieces.Pawn, Black);
    private static readonly Piece FirstHorse = new(new PieceId(6), Chaturaji2027Pieces.Horse, Yellow);
    private static readonly Piece SecondHorse = new(new PieceId(7), Chaturaji2027Pieces.Horse, Yellow);
    private static readonly Piece ThirdHorse = new(new PieceId(8), Chaturaji2027Pieces.Horse, Yellow);
    private static readonly Piece GreenPawn = new(new PieceId(9), Chaturaji2027Pieces.Pawn, Green);

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with
        {
            Kinds = Chaturaji2027Variant.Standard.Kinds.SetItem(
                Chaturaji2027Pieces.Boat,
                new PieceKind(
                    Chaturaji2027Pieces.Boat,
                    [MovementRule.Step(DirectionSets.Diagonal).MoveOnly(), MovementRule.Jump(DirectionSets.Diagonal)]
                )
            ),
            Custodians = [Chaturaji2027Pieces.Elephant],
        }
    );

    [Fact]
    public void Apply_WhenAPieceJumpsTwoEnemiesInTurn_TakesBothAndLandsBeyondTheLast()
    {
        // Arrange - Black's Boat on c1; Yellow's Horses on d2 and d4, with e3 and c5 empty
        GameState state = BlackToMove(
            BoatDie,
            (Cell(2, 0), BlackBoat),
            (Cell(3, 1), FirstHorse),
            (Cell(3, 3), SecondHorse)
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(2, 0), Cell(2, 4)));

        // Assert
        Assert.Equal(
            (BlackBoat, null, null, "6,7"),
            (
                transition.State.Position.PieceAt(Cell(2, 4)),
                transition.State.Position.PieceAt(Cell(3, 1)),
                transition.State.Position.PieceAt(Cell(3, 3)),
                string.Join(',', transition.Events.OfType<PieceCaptured>().Select(captured => captured.Piece.Id.Value))
            )
        );
    }

    [Fact]
    public void Apply_WhenAPieceThatJumpsOnlySteps_TakesNothing()
    {
        // Arrange - Black's Boat on c1, Yellow's Horse on d2, the Boat stepping to b2
        GameState state = BlackToMove(BoatDie, (Cell(2, 0), BlackBoat), (Cell(3, 1), FirstHorse));

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(2, 0), Cell(1, 1)));

        // Assert
        Assert.Equal(
            (BlackBoat, FirstHorse, 0),
            (
                transition.State.Position.PieceAt(Cell(1, 1)),
                transition.State.Position.PieceAt(Cell(3, 1)),
                transition.Events.OfType<PieceCaptured>().Count()
            )
        );
    }

    [Fact]
    public void LegalActions_WhenAPieceMayJump_OffersEachJumpOfTheChainAsAMove()
    {
        // Arrange
        GameState state = BlackToMove(
            BoatDie,
            (Cell(2, 0), BlackBoat),
            (Cell(3, 1), FirstHorse),
            (Cell(3, 3), SecondHorse)
        );

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Contains(new MoveAction(Black, Cell(2, 0), Cell(4, 2)), actions);
        Assert.Contains(new MoveAction(Black, Cell(2, 0), Cell(2, 4)), actions);
    }

    [Fact]
    public void Apply_WhenACustodianHoldsAnEnemyAgainstOneOfItsOwn_TakesItButNotAnAlly()
    {
        // Arrange - the Elephant goes a1 to a3: Yellow's Horse on b3, Black's Pawn on c3 beyond it; Green's Pawn on
        // a4, Black's on a5 beyond it
        GameState state = BlackToMove(
            ElephantDie,
            (Cell(0, 0), BlackElephant),
            (Cell(1, 2), FirstHorse),
            (Cell(2, 2), BlackPawn),
            (Cell(0, 3), GreenPawn),
            (Cell(0, 4), BlackPawn with { Id = new PieceId(10) })
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(0, 0), Cell(0, 2)));

        // Assert
        Assert.Equal(
            (null, GreenPawn, FirstHorse),
            (
                transition.State.Position.PieceAt(Cell(1, 2)),
                transition.State.Position.PieceAt(Cell(0, 3)),
                Assert.Single(transition.Events.OfType<PieceCaptured>()).Piece
            )
        );
    }

    [Fact]
    public void Apply_WhenNoEnemyNextToTheCustodianHasOneOfItsOwnBeyond_TakesNothing()
    {
        // Arrange - the Elephant goes b1 to b2: Yellow's Horses on a2, by the edge; on c2, with d2 empty; and on b3,
        // with another of Yellow's beyond, on b4
        GameState state = BlackToMove(
            ElephantDie,
            (Cell(1, 0), BlackElephant),
            (Cell(0, 1), FirstHorse),
            (Cell(2, 1), SecondHorse),
            (Cell(1, 2), ThirdHorse),
            (Cell(1, 3), FirstHorse with { Id = new PieceId(11) })
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(1, 0), Cell(1, 1)));

        // Assert
        Assert.Empty(transition.Events.OfType<PieceCaptured>());
    }

    [Fact]
    public void GuideOf_WhenTheKindTakesByCustody_TellsSo()
    {
        // Arrange - nothing beyond the rules

        // Act
        PieceGuide guide = this._rules.GuideOf(Chaturaji2027Pieces.Elephant);

        // Assert
        Assert.Contains(guide.SpecialPowers, power => power.StartsWith("Takes by custody", StringComparison.Ordinal));
    }

    [Fact]
    public void GuideOf_WhenTheKindJumps_DrawsItLandingBeyondEachPieceItTakesAndJumpingOnAgain()
    {
        // Arrange - the Boat, from the centre, 40, of a diagram 9 squares across: steps diagonally; an enemy one and
        // three squares out along each diagonal

        // Act
        MoveDiagram diagram = this._rules.GuideOf(Chaturaji2027Pieces.Boat).Diagram;

        // Assert
        Assert.Equal(
            ("30,32,48,50", "10,16,30,32,48,50,64,70", "0,8,20,24,56,60,72,80"),
            (
                string.Join(",", diagram.Moves),
                string.Join(",", diagram.Captures),
                string.Join(",", diagram.Jumps)
            )
        );
    }

    private static CellId Cell(int column, int row) => Grid.CellAt(column, row);

    /// <summary>Black to move, the die rolled, its Raja on h1 and Yellow's on h8, with the pieces placed.</summary>
    private static GameState BlackToMove(int die, params (CellId Cell, Piece Piece)[] placements) =>
        new Chaturaji2027Rules().CreateInitialState() with
        {
            TurnNumber = 2,
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Dice = [die],
            Position = placements.Aggregate(
                Position.Empty.Place(Cell(7, 0), BlackRaja).Place(Cell(7, 7), YellowRaja),
                (position, placed) => position.Place(placed.Cell, placed.Piece)
            ),
        };
}
