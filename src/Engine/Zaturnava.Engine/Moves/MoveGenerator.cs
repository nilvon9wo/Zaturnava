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
        Piece mover = PieceOn(position, from);
        MoveSearch search = new(new Reach(this._topology, position, from, ownerSeat), mover, isCapturable);
        return [.. this._pieceKinds[mover.Kind].Movement.SelectMany(search.MovesFor)];
    }

    /// <summary>
    /// The cells the piece on <paramref name="from"/> could capture on, were there a piece to take on each:
    /// those it guards, as well as those it threatens. Whether it may capture what is there is not asked.
    /// </summary>
    /// <exception cref="ArgumentException">No piece stands on <paramref name="from"/>.</exception>
    public ImmutableList<Attack> AttacksFrom([Required] Position position, CellId from, [Required] Seat ownerSeat)
    {
        Piece attacker = PieceOn(position, from);
        Reach reach = new(this._topology, position, from, ownerSeat);
        return
        [
            .. this._pieceKinds[attacker.Kind].Movement
                .Where(rule => rule.CanCapture)
                .SelectMany(reach.CellsFor)
                .Distinct()
                .Select(target => new Attack(attacker, from, target)),
        ];
    }

    private static Piece PieceOn(Position position, CellId from) =>
        position.PieceAt(from) ?? throw new ArgumentException($"No piece on {from}.", nameof(from));
}

file sealed record MoveSearch(Reach Reach, Piece Mover, Func<Piece, bool> IsCapturable)
{
    public IEnumerable<Move> MovesFor(MovementRule rule) =>
        this.Reach.CellsFor(rule).Select(cell => this.MoveTo(cell, rule)).OfType<Move>();

    private Move? MoveTo(CellId target, MovementRule rule)
    {
        Piece? occupant = this.Reach.Position.PieceAt(target);
        bool isAllowed = occupant is null
            ? rule.CanMove
            : rule.CanCapture && this.IsCapturable(occupant);
        return isAllowed
            ? new Move(this.Mover, this.Reach.From, target, occupant)
            : null;
    }
}

/// <summary>The cells a rule lets a piece reach: far enough, and not past a piece, unless it leaps.</summary>
file sealed record Reach(ICellNavigating Topology, Position Position, CellId From, Seat OwnerSeat)
{
    public IEnumerable<CellId> CellsFor(MovementRule rule) =>
        this.Topology
            .Resolve(rule.Directions, this.OwnerSeat.Forward)
            .SelectMany(direction => this.CellsAlong(rule, direction));

    private IEnumerable<CellId> CellsAlong(MovementRule rule, Direction direction)
    {
        ImmutableList<CellId> ray = this.Ray(this.From, direction, rule.MaximumDistance);
        return ray
            .Select((cell, index) => new RayStep(cell, index + 1, ray.Take(index)))
            .Where(step => step.Distance >= rule.MinimumDistance)
            .Where(step => rule.IsLeaping || this.IsClear(step.PathBefore))
            .Select(step => step.Cell);
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
}

file sealed record RayStep(CellId Cell, int Distance, IEnumerable<CellId> PathBefore);
