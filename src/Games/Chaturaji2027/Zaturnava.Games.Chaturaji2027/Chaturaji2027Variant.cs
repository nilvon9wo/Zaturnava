using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// What a game played by these rules is played with (Proposed): its board, <paramref name="Side"/> cells square,
/// with four seats in a pinwheel; its pieces, how they move, what taking each scores, and their names; which
/// kinds lead an army - a leader is what Chaturaji 2027 calls a Raja, and its start is its seat's throne - and
/// which are Pawns, promoted at the far edge, swapping back to back; and where every piece starts.
/// <see cref="Standard"/> is Chaturaji 2027's own; Grand Legions plays others.
/// </summary>
public sealed record Chaturaji2027Variant(
    string Id,
    string Name,
    int Side,
    ImmutableDictionary<PieceKindId, PieceKind> Kinds,
    ImmutableDictionary<PieceKindId, int> Points,
    ImmutableDictionary<PieceKindId, string> Names,
    ImmutableHashSet<PieceKindId> Leaders,
    ImmutableHashSet<PieceKindId> Pawns,
    ImmutableList<Placement> Start
)
{
    public static Chaturaji2027Variant Standard { get; } = new(
        Chaturaji2027Rules.RulesetId,
        "Chaturaji 2027",
        Chaturaji2027Setup.Side,
        Chaturaji2027Pieces.Kinds,
        Chaturaji2027Pieces.Points,
        Chaturaji2027Pieces.ChaturajiNames,
        [Chaturaji2027Pieces.Raja],
        [Chaturaji2027Pieces.Pawn],
        Chaturaji2027Setup.Start
    )
    {
        Heavies = [Chaturaji2027Pieces.Elephant],
    };

    /// <summary>
    /// The game this is one size of, if it is played at several; none, for a game of one size, which is its own.
    /// </summary>
    public GameSize? Game { get; init; }

    /// <summary>The kinds, leaders apart, most dangerous to let loose, as a Rook is: none, unless set.</summary>
    public ImmutableHashSet<PieceKindId> Heavies { get; init; } = [];

    /// <summary>What a die showing each value is called, where not by the kinds it lets move: none.</summary>
    public ImmutableDictionary<int, string> DieNames { get; init; } = [];

    /// <summary>
    /// Whether a piece may make a move its kind allows, as the board has it (a river, say); null, if every
    /// such move may be made.
    /// </summary>
    public MoveAllowing? MayMove { get; init; }

    /// <summary>Each seat's throne: the square its first leader starts on.</summary>
    public ImmutableDictionary<SeatId, CellId> Thrones([Required] SquareGridTopology grid) =>
        this.Start
            .Where(placed => this.Leaders.Contains(placed.Kind))
            .GroupBy(placed => placed.Owner)
            .ToImmutableDictionary(army => army.Key, army => grid.CellAt(army.First().Column, army.First().Row));

    public Position InitialPosition([Required] SquareGridTopology grid) => Pinwheel.PositionOf(grid, this.Start);
}
