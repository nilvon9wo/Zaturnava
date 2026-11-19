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
    /// For a piece other pieces in its way stop: one or two cells pieces put on would stop it most, to draw them
    /// there when the player asks; none, for a piece nothing in its way stops.
    /// </summary>
    public ImmutableList<int> Obstacles { get; init; } = [];

    /// <summary>The cells of its moves it may no longer move to, pieces on <see cref="Obstacles"/>.</summary>
    public ImmutableList<int> Obstructed { get; init; } = [];

    /// <summary>
    /// As <see cref="Obstacles"/>, for its captures: one or two cells pieces put on would stop it capturing most;
    /// none, for a piece nothing in its way stops capturing.
    /// </summary>
    public ImmutableList<int> CaptureObstacles { get; init; } = [];

    /// <summary>
    /// The cells of its captures it may no longer capture on, pieces on <see cref="CaptureObstacles"/>.
    /// </summary>
    public ImmutableList<int> CaptureObstructed { get; init; } = [];

    /// <summary>
    /// For a piece that takes by custody, as a Tafl King does: one such capture, to draw with the piece of its own
    /// side that must stand beyond; none, for any other.
    /// </summary>
    public CustodyDto? Custody { get; init; }

    /// <summary>
    /// For a piece that takes by custody, its captures gone berserk, as while an enemy holds its throne: landing where
    /// <see cref="Custody"/> does, every piece, friend or foe, held between it and any other piece taken at once; none,
    /// for any other.
    /// </summary>
    public ImmutableList<CustodyDto> Berserk { get; init; } = [];

    /// <summary>
    /// For a piece drawn by a river or in a palace: the same piece on an open board, to draw when the player asks;
    /// none, for any other.
    /// </summary>
    public MoveDiagramDto? Elsewhere { get; init; }

    /// <summary>
    /// Other drawings of the piece, each named, to draw instead when the player asks: where it stands changing how it
    /// moves, and each special power it has, at work. None, for a piece with neither.
    /// </summary>
    public ImmutableList<NamedDiagramDto> Alternates { get; init; } = [];

    /// <summary>For a drawing of a power at work: what the power is, in a few words; none, for any other.</summary>
    public string? Told { get; init; }

    /// <summary>Cells drawn with marks of their own, each mark with what it means here.</summary>
    public ImmutableList<CellMarkDto> Marks { get; init; } = [];

    /// <summary>The example played of a power at work: where the piece goes, and what it takes; none, else.</summary>
    public PlayedExampleDto? Example { get; init; }

    /// <summary>Whether <see cref="Palace"/> is drawn with the lines from each cell to its throne.</summary>
    public bool IsPalaceLined { get; init; } = true;

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

    /// <summary>
    /// The throne of <see cref="Palace"/>, for a piece drawn in its own palace, where it stands; none, for the piece
    /// drawn on the throne, on the centre cell.
    /// </summary>
    public int? Throne { get; init; }
}
