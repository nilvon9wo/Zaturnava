using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Learning;

/// <summary>
/// A position as numbers for a network to learn from, seen from one seat. The seats are counted from it
/// (itself, the next, and so on), so that what is learned of one seat serves every seat. In order: how many
/// pieces of each kind each army has on the board, and how far they have come, all told, each counted in
/// rows from its own edge; the prisoners each seat holds of each army, by kind; the scores; whose decision
/// it is; the dice rolled and not used, and whether the game waits on more; who has given up their turns,
/// and who has retired; and what Sage counts (<see cref="Appraisal.FeaturesOf"/>). Most are nothing, so a
/// position is given as only those that are not, by index, in order. The kinds of piece are those the game
/// starts with; one it does not start with is not seen.
/// </summary>
/// <remarks>
/// The board is summed up, not given square by square: a network that weighs each square learns, from the
/// thousands of games there are to learn from, more by chance than by sense, and judges the moves before it
/// by little more than where its pieces happen to stand. Sums mean the same in every position.
/// </remarks>
public sealed class PositionEncoder
{
    /// <summary>How many faces of a die are told apart: a die showing more is not seen.</summary>
    public const int DieFaces = 6;

    private const double PieceScale = 8;
    private const double ScoreScale = 50;
    private const double FeatureScale = 10;

    private readonly IRulesApplying _rules;
    private readonly ImmutableList<string> _measures;
    private readonly ImmutableList<PieceKindId> _kinds;
    private readonly int _seats;
    private readonly ImmutableDictionary<Direction, ImmutableDictionary<CellId, int>> _rows;

    public PositionEncoder([Required] IRulesApplying rules)
    {
        GameState initial = rules.CreateInitialState();
        this._rules = rules;
        this._measures = rules is IPositionMeasuring measuring ? measuring.Measures : [];
        this._kinds =
        [
            .. initial.Position.Occupants.Values
                .Select(piece => piece.Kind)
                .Distinct()
                .OrderBy(kind => kind.Value, StringComparer.Ordinal),
        ];
        this._seats = initial.Seats.Count;
        this.Features = Appraisal
            .FeaturesOf(new DecisionRequest(rules, initial, initial.Seats[0].Id, []), initial)
            .Count;
        this._rows = initial.Seats
            .Select(seat => seat.Forward)
            .Distinct()
            .ToImmutableDictionary(forward => forward, forward => Rows(rules.Topology, forward));
    }

    /// <summary>How many numbers a position is.</summary>
    public int Size => this.FeaturesAt + this.Features;

    /// <summary>How many of the numbers are what Sage counts.</summary>
    private int Features { get; }

    private int PrisonersAt => this._seats * this._kinds.Count * 2;

    private int ScoresAt => this.PrisonersAt + (this._seats * this._seats * this._kinds.Count);

    private int DecidingAt => this.ScoresAt + this._seats;

    private int DiceAt => this.DecidingAt + this._seats;

    private int AwaitingAt => this.DiceAt + DieFaces;

    private int ResignedAt => this.AwaitingAt + 1;

    private int RetiredAt => this.ResignedAt + this._seats;

    private int FeaturesAt => this.RetiredAt + this._seats;

