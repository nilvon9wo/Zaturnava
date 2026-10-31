using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test;

/// <summary>
/// A one-dimensional board, cells 0 to length - 1: the smallest topology that still has blocking,
/// leaping, edges and a forward direction, so the kernel is tested without any real board.
/// </summary>
internal sealed class LineTopology(int length) : ICellNavigating
{
    public const string Name = "line";
    public static readonly Direction Right = new(1, 0);
    public static readonly Direction Left = new(-1, 0);

    /// <summary>Two cells at a time, either way: a leap that passes one cell, for lame leaps.</summary>
    public static readonly DirectionSet Twos = new("twos");

    private readonly int _length = length;

    public ImmutableList<CellId> Cells => [.. Enumerable.Range(0, this._length).Select(index => new CellId(index))];

    public CellId? Step(CellId from, Direction direction)
    {
        int index = from.Value + direction.First;
        bool isOnBoard = index >= 0 && index < this._length;
        return isOnBoard
            ? new CellId(index)
            : null;
    }

    public ImmutableList<Direction> Resolve([Required] DirectionSet set, Direction forward) =>
        set.Name switch
        {
            "orthogonal" => [Right, Left],
            "forward" => [forward],
            "twos" => [new(2, 0), new(-2, 0)],
            _ => throw new UnsupportedDirectionSetException(set, Name),
        };

    /// <summary>Every cell between, one at a time.</summary>
    public ImmutableList<Direction> WaypointsOf(Direction leap) =>
    [
        .. Enumerable.Range(1, Math.Abs(leap.First) - 1)
            .Select(step => new Direction(step * Math.Sign(leap.First), 0)),
    ];

    public CellLayout LayoutOf(CellId cell) => new(cell.Value, 0, "square");

    public string NameOf(CellId cell) => $"{cell.Value}";
}
