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
            "any" => [.. OrthogonalDirections, .. DiagonalDirections],
            "knight" => KnightDirections,
            "forward" => [forward],
            "forward-diagonals" => ForwardDiagonals(forward),
            "sideways" => [Left(forward), Right(forward)],
            _ when LeaperOf(set.Name) is { } leaper => Leaps(leaper.First, leaper.Second),
            _ => throw new UnsupportedDirectionSetException(set, TopologyName),
        };

    /// <summary>
    /// Straight along the longer side first, as far as it is longer than the other, then diagonally: the Xiangqi
    /// Horse's (2, 1) passes (1, 0); the Janggi Elephant's (3, 2) passes (1, 0) and (2, 1).
    /// </summary>
    public ImmutableList<Direction> WaypointsOf(Direction leap)
    {
        int columns = Math.Abs(leap.First);
        int rows = Math.Abs(leap.Second);
        Direction straight = columns >= rows ? new(Math.Sign(leap.First), 0) : new(0, Math.Sign(leap.Second));
        Direction diagonal = new(Math.Sign(leap.First), Math.Sign(leap.Second));
        int straightSteps = Math.Abs(columns - rows);
        ImmutableList<Direction> way =
        [
            .. Enumerable.Range(1, straightSteps).Select(step => Times(straight, step)),
            .. Enumerable.Range(1, Math.Min(columns, rows))
                .Select(step => Combine(Times(straight, straightSteps), Times(diagonal, step))),
        ];
        return [.. way.SkipLast(1)];
    }

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

    private static ImmutableList<Direction> ForwardDiagonals(Direction forward) =>
        [Combine(forward, Left(forward)), Combine(forward, Right(forward))];

    private static Direction Left(Direction forward) => new(-forward.Second, forward.First);

    private static Direction Right(Direction forward) => new(forward.Second, -forward.First);

    private static Direction Combine(Direction first, Direction second) =>
        new(first.First + second.First, first.Second + second.Second);

    private static Direction Times(Direction direction, int times) =>
        new(direction.First * times, direction.Second * times);

    /// <summary>The two distances a leaper set is named for ("leaper-3-2"); null for any other set.</summary>
    private static Direction? LeaperOf(string name)
    {
        string[] distances = name.StartsWith(DirectionSets.LeaperPrefix, StringComparison.Ordinal)
            ? name[DirectionSets.LeaperPrefix.Length..].Split('-')
            : [];
        return distances.Length == 2
               && int.TryParse(distances[0], NumberStyles.None, CultureInfo.InvariantCulture, out int first)
               && int.TryParse(distances[1], NumberStyles.None, CultureInfo.InvariantCulture, out int second)
            ? new Direction(first, second)
            : null;
    }

    /// <summary>Every leap of <paramref name="first"/> one way and <paramref name="second"/> the other, once.</summary>
    private static ImmutableList<Direction> Leaps(int first, int second) =>
    [
        .. new[] { new Direction(first, second), new Direction(second, first) }
            .SelectMany(leap => new[]
            {
                leap,
                leap with { First = -leap.First },
                new Direction(-leap.First, -leap.Second),
                leap with { Second = -leap.Second },
            })
            .Distinct(),
    ];

    private bool IsOnBoard(int column, int row) =>
        column >= 0 && column < this._width && row >= 0 && row < this._height;
}