    /// <summary>The position as <paramref name="seat"/> sees it: each number that is not nothing, by index.</summary>
    public ImmutableList<(int Index, double Value)> Encode([Required] GameState state, SeatId seat)
    {
        IEnumerable<(int Index, double Value)> board = state.Position.Occupants
            .Select(occupant => (Kind: this._kinds.IndexOf(occupant.Value.Kind), occupant))
            .Where(seen => seen.Kind >= 0)
            .SelectMany(seen => this.Placed(state, seat, seen.occupant.Key, seen.occupant.Value.Owner, seen.Kind));
        IEnumerable<(int Index, double Value)> prisoners = state.Prisoners
            .Select(prisoner => (Kind: this._kinds.IndexOf(prisoner.Piece.Kind), prisoner))
            .Where(seen => seen.Kind >= 0)
            .Select(seen => (
                this.PrisonersAt
                    + (((this.Relative(seen.prisoner.Holder, seat) * this._seats)
                        + this.Relative(seen.prisoner.Piece.Owner, seat)) * this._kinds.Count)
                    + seen.Kind,
                1d
            ));
        IEnumerable<(int Index, double Value)> scores = state.Scores
            .Select(score => (this.ScoresAt + this.Relative(score.Key, seat), score.Value / ScoreScale));
        IEnumerable<(int Index, double Value)> deciding = state.DecidingSeat is { } decider
            ? [(this.DecidingAt + this.Relative(decider, seat), 1d)]
            : [];
        IEnumerable<(int Index, double Value)> dice = state.Dice
            .Where(value => value is >= 1 and <= DieFaces)
            .Select(value => (this.DiceAt + value - 1, 1d));
        IEnumerable<(int Index, double Value)> awaiting = state.AwaitedDice is null
            ? []
            : [(this.AwaitingAt, 1d)];
        IEnumerable<(int Index, double Value)> resigned =
            state.Resigned.Select(other => (this.ResignedAt + this.Relative(other, seat), 1d));
        IEnumerable<(int Index, double Value)> retired =
            state.Retired.Select(other => (this.RetiredAt + this.Relative(other, seat), 1d));
        IEnumerable<(int Index, double Value)> features = Appraisal
            .FeaturesOf(new DecisionRequest(this._rules, state, seat, []), state)
            .Select((value, at) => (this.FeaturesAt + at, value / FeatureScale));
        return
        [
            .. board.Concat(prisoners)
                .Concat(scores)
                .Concat(deciding)
                .Concat(dice)
                .Concat(awaiting)
                .Concat(resigned)
                .Concat(retired)
                .Concat(features)
                .GroupBy(entry => entry.Index)
                .Select(same => (same.Key, same.Sum(entry => entry.Value)))
                .Where(entry => entry.Item2 != 0)
                .OrderBy(entry => entry.Key),
        ];
    }

    /// <summary>
    /// What each of the numbers Sage counts weighs, by its index, as <paramref name="weights"/> weigh them: a
    /// weighted sum of a position's numbers so weighed is what Sage judges it worth.
    /// </summary>
    public ImmutableList<(int Index, double Weight)> Weighing([Required] SageWeights weights) =>
    [
        .. weights.Values
            .Take(this.Features - this._measures.Count)
            .Concat(this._measures.Select(name => weights.Measures.GetValueOrDefault(name)))
            .Select((weight, at) => (this.FeaturesAt + at, weight * FeatureScale)),
    ];

    /// <summary>
    /// A piece of <paramref name="owner"/>'s, of the <paramref name="kind"/>th kind, on <paramref name="cell"/>:
    /// one more of its kind, and the rows it has come from its army's own edge.
    /// </summary>
    private IEnumerable<(int Index, double Value)> Placed(
        GameState state,
        SeatId seat,
        CellId cell,
        SeatId owner,
        int kind
    )
    {
        int at = ((this.Relative(owner, seat) * this._kinds.Count) + kind) * 2;
        return [(at, 1 / PieceScale), (at + 1, this._rows[state.SeatOf(owner).Forward][cell] / PieceScale)];
    }

    /// <summary>Which of the seats <paramref name="other"/> is, counted on from <paramref name="seat"/>.</summary>
    private int Relative(SeatId other, SeatId seat) => (other.Value - seat.Value + this._seats) % this._seats;

    /// <summary>How many rows each cell is from the edge behind a seat facing <paramref name="forward"/>.</summary>
    private static ImmutableDictionary<CellId, int> Rows(ICellNavigating topology, Direction forward)
    {
        Direction back = new(-forward.First, -forward.Second);
        return topology.Cells.ToImmutableDictionary(cell => cell, cell => StepsFrom(topology, cell, back));
    }

    private static int StepsFrom(ICellNavigating topology, CellId cell, Direction direction) =>
        topology.Step(cell, direction) is { } next
            ? 1 + StepsFrom(topology, next, direction)
            : 0;
}
