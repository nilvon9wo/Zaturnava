using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Moves;

/// <summary>
/// The moves the movement grammar allows a piece, on any topology. Whether a given occupant may be
/// captured (enemies only? allies too?) is the ruleset's call, passed in as a predicate.
/// </summary>
public sealed class MoveGenerator(ICellNavigating topology, ImmutableDictionary<PieceKindId, PieceKind> pieceKinds)
{
    private readonly ICellNavigating _topology = topology;
    private readonly ImmutableDictionary<PieceKindId, PieceKind> _pieceKinds = pieceKinds;

    /// <exception cref="ArgumentException">No piece stands on <paramref name="from"/>.</exception>
    public ImmutableList<Move> MovesFrom(
        [Required] Position position,
        CellId from,
        [Required] Seat ownerSeat,
        [Required] Func<Piece, bool> isCapturable
    )
    {
        Piece mover = position.PieceAt(from) ?? throw new ArgumentException($"No piece on {from}.", nameof(from));
        MoveSearch search = new(this._topology, position, mover, from, ownerSeat, isCapturable);
        return [.. this._pieceKinds[mover.Kind].Movement.SelectMany(search.MovesFor)];
    }
}

file sealed record MoveSearch(
    ICellNavigating Topology,
    Position Position,
    Piece Mover,
    CellId From,
    Seat OwnerSeat,
    Func<Piece, bool> IsCapturable
)
{
    public IEnumerable<Move> MovesFor(MovementRule rule) =>
        this.Topology
            .Resolve(rule.Directions, this.OwnerSeat.Forward)
            .SelectMany(direction => this.MovesAlong(rule, direction));

    private IEnumerable<Move> MovesAlong(MovementRule rule, Direction direction)
    {
        ImmutableList<CellId> ray = this.Ray(this.From, direction, rule.MaximumDistance);
        return ray
            .Select((cell, index) => new RayStep(cell, index + 1, ray.Take(index)))
            .Where(step => step.Distance >= rule.MinimumDistance)
            .Where(step => rule.IsLeaping || this.IsClear(step.PathBefore))
            .Select(step => this.MoveTo(step.Cell, rule))
            .OfType<Move>();
    }

    private ImmutableList<CellId> Ray(CellId from, Direction direction, int remaining)
    {
        CellId? next = remaining > 0
            ? this.Topology.Step(from, direction)
            : null;
        return next is { } cell
            ? [cell, .. this.Ray(cell, direction, remaining - 1)]
            : [];
    }

    private bool IsClear(IEnumerable<CellId> path) => path.All(cell => this.Position.PieceAt(cell) is null);

    private Move? MoveTo(CellId target, MovementRule rule)
    {
        Piece? occupant = this.Position.PieceAt(target);
        bool isAllowed = occupant is null
            ? rule.CanMove
            : rule.CanCapture && this.IsCapturable(occupant);
        return isAllowed
            ? new Move(this.Mover, this.From, target, occupant)
            : null;
    }
}

file sealed record RayStep(CellId Cell, int Distance, IEnumerable<CellId> PathBefore);
