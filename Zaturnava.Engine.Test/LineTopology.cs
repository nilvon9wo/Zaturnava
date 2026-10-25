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
            _ => throw new UnsupportedDirectionSetException(set, Name),
        };

    public CellLayout LayoutOf(CellId cell) => new(cell.Value, 0, "square");

    public string NameOf(CellId cell) => $"{cell.Value}";
}
