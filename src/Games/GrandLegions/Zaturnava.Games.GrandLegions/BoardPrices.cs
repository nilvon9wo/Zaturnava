using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// What each piece costs, and scores when taken, on a game's own board (pieces.md, Prices for each game): the
/// sheet's value, counted on 8 × 8, adjusted from the board's actual dimensions, never from values set for each size
/// (Stated 2026-11-06). Its reach is counted again from the board's centre, and its promotion premium again over the
/// distance from its kingdom's front row to its zone; the value moves by what each gains or loses (Proposed).
/// </summary>
public static class BoardPrices
{
    private const int ReferenceSide = 8;

    /// <summary>
    /// The least a piece costs (Proposed): one that reaches nothing from the centre of a small board, as the Giraffe
    /// on 6 × 6, is still never free, which would let an army fill its kingdom with it.
    /// </summary>
    private const int MinimumPrice = 1;

    /// <summary>How many rows a Shogi piece's zone has: the far three.</summary>
    private const int ShogiZoneRows = 3;

    private static readonly Seat Upward = new(new SeatId(0), new Direction(0, 1));
    private static readonly SeatId Enemy = new(1);

    /// <summary>
    /// What the chance of promoting adds to each piece's value, as the sheet's promotion premium has it
    /// (<c>Values/grand-legions-premiums.json</c>); none, for a piece not named.
    /// </summary>
    private static readonly ImmutableDictionary<PieceKindId, int> Premiums =
        PieceValues.Of(typeof(BoardPrices).Assembly, QuickGame.GameId + "-premiums");

    private static readonly ImmutableDictionary<PieceKindId, int> Values =
        GrandLegionsPieces.Catalogue.ToImmutableDictionary(piece => piece.Id, piece => piece.Value);

    private static readonly ImmutableDictionary<PieceKindId, int> ReferenceReaches =
        ReachesOn(ReferenceSide, ReferenceSide);

    private static readonly ConcurrentDictionary<GrandBoard, ImmutableDictionary<PieceKindId, int>> Priced = new();

    /// <summary>Each piece's price on <paramref name="board"/>; its rivers change none.</summary>
    public static ImmutableDictionary<PieceKindId, int> On([Required] GrandBoard board) =>
        Priced.GetOrAdd(board with { Rivers = Rivers.None, Course = RiverCourse.ThroughKingdoms }, PricesOn);

    /// <summary>The catalogue, each piece at its price on <paramref name="board"/>.</summary>
    public static ImmutableList<GrandPiece> Catalogue([Required] GrandBoard board)
    {
        ImmutableDictionary<PieceKindId, int> prices = On(board);
        return [.. GrandLegionsPieces.Catalogue.Select(piece => piece with { Value = prices[piece.Id] })];
    }

    private static ImmutableDictionary<PieceKindId, int> PricesOn(GrandBoard board)
    {
        ImmutableDictionary<PieceKindId, int> reaches = ReachesOn(board.Width, board.Length);
        return GrandLegionsPieces.Catalogue.ToImmutableDictionary(
            piece => piece.Id,
            piece => Math.Max(
                MinimumPrice,
                piece.Value
                    + reaches[piece.Id]
                    - ReferenceReaches[piece.Id]
                    + PremiumOn(piece, board)
                    - Premiums.GetValueOrDefault(piece.Id)
            )
        );
    }

    /// <summary>
    /// Each piece's reach from the centre of an empty board (from d4, on 8 × 8), its own player facing up the board:
    /// the cells it could move or capture on, a jump capturing on the cell it jumps; for a hopper, which needs a piece
    /// to hop, with a piece on every cell next to it, if that reaches more.
    /// </summary>
    private static ImmutableDictionary<PieceKindId, int> ReachesOn(int width, int length)
    {
        SquareGridTopology topology = new(width, length);
        MoveGenerator generator = new(topology, GrandLegionsPieces.Kinds);
        CellId centre = topology.CellAt((width - 1) / 2, (length - 1) / 2);
        Position Alone(PieceKindId kind) => Position.Empty.Place(centre, new Piece(new PieceId(0), kind, Upward.Id));
        Position Surrounded(PieceKindId kind) =>
            topology.Resolve(DirectionSets.Any, Upward.Forward)
                .Select((direction, index) => (Cell: topology.Step(centre, direction)!.Value, Id: index + 1))
                .Aggregate(
                    Alone(kind),
                    (around, next) => around.Place(
                        next.Cell,
                        new Piece(new PieceId(next.Id), GrandLegionsPieces.Rook, Enemy)
                    )
                );
        int Reach(Position position) =>
            generator.MovesFrom(position, centre, Upward, _ => true)
                .Select(move => move.Jumped.IsEmpty ? move.To : move.Jumped[0])
                .Concat(generator.AttacksFrom(position, centre, Upward).Select(attack => attack.Target))
                .Distinct()
                .Count();
        return GrandLegionsPieces.Catalogue.ToImmutableDictionary(
            piece => piece.Id,
            piece => Math.Max(Reach(Alone(piece.Id)), Reach(Surrounded(piece.Id)))
        );
    }

    /// <summary>
    /// The premium on <paramref name="board"/>: the higher of its own promotion's and, for Infantry not confined to
    /// a palace, the far edge's, a promotion into a stand-in, the Rook (pieces.md, Promotion premium).
    /// </summary>
    private static int PremiumOn(GrandPiece piece, GrandBoard board)
    {
        int unpromoted = piece.Value - Premiums.GetValueOrDefault(piece.Id);
        int own = piece.PromotesTo.Count == 0
            ? 0
            : Over(piece.PromotesTo.Max(into => Values[into]) - unpromoted, OwnDistance(piece, board));
        int farEdge = piece.Class == PieceClass.Infantry && !piece.Restrictions.Any(limit => limit.IsConfining)
            ? Over(Values[GrandLegionsPieces.Rook] - unpromoted, board.Length - board.KingdomDepth)
            : 0;
        return Math.Max(own, farEdge);
    }

    /// <summary>
    /// How far a piece starting on its kingdom's front row has to go to its own promotion's zone: a Western or
    /// Checkers piece's, the last row; a Shogi piece's, the far three rows; a Makruk Pawn's, the kingdom ahead.
    /// </summary>
    private static int OwnDistance(GrandPiece piece, GrandBoard board)
    {
        int front = board.KingdomDepth - 1;
        return (piece.Id == GrandLegionsPieces.MakrukPawn, piece.PromotesOnLastRow) switch
        {
            (true, _) => board.Kingdoms[1].Row - front,
            (_, true) => board.Length - 1 - front,
            _ => board.Length - ShogiZoneRows - front,
        };
    }

    /// <summary>What a gain is worth over a distance at a premium factor of 1, to the nearest point.</summary>
    private static int Over(int gain, int distance) =>
        (int)Math.Round((double)gain / Math.Max(1, distance), MidpointRounding.AwayFromZero);
}
