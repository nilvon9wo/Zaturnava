using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Moves;

public sealed class MoveGeneratorTest
{
    private const int BoardLength = 6;
    private static readonly SeatId MySeatId = new(0);
    private static readonly SeatId Theirs = new(1);
    private static readonly Seat MySeat = new(MySeatId, LineTopology.Right);
    private static readonly PieceKindId Stepper = new("stepper");
    private static readonly PieceKindId Slider = new("slider");
    private static readonly PieceKindId Leaper = new("leaper");
    private static readonly PieceKindId Walker = new("walker");
    private static readonly PieceKindId Striker = new("striker");
    private static readonly PieceKindId Wanderer = new("wanderer");

    private readonly MoveGenerator _generator = new(
        new LineTopology(BoardLength),
        ImmutableDictionary<PieceKindId, PieceKind>.Empty
            .Add(Stepper, Kind(Stepper, MovementRule.Step(DirectionSets.Orthogonal)))
            .Add(Slider, Kind(Slider, MovementRule.Slide(DirectionSets.Orthogonal)))
            .Add(Leaper, Kind(Leaper, MovementRule.Leap(DirectionSets.Orthogonal, 2)))
            .Add(Walker, Kind(Walker, MovementRule.Step(DirectionSets.Forward).MoveOnly()))
            .Add(Striker, Kind(Striker, MovementRule.Step(DirectionSets.Forward).CaptureOnly()))
            .Add(Wanderer, Kind(Wanderer, MovementRule.Step(DirectionSets.Diagonal)))
    );

    [Fact]
    public void MovesFrom_WhenNoPieceIsThere_ThrowsArgumentException()
    {
        // Arrange - nothing beyond the empty position

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => this._generator.MovesFrom(Position.Empty, new CellId(0), MySeat, IsEnemy)
        );

