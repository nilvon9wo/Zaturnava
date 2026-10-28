using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

/// <summary>
/// A <paramref name="width"/> by <paramref name="height"/> grid of square cells. A direction is
/// (columns, rows); cell (column, row) has id <c>row * width + column</c>, row 0 at the bottom.
/// </summary>
public sealed class SquareGridTopology(
    [Range(1, SquareGridTopology.MaximumSide)] int width,
    [Range(1, SquareGridTopology.MaximumSide)] int height
) : ICellNavigating
{
    public const int MaximumSide = 1024;
    public const string TopologyName = "square grid";
    public const string CellShape = "square";

    private const int Letters = 26;

    private static readonly ImmutableList<Direction> OrthogonalDirections =
        [new(1, 0), new(0, 1), new(-1, 0), new(0, -1)];
    private static readonly ImmutableList<Direction> DiagonalDirections =
        [new(1, 1), new(-1, 1), new(-1, -1), new(1, -1)];
    private static readonly ImmutableList<Direction> KnightDirections =
    [
        new(1, 2),
        new(2, 1),
        new(2, -1),
        new(1, -2),
        new(-1, -2),
        new(-2, -1),
        new(-2, 1),
        new(-1, 2),
    ];

    private readonly int _width = width;
    private readonly int _height = height;

    public ImmutableList<CellId> Cells => [.. Enumerable.Range(0, this._width * this._height).Select(Cell)];

    public CellId CellAt(int column, int row) => new((row * this._width) + column);

    public CellId? Step(CellId from, Direction direction)
    {
        int column = this.ColumnOf(from) + direction.First;
        int row = this.RowOf(from) + direction.Second;
        return this.IsOnBoard(column, row)
            ? this.CellAt(column, row)
            : null;
    }

    public ImmutableList<Direction> Resolve([Required] DirectionSet set, Direction forward) =>
        set.Name switch
        {
            "orthogonal" => OrthogonalDirections,
            "diagonal" => DiagonalDirections,
            "knight" => KnightDirections,
            "forward" => [forward],
            "forward-diagonals" => ForwardDiagonals(forward),
            _ => throw new UnsupportedDirectionSetException(set, TopologyName),
        };

    public CellLayout LayoutOf(CellId cell) => new(this.ColumnOf(cell), this.RowOf(cell), CellShape);

    /// <summary>
    /// The column's letters and the row's number, counted from the bottom left: "a1", "h8"; after "z"
    /// the columns go on "aa", "ab"...
    /// </summary>
    public string NameOf(CellId cell) =>
        string.Create(CultureInfo.InvariantCulture, $"{ColumnName(this.ColumnOf(cell))}{this.RowOf(cell) + 1}");

    public int ColumnOf(CellId cell) => cell.Value % this._width;

    public int RowOf(CellId cell) => cell.Value / this._width;

    private static CellId Cell(int index) => new(index);

    private static string ColumnName(int column) =>
        column < Letters
            ? $"{(char)('a' + column)}"
            : ColumnName((column / Letters) - 1) + ColumnName(column % Letters);

    private static ImmutableList<Direction> ForwardDiagonals(Direction forward)
    {
        Direction left = new(-forward.Second, forward.First);
        Direction right = new(forward.Second, -forward.First);
        return [Combine(forward, left), Combine(forward, right)];
    }

    private static Direction Combine(Direction first, Direction second) =>
        new(first.First + second.First, first.Second + second.Second);

    private bool IsOnBoard(int column, int row) =>
        column >= 0 && column < this._width && row >= 0 && row < this._height;
}
