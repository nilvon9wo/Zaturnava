using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
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
    public void LegalActions_WhenTheGameStarts_OffersAPassAndNineMoves()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal((1, 9), (actions.OfType<PassAction>().Count(), actions.OfType<MoveAction>().Count()));
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
        GameState state = this._rules.CreateInitialState();

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        Assert.Equal((Yellow, 2), (transition.State.DecidingSeat, transition.State.TurnNumber));
        Assert.Equal([new TurnPassed(Black, Yellow)], transition.Events);
    }

    [Fact]
    public void Apply_WhenTheLastSeatPasses_HandsTheTurnBackToTheFirst()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState() with { DecidingSeat = Red };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Red));

        // Assert
        Assert.Equal(Black, transition.State.DecidingSeat);
    }

    [Fact]
    public void Apply_WhenMovingToAnEmptySquare_MovesThePieceAndScoresNothing()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();
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

    private GameState StateWith(params (CellId Cell, Piece Piece)[] placements) =>
        this._rules.CreateInitialState() with
        {
            Position = placements.Aggregate(Position.Empty, Placed),
        };
}
