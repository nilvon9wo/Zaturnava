using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Engine.Boards;

/// <summary>
/// A board topology: its cells, how to step from one to the next, and how to lay them out. The
/// engine and every piece move only through this, never through coordinates.
/// </summary>
public interface ICellNavigating
{
    ImmutableList<CellId> Cells { get; }

    /// <summary>The neighbouring cell in <paramref name="direction"/>, or null off the board.</summary>
    CellId? Step(CellId from, Direction direction);

    /// <summary>
    /// The concrete directions of <paramref name="set"/> for a seat facing <paramref name="forward"/>.
    /// </summary>
    /// <exception cref="UnsupportedDirectionSetException">The topology has no such family.</exception>
    ImmutableList<Direction> Resolve([Required] DirectionSet set, Direction forward);

    CellLayout LayoutOf(CellId cell);
}
