using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The flying-general capture (Grand Legions specification §6.4), as in Xiangqi: a piece of <paramref name="flyers"/>
/// (the Xiangqi General) may take a leader, any of <paramref name="leaders"/>, along an open orthogonal line of any
/// length, nothing between the two, moving onto its cell. It is the flyer's move: a Leader die allows it, or a move
/// made without rolling.
/// </summary>
internal sealed class FlyingCapture(
    SquareGridTopology grid,
    ImmutableHashSet<PieceKindId> flyers,
    ImmutableHashSet<PieceKindId> leaders
)
{
    private static readonly ImmutableList<Direction> Orthogonal = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

    private readonly SquareGridTopology _grid = grid;
    private readonly ImmutableHashSet<PieceKindId> _flyers = flyers;
    private readonly ImmutableHashSet<PieceKindId> _leaders = leaders;

    /// <summary>Each leader the piece on <paramref name="cell"/> may take flying, as its move.</summary>
    public IEnumerable<Move> MovesFrom(GameState state, CellId cell, Piece piece, Func<Piece, bool> isCapturable) =>
        this.AttacksFrom(state, cell, piece)
            .Select(attack => new Move(piece, cell, attack.Target, state.Position.PieceAt(attack.Target)))
            .Where(move => isCapturable(move.Captured!));

    /// <summary>The leaders the piece on <paramref name="cell"/> could take flying, whoever's they are.</summary>
    public IEnumerable<Attack> AttacksFrom(GameState state, CellId cell, Piece piece) =>
        this.ReachFrom(state, cell, piece)
            .Where(target => state.Position.PieceAt(target) is { } leader && this._leaders.Contains(leader.Kind))
            .Select(target => new Attack(piece, cell, target));

    /// <summary>
    /// Each cell the piece on <paramref name="cell"/> could take a leader on, were one there: along each open line,
    /// to the first piece on it.
    /// </summary>
    public IEnumerable<CellId> ReachFrom(GameState state, CellId cell, Piece piece) =>
        this._flyers.Contains(piece.Kind)
            ? Orthogonal.SelectMany(way => OpenTo(state, this._grid.Ray(cell, way)))
            : [];

    /// <summary>The cells of <paramref name="line"/>, to its first piece, if any, and that one.</summary>
    private static IEnumerable<CellId> OpenTo(GameState state, ImmutableList<CellId> line)
    {
        int first = line.FindIndex(next => state.Position.PieceAt(next) is not null);
        return first < 0 ? line : line.Take(first + 1);
    }
}
