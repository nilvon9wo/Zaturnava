using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic.Test;

public sealed class ClassicRulesTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);
    private static readonly SquareGridTopology Grid = new(ClassicSetup.Side, ClassicSetup.Side);

    private readonly ClassicRules _rules = new();

    [Fact]
    public void Identity_WhenRead_IsTheClassicSquareGridRuleset()
    {
        // Arrange - nothing beyond the rules

        // Act
        (string Id, string Name, CellLayout Corner) identity = (
            this._rules.Id,
            this._rules.Name,
            this._rules.Topology.LayoutOf(new CellId(63))
        );

        // Assert
        Assert.Equal(("classic", "Zaturnava (classic)", new CellLayout(7, 7, "square")), identity);
    }

    [Fact]
    public void CreateInitialState_WhenCalled_StartsWithBlackToDecideAndNoScores()
    {
        // Arrange - nothing beyond the rules

        // Act
        GameState state = this._rules.CreateInitialState();

        // Assert
        Assert.Equal(
            (Black, 1, 0, 0),
            (state.DecidingSeat, state.TurnNumber, state.Scores.Values.Sum(), state.Prisoners.Count)
        );
    }

    [Fact]
    public void LegalActions_WhenTheGameOpens_OffersOnlyARoll()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal([new RollAction(Black)], actions);
    }

    [Fact]
    public void LegalActions_WhenALaterTurnStarts_OffersAPassARollAndNineMoves()
    {
        // Arrange
        GameState state = this.Later();

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            (1, 1, 9),
            (
                actions.OfType<PassAction>().Count(),
                actions.OfType<RollAction>().Count(),
                actions.OfType<MoveAction>().Count()
            )
        );
    }

    [Fact]
    public void LegalActions_WhenTheGameWaitsOnDice_IsEmpty()
    {
        // Arrange
        GameState state = this.Later() with { AwaitedDice = new DiceRequest(2, 6) };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Empty(actions);
    }

    [Fact]
    public void LegalActions_WhenDiceAreRolled_OffersAPassAndMovesOfTheirKindsOnly()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [1] };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            [new PassAction(Black), new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(2, 2))],
            actions
        );
    }

    [Fact]
    public void LegalActions_WhenNoSeatIsDeciding_IsEmpty()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState() with { DecidingSeat = null };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Empty(actions);
    }

    [Fact]
    public void LegalActions_WhenAPawnHasAnEnemyAndAnAllyDiagonallyAhead_CapturesOnlyTheEnemy()
    {
        // Arrange
        GameState state = this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, ClassicPieces.Pawn, Black)),
            (Grid.CellAt(2, 4), Piece(2, ClassicPieces.Pawn, Yellow)),
            (Grid.CellAt(4, 4), Piece(3, ClassicPieces.Pawn, Green))
        );

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            [
                new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4)),
                new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(2, 4)),
            ],
            actions.OfType<MoveAction>().OrderBy(move => move.To.Value).Reverse()
        );
    }

    [Fact]
    public void Apply_WhenTheActionIsNotLegal_ThrowsIllegalActionException()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();
        PassAction outOfTurn = new(Yellow);

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, outOfTurn)
        );

        // Assert
        Assert.Contains("is not legal now", exception.Message);
    }

    [Fact]
    public void Apply_WhenPassing_HandsTheTurnClockwise()
    {
        // Arrange
        GameState state = this.Later();

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        Assert.Equal((Yellow, 3), (transition.State.DecidingSeat, transition.State.TurnNumber));
        Assert.Equal([new TurnPassed(Black, Yellow)], transition.Events);
    }

    [Fact]
    public void Apply_WhenTheLastSeatPasses_HandsTheTurnBackToTheFirst()
    {
        // Arrange
        GameState state = this.Later() with { DecidingSeat = Red };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Red));

        // Assert
        Assert.Equal(Black, transition.State.DecidingSeat);
    }

    [Fact]
    public void Apply_WhenMovingToAnEmptySquare_MovesThePieceAndScoresNothing()
    {
        // Arrange
        GameState state = this.Later();
        MoveAction pawnForward = new(Black, Grid.CellAt(0, 1), Grid.CellAt(0, 2));
        Piece pawn = state.Position.PieceAt(pawnForward.From)!;

        // Act
        Transition transition = this._rules.Apply(state, pawnForward);

        // Assert
        Assert.Equal(
            [new PieceMoved(pawn, pawnForward.From, pawnForward.To), new TurnPassed(Black, Yellow)],
            transition.Events
        );
    }

    [Fact]
    public void Apply_WhenCapturing_ScoresThePieceAndTakesItPrisoner()
    {
        // Arrange
        Piece elephant = Piece(1, ClassicPieces.Elephant, Black);
        Piece horse = Piece(2, ClassicPieces.Horse, Yellow);
        GameState state = this.StateWith((Grid.CellAt(0, 0), elephant), (Grid.CellAt(0, 5), horse));
        MoveAction capture = new(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5));

        // Act
        Transition transition = this._rules.Apply(state, capture);

        // Assert
        Assert.Equal((3, new PieceCaptured(horse, Black, 3)), (transition.State.Scores[Black], transition.Events[1]));
        Assert.Equal([horse], transition.State.Prisoners);
    }

    [Fact]
    public void Apply_WhenRollingOnALaterTurn_WaitsOnTwoDiceWithoutPassingTheTurn()
    {
        // Arrange
        GameState state = this.Later();

        // Act
        Transition transition = this._rules.Apply(state, new RollAction(Black));

        // Assert
        Assert.Equal(
            (new DiceRequest(2, 6), Black, 0),
            (transition.State.AwaitedDice, transition.State.DecidingSeat, transition.Events.Count)
        );
    }

    [Fact]
    public void Apply_WhenRollingTheOpeningTurn_WaitsOnOneDie()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();

        // Act
        Transition transition = this._rules.Apply(state, new RollAction(Black));

        // Assert
        Assert.Equal(new DiceRequest(1, 6), transition.State.AwaitedDice);
    }

    [Fact]
    public void Apply_WhenRollingFourSidedDice_WaitsOnFourSidedDice()
    {
        // Arrange
        ClassicRules rules = new(DiceProfile.FourSided);
        GameState state = rules.CreateInitialState();

        // Act
        Transition transition = rules.Apply(state, new RollAction(Black));

        // Assert
        Assert.Equal(new DiceRequest(1, 4), transition.State.AwaitedDice);
    }

    [Fact]
    public void Apply_WhenTheDiceComeUpUsable_KeepsThemForTheRoller()
    {
        // Arrange
        GameState state = this.Later() with { AwaitedDice = new DiceRequest(2, 6) };

        // Act
        Transition transition = this._rules.Apply(state, new DiceAction(Black, [5, 1]));

        // Assert
        Assert.Equal(
            ("5,1", Black, null),
            (string.Join(',', transition.State.Dice), transition.State.DecidingSeat, transition.State.AwaitedDice)
        );
        Assert.Equal([new DiceRolled(Black, [5, 1])], transition.Events);
    }

    [Fact]
    public void Apply_WhenNoPieceCanUseTheDice_ForfeitsThemAndPassesTheTurn()
    {
        // Arrange - the opening die shows an elephant, still hemmed in by its own pawn
        GameState state = this._rules.CreateInitialState() with { AwaitedDice = new DiceRequest(1, 6) };

        // Act
        Transition transition = this._rules.Apply(state, new DiceAction(Black, [3]));

        // Assert
        Assert.Equal((Yellow, 0), (transition.State.DecidingSeat, transition.State.Dice.Count));
        Assert.Equal([new DiceRolled(Black, [3]), new TurnPassed(Black, Yellow)], transition.Events);
    }

    [Theory]
    [MemberData(nameof(UnawaitedDice), DisableDiscoveryEnumeration = true)]
    public void Apply_WhenTheDiceAreNotTheOnesAwaited_ThrowsIllegalActionException(
        DiceRequest? awaited,
        DiceAction dice
    )
    {
        // Arrange
        GameState state = this.Later() with { AwaitedDice = awaited };

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, dice)
        );

        // Assert
        Assert.Contains("is not legal now", exception.Message);
    }

    [Fact]
    public void Apply_WhenTheGameIsOver_RefusesDice()
    {
        // Arrange
        GameState state = this.Later() with { AwaitedDice = new DiceRequest(2, 6), DecidingSeat = null };

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, new DiceAction(Black, [5, 1]))
        );

        // Assert
        Assert.Contains("is not legal now", exception.Message);
    }

    public static TheoryData<DiceRequest?, DiceAction> UnawaitedDice =>
        new()
        {
            { null, new DiceAction(Black, [5, 1]) },
            { new DiceRequest(2, 6), new DiceAction(Yellow, [5, 1]) },
            { new DiceRequest(2, 6), new DiceAction(Black, [5]) },
            { new DiceRequest(2, 6), new DiceAction(Black, [0, 1]) },
            { new DiceRequest(2, 6), new DiceAction(Black, [5, 7]) },
        };

    [Fact]
    public void Apply_WhenMovingWithOneOfTwoDice_UsesItUpAndKeepsTheTurn()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [5, 1] };
        MoveAction pawnForward = new(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4));

        // Act
        Transition transition = this._rules.Apply(state, pawnForward);

        // Assert
        Assert.Equal(("1", Black), (string.Join(',', transition.State.Dice), transition.State.DecidingSeat));
    }

    [Fact]
    public void Apply_WhenTheLastDieIsUsed_PassesTheTurn()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [1] };
        MoveAction boatLeap = new(Black, Grid.CellAt(0, 0), Grid.CellAt(2, 2));

        // Act
        Transition transition = this._rules.Apply(state, boatLeap);

        // Assert
        Assert.Equal((Yellow, 0), (transition.State.DecidingSeat, transition.State.Dice.Count));
    }

    [Fact]
    public void Apply_WhenAPieceNoDieShowsIsMoved_ThrowsIllegalActionException()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [1] };
        MoveAction pawnForward = new(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4));

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, pawnForward)
        );

        // Assert
        Assert.Contains("is not legal now", exception.Message);
    }

    [Fact]
    public void Apply_WhenDoublesAreRolled_LetsTheSamePieceMoveTwice()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [5, 5] };
        MoveAction first = new(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4));
        GameState once = this._rules.Apply(state, first).State;
        MoveAction second = new(Black, Grid.CellAt(3, 4), Grid.CellAt(3, 5));

        // Act
        Transition twice = this._rules.Apply(once, second);

        // Assert
        Assert.Equal(
            (ClassicPieces.Pawn, Yellow),
            (twice.State.Position.PieceAt(Grid.CellAt(3, 5))?.Kind, twice.State.DecidingSeat)
        );
    }

    [Fact]
    public void Apply_WhenPassingWithDiceLeft_ForfeitsThem()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [5, 1] };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        Assert.Equal((Yellow, 0), (transition.State.DecidingSeat, transition.State.Dice.Count));
    }

    [Fact]
    public void Parse_WhenGivenADescribedMove_ReturnsTheMove()
    {
        // Arrange
        MoveAction move = new(Black, Grid.CellAt(0, 1), Grid.CellAt(0, 2));

        // Act
        GameAction parsed = this._rules.Parse(move.Describe());

        // Assert
        Assert.Equal(move, parsed);
    }

    [Fact]
    public void NameOf_WhenGivenAPieceKind_ReturnsItsTraditionalName()
    {
        // Arrange - nothing beyond the rules

        // Act
        string name = this._rules.NameOf(ClassicPieces.Elephant);

        // Assert
        Assert.Equal("Elephant", name);
    }

    [Fact]
    public void NameOf_WhenGivenASeat_ReturnsItsColour()
    {
        // Arrange - nothing beyond the rules

        // Act
        string name = this._rules.NameOf(Green);

        // Assert
        Assert.Equal("Green", name);
    }

    private static Position Placed(Position position, (CellId Cell, Piece Piece) placed) =>
        position.Place(placed.Cell, placed.Piece);

    private static Piece Piece(int id, PieceKindId kind, SeatId owner) => new(new PieceId(id), kind, owner);

    /// <summary>The starting position on the second turn, past the opening roll.</summary>
    private GameState Later() => this._rules.CreateInitialState() with { TurnNumber = 2 };

    /// <summary>Black's pawn in mid-board and its boat in the corner, alone, on a later turn.</summary>
    private GameState PawnAndBoat() =>
        this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, ClassicPieces.Pawn, Black)),
            (Grid.CellAt(0, 0), Piece(2, ClassicPieces.Boat, Black))
        );

    private GameState StateWith(params (CellId Cell, Piece Piece)[] placements) =>
        this.Later() with
        {
            Position = placements.Aggregate(Position.Empty, Placed),
        };
}
