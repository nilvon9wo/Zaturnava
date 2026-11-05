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
/// Start-row powers, given here to Chaturaji 2027's Pawns: Black's face up the board, from row 0; Yellow's pieces
/// face right; Red's, left; Green is Black's ally.
/// </summary>
public sealed class StartRowMovesTest
{
    private const int PawnDie = 5;
    private const int ElephantDie = 3;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);
    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

    private static readonly Piece BlackPawn = new(new PieceId(1), Chaturaji2027Pieces.Pawn, Black);
    private static readonly Piece YellowElephant = new(new PieceId(2), Chaturaji2027Pieces.Elephant, Yellow);
    private static readonly Piece RedElephant = new(new PieceId(3), Chaturaji2027Pieces.Elephant, Red);
    private static readonly Piece GreenElephant = new(new PieceId(4), Chaturaji2027Pieces.Elephant, Green);

    private readonly Chaturaji2027Rules _rules = RulesWith(2);

    [Fact]
    public void LegalActions_WhenAPawnWithPowersStandsWhereItStarted_MayStepTwoAhead()
    {
        // Arrange
        GameState state = this.BlackToMove(Cell(3, 1), PawnDie, (Cell(3, 1), BlackPawn));

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Contains(new MoveAction(Black, Cell(3, 1), Cell(3, 3)), actions);
    }

    [Fact]
    public void LegalActions_WhenTheGameWasNotDealt_LetsAPawnStepTwoFromWhereTheRulesStartIt()
    {
        // Arrange - Chaturaji 2027's own start, Black's Pawns on its second row
        GameState state = this._rules.CreateInitialState() with
        {
            TurnNumber = 2,
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Dice = [PawnDie],
        };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Null(state.Start);
        Assert.Contains(new MoveAction(Black, Cell(1, 1), Cell(1, 3)), actions);
    }

    public static TheoryData<string, int, int, int, int> NoDoubleSteps =>
        new()
        {
            { "a piece just ahead", 1, 1, 3, 2 },
            { "a piece two ahead", 1, 1, 3, 3 },
            { "moved on from where it started", 1, 2, 0, 7 },
            { "started beyond the start rows", 2, 2, 0, 7 },
        };

    [Theory]
    [MemberData(nameof(NoDoubleSteps), DisableDiscoveryEnumeration = true)]
    public void LegalActions_WhenTheWayIsBlockedOrThePawnHasMovedOrStartedTooFar_OffersNoDoubleStep(
        string why,
        int startRow,
        int row,
        int blockerColumn,
        int blockerRow
    )
    {
        // Arrange
        GameState state = this.BlackToMove(
            Cell(3, startRow),
            PawnDie,
            (Cell(3, row), BlackPawn),
            (Cell(blockerColumn, blockerRow), YellowElephant)
        );

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Cell(3, row), Cell(3, row + 2)), actions);
        Assert.NotEmpty(why);
    }

    [Fact]
    public void LegalActions_WhenAPawnStartedNowhereTheGameKnows_OffersNoDoubleStep()
    {
        // Arrange - the start has no piece of the pawn's
        GameState state = this.BlackToMove(Cell(3, 1), PawnDie, (Cell(3, 1), BlackPawn)) with
        {
            Start = Position.Empty,
        };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Cell(3, 1), Cell(3, 3)), actions);
    }

    [Fact]
    public void LegalActions_WhenAPawnStartedOnTheFarEdge_HasNoRoomForADoubleStep()
    {
        // Arrange - every row a start row
        Chaturaji2027Rules rules = RulesWith(Chaturaji2027Setup.Side);
        GameState state = this.BlackToMove(Cell(3, 7), PawnDie, (Cell(3, 7), BlackPawn));

        // Act
        ImmutableList<GameAction> actions = rules.LegalActions(state);

        // Assert
        Assert.Empty(actions.OfType<MoveAction>());
    }

    [Fact]
    public void Apply_WhenAPieceGoesTwoOrMoreStraightAhead_MayBeTakenInPassing()
    {
        // Arrange - Yellow's Elephant, facing right, goes three; Black's Pawn's double step, from before, stands
        Passage black = new(BlackPawn, Cell(3, 1), [Cell(3, 2)], Cell(3, 3));
        GameState state = this.YellowToMove(
            (Cell(1, 4), YellowElephant),
            (Cell(3, 3), BlackPawn),
            (Cell(7, 7), GreenElephant),
            (Cell(7, 0), RedElephant)
        ) with
        {
            Passages = [black],
        };

        // Act
        GameState moved = this._rules.Apply(state, new MoveAction(Yellow, Cell(1, 4), Cell(4, 4))).State;

        // Assert
        Assert.Equal(black, moved.Passages[0]);
        Passage passage = moved.Passages[1];
        Assert.Equal(
            (YellowElephant, Cell(1, 4), Cell(4, 4)),
            (passage.Piece, passage.From, passage.To)
        );
        Assert.Equal([Cell(2, 4), Cell(3, 4)], passage.Passed);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(1, 5)]
    public void Apply_WhenAPieceGoesOneAheadOrNotStraightAhead_LeavesNothingToTakeInPassing(int column, int row)
    {
        // Arrange
        GameState state = this.YellowToMove((Cell(1, 4), YellowElephant), (Cell(3, 3), BlackPawn));

        // Act
        GameState moved = this._rules.Apply(state, new MoveAction(Yellow, Cell(1, 4), Cell(column, row))).State;

        // Assert
        Assert.Empty(moved.Passages);
    }

    [Fact]
    public void LegalActions_WhenAnEnemyPassedFromTwoAway_MayTakeItInPassingFromEachCellItPassed()
    {
        // Arrange - Yellow's Elephant went from b5 to f5, over c5, d5 and e5, past Black's Pawn on d4
        GameState state = this.Passed(YellowElephant, Cell(1, 4), Cell(5, 4));

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Contains(new MoveAction(Black, Cell(3, 3), Cell(2, 4)), actions);
        Assert.Contains(new MoveAction(Black, Cell(3, 3), Cell(4, 4)), actions);
    }

    [Fact]
    public void Apply_WhenAPawnTakesInPassing_TakesThePieceWhereItStopped()
    {
        // Arrange
        GameState state = this.Passed(YellowElephant, Cell(1, 4), Cell(5, 4));

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(3, 3), Cell(2, 4)));

        // Assert
        Assert.Equal(BlackPawn, transition.State.Position.PieceAt(Cell(2, 4)));
        Assert.Null(transition.State.Position.PieceAt(Cell(5, 4)));
        Assert.Contains(new PieceCaptured(YellowElephant, Black, 4), transition.Events);
        Assert.Empty(transition.State.Passages);
    }

    [Fact]
    public void Apply_WhenAPawnTakesAPieceThatPassedWhereItStands_TakesItThereAndNoMoreInPassing()
    {
        // Arrange - Yellow's Elephant went from b5 to e5, past Black's Pawn on d4
        GameState state = this.Passed(YellowElephant, Cell(1, 4), Cell(4, 4));

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Cell(3, 3), Cell(4, 4)));

        // Assert
        Assert.Equal(BlackPawn, transition.State.Position.PieceAt(Cell(4, 4)));
        Assert.Empty(transition.State.Passages);
    }

    public static TheoryData<string, Piece, int, int, int, int> NoPassings =>
        new()
        {
            { "an ally", GreenElephant, 4, 5, 4, 2 },
            { "from three away", YellowElephant, 0, 4, 5, 4 },
            { "passing no cell the pawn takes on", RedElephant, 5, 2, 2, 2 },
        };

    [Theory]
    [MemberData(nameof(NoPassings), DisableDiscoveryEnumeration = true)]
    public void LegalActions_WhenThePassingIsNoEnemyFromTwoAwayPastThePawn_OffersNoTakingInPassing(
        string why,
        Piece piece,
        int fromColumn,
        int fromRow,
        int toColumn,
        int toRow
    )
    {
        // Arrange
        GameState state = this.Passed(piece, Cell(fromColumn, fromRow), Cell(toColumn, toRow));

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Cell(3, 3), Cell(2, 4)), actions);
        Assert.DoesNotContain(new MoveAction(Black, Cell(3, 3), Cell(4, 4)), actions);
        Assert.NotEmpty(why);
    }

    [Fact]
    public void LegalActions_WhenTheCellPassedIsTakenSince_OffersNoTakingInPassingOntoIt()
    {
        // Arrange - Green's Elephant has come onto c5 since
        GameState passed = this.Passed(YellowElephant, Cell(1, 4), Cell(4, 4));
        GameState state = passed with { Position = passed.Position.Place(Cell(2, 4), GreenElephant) };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Cell(3, 3), Cell(2, 4)), actions);
    }

    [Fact]
    public void Apply_WhenTheTurnComesToTheSeatThatPassed_NoLongerLetsItsPieceBeTakenInPassing()
    {
        // Arrange - Yellow's passing and Red's both stand as Black passes the turn to Yellow
        GameState passed = this.Passed(YellowElephant, Cell(1, 4), Cell(4, 4));
        Passage red = new(RedElephant, Cell(7, 6), [Cell(6, 6)], Cell(5, 6));
        GameState state = passed with
        {
            Position = passed.Position.Place(Cell(5, 6), RedElephant),
            Passages = passed.Passages.Add(red),
        };

        // Act
        GameState handed = this._rules.Apply(state, new PassAction(Black)).State;

        // Assert
        Assert.Equal(Yellow, handed.DecidingSeat);
        Assert.Equal([red], handed.Passages);
    }

    private static Chaturaji2027Rules RulesWith(int rows) =>
        new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Chaturaji2027Variant.Standard with
            {
                StartRowPowers = [Chaturaji2027Pieces.Pawn],
                StartRows = rows,
            }
        );

    private static CellId Cell(int column, int row) => Grid.CellAt(column, row);

    /// <summary>Black to move with <paramref name="die"/>, its Pawn started on <paramref name="start"/>.</summary>
    private GameState BlackToMove(CellId start, int die, params (CellId Cell, Piece Piece)[] placements) =>
        this._rules.CreateInitialState() with
        {
            TurnNumber = 2,
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Dice = [die],
            Position = placements.Aggregate(
                Position.Empty,
                (position, placed) => position.Place(placed.Cell, placed.Piece)
            ),
            Start = Position.Empty.Place(start, BlackPawn),
        };

    /// <summary>Yellow to move an Elephant.</summary>
    private GameState YellowToMove(params (CellId Cell, Piece Piece)[] placements) =>
        this.BlackToMove(Cell(3, 1), ElephantDie, placements) with { DecidingSeat = Yellow };

    /// <summary>Black to move its Pawn on d4, which started on d2, once <paramref name="piece"/> passed.</summary>
    private GameState Passed(Piece piece, CellId from, CellId to) =>
        this.BlackToMove(Cell(3, 1), PawnDie, (Cell(3, 3), BlackPawn), (to, piece)) with
        {
            Passages = [new Passage(piece, from, Between(from, to), to)],
        };

    /// <summary>The cells strictly between two on one row or column, nearest <paramref name="from"/> first.</summary>
    private static ImmutableList<CellId> Between(CellId from, CellId to)
    {
        int columns = Grid.ColumnOf(to) - Grid.ColumnOf(from);
        int rows = Grid.RowOf(to) - Grid.RowOf(from);
        return
        [
            .. Enumerable
                .Range(1, Math.Max(Math.Abs(columns), Math.Abs(rows)) - 1)
                .Select(step => Cell(
                        Grid.ColumnOf(from) + (step * Math.Sign(columns)),
                        Grid.RowOf(from) + (step * Math.Sign(rows))
                    )
                ),
        ];
    }
}
