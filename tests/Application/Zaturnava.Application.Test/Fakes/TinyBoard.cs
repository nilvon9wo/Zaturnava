using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

/// <summary>A row of four cells, "a1" to "d1", to name, not to move on.</summary>
internal sealed class TinyBoard : ICellNavigating
{
    public ImmutableList<CellId> Cells { get; } = [.. Enumerable.Range(0, 4).Select(cell => new CellId(cell))];

    public CellId? Step(CellId from, Direction direction) => null;

    public ImmutableList<Direction> Resolve([Required] DirectionSet set, Direction forward) => [];

    public CellLayout LayoutOf(CellId cell) => new(cell.Value, 0, "square");

    public string NameOf(CellId cell) => $"{(char)('a' + cell.Value)}1";
}
