using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Topology.SquareGrid.Test;

public sealed class SquareGridTopologyTest
{
    private static readonly Direction Up = new(0, 1);
    private static readonly Direction Right = new(1, 0);

    private readonly SquareGridTopology _grid = new(4, 3);

    [Theory]
    [InlineData(0)]
    [InlineData(SquareGridTopology.MaximumSide + 1)]
    public void Constructor_WhenTheWidthIsOutOfRange_ThrowsArgumentException(int width)
    {
        // Arrange - nothing beyond the arguments

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new SquareGridTopology(width, 3));

        // Assert
        Assert.Equal("width", exception.ParamName);
    }

    [Fact]
    public void Cells_WhenListed_CoverTheWholeGrid()
    {
        // Arrange - nothing beyond the grid

        // Act
        ImmutableList<CellId> cells = this._grid.Cells;

        // Assert
        Assert.Equal(Enumerable.Range(0, 12), cells.Select(cell => cell.Value));
    }

    [Fact]
    public void CellAt_WhenGivenAColumnAndRow_NumbersRowByRow()
    {
        // Arrange - nothing beyond the grid

        // Act
        CellId cell = this._grid.CellAt(1, 2);

        // Assert
        Assert.Equal(new CellId(9), cell);
    }

    [Fact]
    public void Step_WhenTheTargetIsOnTheBoard_ReturnsIt()
    {
        // Arrange
        CellId from = this._grid.CellAt(1, 1);

        // Act
        CellId? to = this._grid.Step(from, new Direction(2, 1));

        // Assert
        Assert.Equal(this._grid.CellAt(3, 2), to);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 1)]
    public void Step_WhenTheTargetIsOffAnEdge_ReturnsNull(int columns, int rows)
    {
        // Arrange
        CellId from = this._grid.CellAt(Math.Max(columns, 0) * 3, Math.Max(rows, 0) * 2);

        // Act
        CellId? to = this._grid.Step(from, new Direction(columns, rows));

        // Assert
        Assert.Null(to);
    }

    [Theory]
    [InlineData("orthogonal", 4)]
    [InlineData("diagonal", 4)]
    [InlineData("knight", 8)]
    [InlineData("forward", 1)]
    [InlineData("any", 8)]
    [InlineData("leaper-3-2", 8)]
    [InlineData("leaper-2-2", 4)]
    [InlineData("leaper-3-0", 4)]
    public void Resolve_WhenTheSetIsSupported_ReturnsItsDirections(string name, int count)
    {
        // Arrange
        DirectionSet set = new(name);

        // Act
        ImmutableList<Direction> directions = this._grid.Resolve(set, Up);

        // Assert
        Assert.Equal(count, directions.Distinct().Count());
    }

    [Fact]
    public void Resolve_WhenForwardDiagonalsFaceUp_ReturnsUpLeftAndUpRight()
    {
        // Arrange - nothing beyond the grid

        // Act
        ImmutableList<Direction> directions = this._grid.Resolve(DirectionSets.ForwardDiagonals, Up);

        // Assert
        Assert.Equal([new Direction(-1, 1), new Direction(1, 1)], directions);
    }

    [Fact]
    public void Resolve_WhenForwardDiagonalsFaceRight_ReturnsUpRightAndDownRight()
    {
        // Arrange - nothing beyond the grid

        // Act
        ImmutableList<Direction> directions = this._grid.Resolve(DirectionSets.ForwardDiagonals, Right);

        // Assert
        Assert.Equal([new Direction(1, 1), new Direction(1, -1)], directions);
    }

    [Fact]
    public void Resolve_WhenSidewaysFacesUp_ReturnsLeftAndRight()
    {
        // Arrange - nothing beyond the grid

        // Act
        ImmutableList<Direction> directions = this._grid.Resolve(DirectionSets.Sideways, Up);

        // Assert
        Assert.Equal([new Direction(-1, 0), new Direction(1, 0)], directions);
    }

    [Fact]
    public void Resolve_WhenALeaperIsNamed_ReturnsEachOfItsLeaps()
    {
        // Arrange - nothing beyond the grid

        // Act
        ImmutableList<Direction> directions = this._grid.Resolve(DirectionSets.Leaper(2, 1), Up);

        // Assert
        Assert.Equal(
            this._grid.Resolve(DirectionSets.Knight, Up).OrderBy(Key),
            directions.OrderBy(Key)
        );
    }

    [Theory]
    [InlineData("hexagonal")]
    [InlineData("leaper-3")]
    [InlineData("leaper-x-2")]
    [InlineData("leaper-3-x")]
    public void Resolve_WhenTheSetIsUnknown_ThrowsUnsupportedDirectionSetException(string name)
    {
        // Arrange
        DirectionSet set = new(name);

        // Act
        UnsupportedDirectionSetException exception = Assert.Throws<UnsupportedDirectionSetException>(
            () => this._grid.Resolve(set, Up)
        );

        // Assert
        Assert.Equal($"The square grid topology has no '{name}' directions.", exception.Message);
    }

    [Theory]
    [InlineData(1, 0, "")]
    [InlineData(1, 1, "")]
    [InlineData(2, 1, "1,0")]
    [InlineData(-1, -2, "0,-1")]
    [InlineData(2, 2, "1,1")]
    [InlineData(3, -2, "1,0 2,-1")]
    [InlineData(0, 3, "0,1 0,2")]
    public void WaypointsOf_WhenALeapIsLame_PassesStraightThenDiagonally(int columns, int rows, string expected)
    {
        // Arrange - nothing beyond the grid

        // Act
        ImmutableList<Direction> waypoints = this._grid.WaypointsOf(new Direction(columns, rows));

        // Assert
        Assert.Equal(expected, string.Join(" ", waypoints.Select(step => $"{step.First},{step.Second}")));
    }

    [Fact]
    public void LayoutOf_WhenCalled_PlacesTheCellAtItsColumnAndRow()
    {
        // Arrange
        CellId cell = this._grid.CellAt(3, 1);

        // Act
        CellLayout layout = this._grid.LayoutOf(cell);

        // Assert
        Assert.Equal(new CellLayout(3, 1, "square"), layout);
    }

    [Theory]
    [InlineData(0, 0, "a1")]
    [InlineData(7, 7, "h8")]
    [InlineData(25, 1, "z2")]
    [InlineData(26, 0, "aa1")]
    [InlineData(53, 9, "bb10")]
    public void NameOf_WhenGivenACell_NamesItsColumnByLettersAndItsRowByNumber(int column, int row, string expected)
    {
        // Arrange
        SquareGridTopology grid = new(60, 10);

        // Act
        string name = grid.NameOf(grid.CellAt(column, row));

        // Assert
        Assert.Equal(expected, name);
    }

    private static (int, int) Key(Direction direction) => (direction.First, direction.Second);
}
