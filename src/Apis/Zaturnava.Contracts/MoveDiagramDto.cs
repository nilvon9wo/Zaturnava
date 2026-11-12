using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Where a kind of piece may go, drawn on a small board of its own, <paramref name="Side"/> cells square, the piece on
/// its centre cell, facing up: the cells it may move to, those it may capture on, and the pieces it hops over, if it
/// hops. Cells are numbered <c>row * Side + column</c>, row 0 at the bottom. A side of 0 draws nothing.
/// </summary>
public sealed record MoveDiagramDto(
    int Side,
    ImmutableList<int> Moves,
    ImmutableList<int> Captures,
    ImmutableList<int> Screens
)
{
    /// <summary>
    /// The first row across a river the piece may not cross, the river drawn along its lower edge; none, for a piece
    /// no river holds back.
    /// </summary>
    public int? RiverRow { get; init; }
}
