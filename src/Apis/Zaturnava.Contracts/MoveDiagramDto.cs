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

    /// <summary>
    /// The cells a piece on which would block one of its leaps, as a Xiangqi Horse's leg does; none, for a piece
    /// nothing blocks so.
    /// </summary>
    public ImmutableList<int> Blockers { get; init; } = [];

    /// <summary>
    /// The cells it moves to farther than the next cell by leaping, over anything between, as a Knight does; every
    /// other cell farther off, it slides to, stopped by the first piece in the way.
    /// </summary>
    public ImmutableList<int> Leaps { get; init; } = [];

    /// <summary>
    /// For a piece that jumps, as a piece of Checkers does: the empty cells it lands on, having jumped and taken the
    /// piece on the capture cell before each, and then again from there; none, for any other.
    /// </summary>
    public ImmutableList<int> Jumps { get; init; } = [];

    /// <summary>
    /// For a piece that steps along a palace's lines: the cells of a palace about it, the piece on its throne, to draw
    /// with a line from each to the throne; its moves and captures include the steps along them. None, for any other.
    /// </summary>
    public ImmutableList<int> Palace { get; init; } = [];
}
