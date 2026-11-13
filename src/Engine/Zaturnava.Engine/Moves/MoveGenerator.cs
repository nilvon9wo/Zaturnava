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

    /// <summary>
    /// The moves of the piece on <paramref name="from"/>: a jump, a chain of them, ending wherever it may stop; or, if
    /// <paramref name="isJumpByJump"/>, one jump alone, as each is played apart.
    /// </summary>
    /// <exception cref="ArgumentException">No piece stands on <paramref name="from"/>.</exception>
    public ImmutableList<Move> MovesFrom(
        [Required] Position position,
        CellId from,
        [Required] Seat ownerSeat,
        [Required] Func<Piece, bool> isCapturable,
        bool isJumpByJump = false
    )
    {
        Piece mover = PieceOn(position, from);
        MoveSearch search = new(new Reach(this._topology, position, from, ownerSeat), mover, isCapturable)
        {
            IsJumpByJump = isJumpByJump,
        };
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
                .SelectMany(rule => rule.IsJumping ? reach.JumpedFor(rule) : reach.CellsFor(rule))
                .Distinct()
                .Select(target => new Attack(attacker, from, target)),
        ];
    }

    private static Piece PieceOn(Position position, CellId from) =>
        position.PieceAt(from) ?? throw new ArgumentException($"No piece on {from}.", nameof(from));
}

file sealed record MoveSearch(Reach Reach, Piece Mover, Func<Piece, bool> IsCapturable)
{
    /// <summary>Whether a jump is a move alone, not the first of a chain the move goes on with.</summary>
    public bool IsJumpByJump { get; init; }

    public IEnumerable<Move> MovesFor(MovementRule rule) =>
        rule.IsJumping
            ? this.JumpsFor(rule)
            : this.Reach.CellsFor(rule).Select(cell => this.MoveTo(cell, rule)).OfType<Move>();

    /// <summary>
    /// Every chain of jumps the rule allows, each a move of its own, so that it may stop after any jump (Proposed,
    /// 2026-11-13): over a piece it may take, next to it, to the empty cell beyond; then on from there, each piece
    /// jumped gone. Of the chains that end on one cell, the one that takes the most, the first found among those
    /// alike, as a move names only where it ends; none that ends where it began.
    /// </summary>
    private IEnumerable<Move> JumpsFor(MovementRule rule)
    {
        Position lifted = new(this.Reach.Position.Occupants.Remove(this.Reach.From));
        return this.ChainsFrom(rule, lifted, this.Reach.From, [])
            .Where(chain => chain.Landing != this.Reach.From)
            .GroupBy(chain => chain.Landing)
            .Select(ends => ends.MaxBy(chain => chain.Jumped.Count)!)
            .Select(chain =>
                new Move(this.Mover, this.Reach.From, chain.Landing, this.Reach.Position.PieceAt(chain.Jumped[0]))
                {
                    Jumped = chain.Jumped,
                }
            );
    }

    /// <summary>Each chain of one jump or more from <paramref name="from"/>, after those already made.</summary>
    private IEnumerable<Chain> ChainsFrom(
        MovementRule rule,
        Position position,
        CellId from,
        ImmutableList<CellId> jumped
    ) =>
        this.Reach.Topology
            .Resolve(rule.Directions, this.Reach.OwnerSeat.Forward)
            .Select(direction => this.JumpOf(rule, position, from, direction, jumped))
            .OfType<Chain>()
            .SelectMany(chain => this.IsJumpByJump
                ? (IEnumerable<Chain>)[chain]
                : (IEnumerable<Chain>)[chain, .. this.ChainsFrom(rule, chain.Position, chain.Landing, chain.Jumped)]
            );

    /// <summary>
    /// The jump along <paramref name="direction"/>: over a piece it may take, to the empty cell beyond, that piece
    /// gone; none, if there is no such piece, or no such cell.
    /// </summary>
    private Chain? JumpOf(
        MovementRule rule,
        Position position,
        CellId from,
        Direction direction,
        ImmutableList<CellId> jumped
    ) =>
        this.Reach.Topology.Step(from, direction) is { } over
        && position.PieceAt(over) is { } piece
        && !rule.Shunned.Contains(piece.Kind)
        && this.IsCapturable(piece)
        && this.Reach.Topology.Step(over, direction) is { } landing
        && position.PieceAt(landing) is null
            ? new Chain(landing, jumped.Add(over), new Position(position.Occupants.Remove(over)))
            : null;

    private Move? MoveTo(CellId target, MovementRule rule)
    {
        Piece? occupant = this.Reach.Position.PieceAt(target);
        bool isAllowed = occupant is null
            ? rule.CanMove
            : rule.CanCapture && !rule.Shunned.Contains(occupant.Kind) && this.IsCapturable(occupant);
        return isAllowed
            ? new Move(this.Mover, this.Reach.From, target, occupant)
            : null;
    }
}

