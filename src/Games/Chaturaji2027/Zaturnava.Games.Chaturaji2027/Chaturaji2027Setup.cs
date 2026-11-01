using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The pinwheel (rules.md §2): seat 0 holds the bottom-left corner, its back rank along the bottom
/// edge from the corner inward - Boat, Horse, Elephant, Raja - with its Pawns in front; each next
/// seat, clockwise, is the same army turned a quarter turn clockwise about the centre.
/// </summary>
public static class Chaturaji2027Setup
{
    public const int Side = 8;
    public const int SeatCount = Pinwheel.SeatCount;
    private const int BackRow = 0;
    private const int PawnRow = 1;

    public static readonly ImmutableList<string> SeatNames = ["Black", "Yellow", "Green", "Red"];

    private static readonly ImmutableList<PieceKindId> BackRank =
        [Chaturaji2027Pieces.Boat, Chaturaji2027Pieces.Horse, Chaturaji2027Pieces.Elephant, Chaturaji2027Pieces.Raja];

    /// <summary>Every seat's pieces where they start, seat by seat, each army's back rank first.</summary>
    public static ImmutableList<Placement> Start { get; } = Pinwheel.Armies(
        Side,
        [
            .. BackRank.Select((kind, column) => new ArmyPiece(kind, column, BackRow)),
            .. BackRank.Select((_, column) => new ArmyPiece(Chaturaji2027Pieces.Pawn, column, PawnRow)),
        ]
    );

    public static ImmutableList<Seat> Seats() => Pinwheel.Seats();

    /// <summary>Opposite seats are allies.</summary>
    public static TeamGraph Teams() => Pinwheel.Teams();

    /// <summary>Each seat's throne: the square its Raja starts on.</summary>
    public static ImmutableDictionary<SeatId, CellId> Thrones(SquareGridTopology grid) =>
        Chaturaji2027Variant.Standard.Thrones(grid);

    public static Position InitialPosition(SquareGridTopology grid) => Pinwheel.PositionOf(grid, Start);
}