        // Assert
        Assert.Equal("from", exception.ParamName);
    }

    [Fact]
    public void MovesFrom_WhenAStepperIsMidBoard_StepsBothWays()
    {
        // Arrange
        Position position = Position.Empty.Place(new CellId(2), Friend(Stepper));

        // Act
        ImmutableList<Move> moves = this._generator.MovesFrom(position, new CellId(2), MySeat, IsEnemy);

        // Assert
        Assert.Equal([3, 1], Targets(moves));
    }

    [Fact]
    public void MovesFrom_WhenAStepperIsOnTheEdge_StopsAtTheEdge()
    {
        // Arrange
        Position position = Position.Empty.Place(new CellId(0), Friend(Stepper));

        // Act
        ImmutableList<Move> moves = this._generator.MovesFrom(position, new CellId(0), MySeat, IsEnemy);

        // Assert
        Assert.Equal([1], Targets(moves));
    }

    [Fact]
    public void MovesFrom_WhenASliderMeetsAnEnemy_CapturesItAndGoesNoFurther()
    {
        // Arrange
        Position position = Position.Empty
            .Place(new CellId(0), Friend(Slider))
            .Place(new CellId(3), Enemy(Stepper));

        // Act
        ImmutableList<Move> moves = this._generator.MovesFrom(position, new CellId(0), MySeat, IsEnemy);

        // Assert
        Assert.Equal([1, 2, 3], Targets(moves));
    }

    [Fact]
    public void MovesFrom_WhenASliderMeetsAFriend_StopsShortOfIt()
    {
        // Arrange
        Position position = Position.Empty
            .Place(new CellId(0), Friend(Slider))
            .Place(new CellId(3), Friend(Stepper));

        // Act
        ImmutableList<Move> moves = this._generator.MovesFrom(position, new CellId(0), MySeat, IsEnemy);

        // Assert
        Assert.Equal([1, 2], Targets(moves));
    }

    [Fact]
    public void MovesFrom_WhenALeaperIsBlocked_LeapsOverTheBlocker()
    {
        // Arrange
        Position position = Position.Empty
            .Place(new CellId(2), Friend(Leaper))
            .Place(new CellId(3), Friend(Stepper));

        // Act
        ImmutableList<Move> moves = this._generator.MovesFrom(position, new CellId(2), MySeat, IsEnemy);

        // Assert
        Assert.Equal([4, 0], Targets(moves));
    }

    [Fact]
    public void MovesFrom_WhenAMoveOnlyPieceFacesAnEnemy_CannotCaptureIt()
    {
        // Arrange
        Position position = Position.Empty
            .Place(new CellId(0), Friend(Walker))
            .Place(new CellId(1), Enemy(Stepper));

        // Act
        ImmutableList<Move> moves = this._generator.MovesFrom(position, new CellId(0), MySeat, IsEnemy);

        // Assert
        Assert.Empty(moves);
    }

    [Fact]
    public void MovesFrom_WhenACaptureOnlyPieceFacesAnEnemy_CapturesIt()
    {
        // Arrange
        Piece enemy = Enemy(Stepper);
        Position position = Position.Empty
            .Place(new CellId(0), Friend(Striker))
            .Place(new CellId(1), enemy);

        // Act
        ImmutableList<Move> moves = this._generator.MovesFrom(position, new CellId(0), MySeat, IsEnemy);

        // Assert
        Assert.Equal(enemy, Assert.Single(moves).Captured);
    }

    [Fact]
    public void MovesFrom_WhenACaptureOnlyPieceFacesAnEmptyCell_CannotMove()
    {
        // Arrange
        Position position = Position.Empty.Place(new CellId(0), Friend(Striker));

        // Act
        ImmutableList<Move> moves = this._generator.MovesFrom(position, new CellId(0), MySeat, IsEnemy);

        // Assert
        Assert.Empty(moves);
    }

    [Fact]
    public void MovesFrom_WhenTheTopologyLacksTheDirections_ThrowsUnsupportedDirectionSetException()
    {
        // Arrange
        Position position = Position.Empty.Place(new CellId(0), Friend(Wanderer));

        // Act
        UnsupportedDirectionSetException exception = Assert.Throws<UnsupportedDirectionSetException>(
            () => this._generator.MovesFrom(position, new CellId(0), MySeat, IsEnemy)
        );

        // Assert
        Assert.Equal("The line topology has no 'diagonal' directions.", exception.Message);
    }

    [Fact]
    public void AttacksFrom_WhenNoPieceIsThere_ThrowsArgumentException()
    {
        // Arrange - nothing beyond the empty position

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => this._generator.AttacksFrom(Position.Empty, new CellId(0), MySeat)
        );

        // Assert
        Assert.Equal("from", exception.ParamName);
    }

    [Fact]
    public void AttacksFrom_WhenASliderMeetsAFriend_GuardsItAndGoesNoFurther()
    {
        // Arrange
        Piece slider = Friend(Slider);
        Position position = Position.Empty
            .Place(new CellId(2), slider)
            .Place(new CellId(4), Friend(Stepper));

        // Act
        ImmutableList<Attack> attacks = this._generator.AttacksFrom(position, new CellId(2), MySeat);

        // Assert
        Assert.Equal(
            [(slider, 2, 3), (slider, 2, 4), (slider, 2, 1), (slider, 2, 0)],
            attacks.Select(attack => (attack.Attacker, attack.From.Value, attack.Target.Value))
        );
    }

    [Fact]
    public void AttacksFrom_WhenAPieceOnlyMoves_AttacksNothing()
    {
        // Arrange
        Position position = Position.Empty.Place(new CellId(0), Friend(Walker));

        // Act
        ImmutableList<Attack> attacks = this._generator.AttacksFrom(position, new CellId(0), MySeat);

        // Assert
        Assert.Empty(attacks);
    }

    [Fact]
    public void AttacksFrom_WhenAPieceOnlyCaptures_AttacksTheEmptyCellItWouldCaptureOn()
    {
        // Arrange
        Position position = Position.Empty.Place(new CellId(0), Friend(Striker));

        // Act
        ImmutableList<Attack> attacks = this._generator.AttacksFrom(position, new CellId(0), MySeat);

        // Assert
        Assert.Equal(1, Assert.Single(attacks).Target.Value);
    }

    private static PieceKind Kind(PieceKindId id, MovementRule rule) => new(id, [rule]);

    private static Piece Friend(PieceKindId kind) => new(new PieceId(1), kind, MySeatId);

    private static Piece Enemy(PieceKindId kind) => new(new PieceId(2), kind, Theirs);

    private static bool IsEnemy(Piece piece) => piece.Owner != MySeatId;

    private static int[] Targets(ImmutableList<Move> moves) => [.. moves.Select(move => move.To.Value)];
}