/// <summary>
/// The cells a rule lets a piece reach: far enough, and not past a piece, unless it leaps; not by a lame leap
/// whose way is blocked; and, for a hop, only those beyond the piece it hops.
/// </summary>
file sealed record Reach(ICellNavigating Topology, Position Position, CellId From, Seat OwnerSeat)
{
    public IEnumerable<CellId> CellsFor(MovementRule rule) =>
        this.Topology
            .Resolve(rule.Directions, this.OwnerSeat.Forward)
            .SelectMany(direction => this.CellsAlong(rule, direction));

    /// <summary>
    /// The cells a jump could take a piece on, were one there: each next to it whose cell beyond is on the board and
    /// empty.
    /// </summary>
    public IEnumerable<CellId> JumpedFor(MovementRule rule) =>
        this.Topology
            .Resolve(rule.Directions, this.OwnerSeat.Forward)
            .Select(direction =>
                this.Topology.Step(this.From, direction) is { } over
                && this.Topology.Step(over, direction) is { } landing
                && this.Position.PieceAt(landing) is null
                    ? over
                    : (CellId?)null
            )
            .OfType<CellId>();

    private IEnumerable<CellId> CellsAlong(MovementRule rule, Direction direction)
    {
        ImmutableList<CellId> ray = this.Ray(this.From, direction, rule.MaximumDistance);
        return rule.IsHopping ? this.Beyond(rule, ray) : this.Along(rule, direction, ray);
    }

    private IEnumerable<CellId> Along(MovementRule rule, Direction direction, ImmutableList<CellId> ray) =>
        ray
            .Select((cell, index) => new RayStep(cell, index + 1, ray.Take(index), [this.From, .. ray.Take(index)]))
            .Where(step => step.Distance >= rule.MinimumDistance)
            .Where(step => rule.IsLeaping || this.IsClear(step.PathBefore))
            .Where(step => !rule.IsLame || step.LegStarts.All(start => this.IsLegClear(start, direction)))
            .Select(step => step.Cell);

    /// <summary>
    /// The cells beyond the first piece on the ray, unless the rule shuns it: as many as the rule allows, up to
    /// and including the next piece.
    /// </summary>
    private IEnumerable<CellId> Beyond(MovementRule rule, ImmutableList<CellId> ray)
    {
        int screen = ray.FindIndex(cell => this.Position.PieceAt(cell) is not null);
        bool mayHop = screen >= 0 && !rule.Shunned.Contains(this.Position.PieceAt(ray[screen])!.Kind);
        ImmutableList<CellId> beyond = mayHop ? [.. ray.Skip(screen + 1).Take(rule.MaximumBeyond)] : [];
        return beyond.Where((_, index) => this.IsClear(beyond.Take(index)));
    }

    /// <summary>Whether no piece stands on the cells a lame leap from <paramref name="start"/> passes.</summary>
    private bool IsLegClear(CellId start, Direction direction) =>
        this.IsClear(
            this.Topology.WaypointsOf(direction).Select(offset => this.Topology.Step(start, offset)).OfType<CellId>()
        );

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

/// <summary>Jumps made one after another: where the last landed, the cells jumped, and what is left standing.</summary>
file sealed record Chain(CellId Landing, ImmutableList<CellId> Jumped, Position Position);

/// <summary>A cell on a ray: how far along it is, the cells before it, and where each step to it began.</summary>
file sealed record RayStep(CellId Cell, int Distance, IEnumerable<CellId> PathBefore, ImmutableList<CellId> LegStarts);
