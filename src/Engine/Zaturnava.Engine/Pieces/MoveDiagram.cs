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
    /// For a piece that other pieces in its way stop, as a Rook or a Xiangqi Horse is: one or two cells pieces put on
    /// would stop it most, to draw them there (the user's report of 2026-11-15, C2: pieces in the way, not red
    /// crosses); none, for a piece nothing in its way stops.
    /// </summary>
    public ImmutableList<int> Obstacles { get; init; } = [];

    /// <summary>The cells of <see cref="Moves"/> it may no longer move to, pieces on <see cref="Obstacles"/>.</summary>
    public ImmutableList<int> Obstructed { get; init; } = [];

    /// <summary>
    /// For a piece that takes by custody, as a Tafl King does: one such capture, drawn, with the piece another of its
    /// side must hold beyond (the user's report of 2026-11-15, C2); none, for any other.
    /// </summary>
    public CustodyExample? Custody { get; init; }

    /// <summary>
    /// For a piece drawn by a river or in a palace: the same piece on an open board, to draw instead when the player
    /// asks (the user's report of 2026-11-15, C2); none, for any other.
    /// </summary>
    public MoveDiagram? Elsewhere { get; init; }

    /// <summary>
    /// The cells it moves to farther than the next cell by leaping, over anything between, as a Knight does, rather
    /// than sliding, stopped by the first piece in the way (the user's report of 2026-11-12, of not knowing which).
    /// </summary>
    public ImmutableList<int> Leaps { get; init; } = [];

    /// <summary>
    /// For a piece that jumps, as a piece of Checkers does (<see cref="MovementRule.IsJumping"/>): the empty cells it
    /// lands on, having jumped and taken the piece on the capture cell before each, the first jump and then a second
    /// from there (the user's report of 2026-11-13, of its card showing it take where it steps); none, for any other.
    /// </summary>
    public ImmutableList<int> Jumps { get; init; } = [];

    /// <summary>
    /// For a piece that steps along a palace's lines: the cells of a palace about it, the piece on its throne, drawn
    /// with the lines from each of them to the throne, which <see cref="Moves"/> and <see cref="Captures"/> include
    /// the steps along (the user's report of 2026-11-13, of the Guard's card not showing them); none, for any other.
    /// </summary>
    public ImmutableList<int> Palace { get; init; } = [];

    /// <summary>
    /// The throne of <see cref="Palace"/>, for a piece drawn in its own palace, where it stands; none, for the piece
    /// drawn on the throne, on the centre cell.
    /// </summary>
    public int? Throne { get; init; }
}
