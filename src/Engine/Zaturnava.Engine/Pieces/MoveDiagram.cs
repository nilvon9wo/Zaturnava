using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// Where a piece may go, drawn on a small board of its own, <paramref name="Side"/> cells square, with the piece on
/// its centre cell, facing up: the cells it may move to (<paramref name="Moves"/>), those it may capture on, were an
/// enemy there (<paramref name="Captures"/>), and the pieces it hops over, for a piece that hops
/// (<paramref name="Screens"/>). Cells are numbered <c>row * Side + column</c>, row 0 at the bottom.
/// <see cref="None"/>, of no cells, for a kind no diagram is drawn of.
/// </summary>
public sealed record MoveDiagram(
    int Side,
    ImmutableList<int> Moves,
    ImmutableList<int> Captures,
    ImmutableList<int> Screens
)
{
    public static MoveDiagram None { get; } = new(0, [], [], []);

    /// <summary>
    /// The first row across a river the piece may not cross, the river drawn along its lower edge; none, for a piece
    /// no river holds back.
    /// </summary>
    public int? RiverRow { get; init; }

    /// <summary>
    /// The cells a piece on which would block one of its leaps, for a piece whose leaps a piece in the way blocks, as
    /// a Xiangqi Horse's leg does; none, for any other.
    /// </summary>
    public ImmutableList<int> Blockers { get; init; } = [];
}
