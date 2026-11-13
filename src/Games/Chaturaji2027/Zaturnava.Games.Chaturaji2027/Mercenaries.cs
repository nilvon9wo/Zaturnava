using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Mercenaries (Grand Legions specification §6.5, Stated 2026-11-13): every piece that starts as one of
/// <paramref name="kinds"/> (the Shogi pieces), but a leader, is a mercenary, and stays one whatever it is promoted
/// into; so does what it is promoted into. The holder of a mercenary prisoner may hire it for points, 110% of what it
/// cost last (at first, its value), rounded up (Proposed): it becomes the hirer's own, off the board
/// (<see cref="GameState.Hired"/>), until they place it on a free square of their palace - or, with none, of the 3 × 3
/// square about their throne (Proposed). Each step is a turn's deliberate action, or uses a die that moves its kind.
/// Only in a game dealt from <see cref="Edition"/> on, so that a game stored before replays as it was played.
/// </summary>
internal sealed class Mercenaries(
    SquareGridTopology grid,
    ImmutableHashSet<PieceKindId> kinds,
    ImmutableHashSet<PieceKindId> leaders,
    Func<int, ImmutableDictionary<PieceKindId, int>> points,
    Position initial,
    ThroneControl control
)
{
    /// <summary>The edition of the rules (<see cref="GameState.Edition"/>) a game has mercenaries from.</summary>
    public const int Edition = 2;

    private readonly SquareGridTopology _grid = grid;
    private readonly ImmutableHashSet<PieceKindId> _kinds = kinds;
    private readonly ImmutableHashSet<PieceKindId> _leaders = leaders;
    private readonly Func<int, ImmutableDictionary<PieceKindId, int>> _points = points;
    private readonly Position _initial = initial;
    private readonly ThroneControl _control = control;

    /// <summary>Whether the piece is a mercenary: it started as one, or became one by a mercenary promotion.</summary>
    public bool IsMercenary(GameState state, Piece piece) =>
        state.Edition >= Edition && (state.MercenaryCosts.ContainsKey(piece.Id) || this.StartedAsOne(state, piece));

    /// <summary>
    /// Whether the piece started as a mercenary kind; one that started nowhere, as a scenario may add (no game dealt
    /// has any), is judged as it is.
    /// </summary>
    private bool StartedAsOne(GameState state, Piece piece)
    {
        Piece started =
            (state.Start ?? this._initial).Occupants.Values.FirstOrDefault(start => start.Id == piece.Id) ?? piece;
        return this._kinds.Contains(started.Kind) && !this._leaders.Contains(started.Kind);
    }

    /// <summary>What hiring the mercenary costs now: 110% of what it cost last, rounded up.</summary>
    public int CostOf(GameState state, Piece piece) => ((this.LastCostOf(state, piece) * 11) + 9) / 10;

    /// <summary>
    /// The hires the seat may make, playing for itself, of a kind <paramref name="isMovable"/>: each mercenary it holds
    /// prisoner that it has the points for.
    /// </summary>
    public IEnumerable<HireAction> HiresOf(GameState state, SeatId seat, Func<PieceKindId, bool> isMovable) =>
        state.ActingFor is null
            ? state.Prisoners
                .Where(prisoner => prisoner.Holder == seat && isMovable(prisoner.Piece.Kind))
                .Where(prisoner => this.IsMercenary(state, prisoner.Piece))
                .Where(prisoner => this.CostOf(state, prisoner.Piece) <= state.Scores.GetValueOrDefault(seat))
                .Select(prisoner => new HireAction(seat, prisoner.Piece.Id))
            : [];

    /// <summary>
    /// The placings the seat may make, playing for itself, of a kind <paramref name="isMovable"/>: each mercenary it
    /// hired, on each free square of its palace.
    /// </summary>
    public IEnumerable<DeployAction> DeploysOf(GameState state, SeatId seat, Func<PieceKindId, bool> isMovable) =>
        state.ActingFor is null
            ? state.Hired
                .Where(piece => piece.Owner == seat && isMovable(piece.Kind))
                .SelectMany(piece => this.BarracksOf(state, seat)
                    .Where(cell => state.Position.PieceAt(cell) is null)
                    .Select(cell => new DeployAction(seat, piece.Id, cell))
                )
            : [];

    /// <summary>The prisoner is hired: the hirer's own now, off the board; the hirer pays for it.</summary>
    public Transition Hired(GameState state, HireAction hire)
    {
        Prisoner prisoner = state.Prisoners.First(held => held.Piece.Id == hire.Piece); // the hire is legal
        int cost = this.CostOf(state, prisoner.Piece);
        GameState hired = state with
        {
            Prisoners = state.Prisoners.Remove(prisoner),
            Hired = state.Hired.Add(prisoner.Piece with { Owner = hire.Actor }),
            Scores = state.Scores.SetItem(hire.Actor, state.Scores.GetValueOrDefault(hire.Actor) - cost),
            MercenaryCosts = state.MercenaryCosts.SetItem(prisoner.Piece.Id, cost),
        };
        return new Transition(hired, [new PieceHired(prisoner.Piece, hire.Actor, cost)]);
    }

    /// <summary>The hired mercenary is placed on the board.</summary>
    public static Transition Deployed(GameState state, DeployAction deploy)
    {
        Piece piece = state.Hired.First(hired => hired.Id == deploy.Piece); // the placing is legal
        GameState deployed = state with
        {
            Hired = state.Hired.Remove(piece),
            Position = state.Position.Place(deploy.Home, piece),
        };
        return new Transition(deployed, [new PieceDeployed(piece, deploy.Home)]);
    }

    /// <summary>
    /// The state once <paramref name="from"/> is promoted into <paramref name="into"/>: if it was a mercenary, what it
    /// becomes is one, costing what it cost last, or that piece's value, if more; and what it becomes keeps its own
    /// cost, if it was a mercenary and that is more.
    /// </summary>
    public GameState Promoted(GameState state, Piece from, Piece into) =>
        this.IsMercenary(state, from)
            ? state with
            {
                MercenaryCosts = state.MercenaryCosts.SetItem(
                    into.Id,
                    new[]
                    {
                        this.LastCostOf(state, from),
                        this._points(state.Edition).GetValueOrDefault(into.Kind),
                        this.IsMercenary(state, into) ? this.LastCostOf(state, into) : 0,
                    }.Max()
                ),
            }
            : state;

    private int LastCostOf(GameState state, Piece piece) =>
        state.MercenaryCosts.TryGetValue(piece.Id, out int cost)
            ? cost
            : this._points(state.Edition).GetValueOrDefault(piece.Kind);

    /// <summary>Where the seat's mercenaries are placed: its palace, or the 3 × 3 square about its throne.</summary>
    private IEnumerable<CellId> BarracksOf(GameState state, SeatId seat) =>
        (state.Palaces ?? []).TryGetValue(seat, out ImmutableList<CellId>? palace)
            ? palace
            : this.SquareAbout(this._control.ThronesOf(state)[seat]); // every seat has its throne

    /// <summary>The cell and those about it on the board.</summary>
    private IEnumerable<CellId> SquareAbout(CellId centre) =>
        Enumerable.Range(-1, 3)
            .SelectMany(rows => Enumerable.Range(-1, 3).Select(columns => new Direction(columns, rows)))
            .Select(way => way == new Direction(0, 0) ? centre : this._grid.Step(centre, way))
            .OfType<CellId>();
}
