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
/// position is given as only those that are not, by index, in order. The kinds of piece are every kind the
/// ruleset plays (<see cref="IRulesApplying.Kinds"/>), and those a game starts with: not those of one deal alone
/// (2026-11-26: a Grand Legions network sized by the armies its first seed dealt saw none of the kinds other deals
/// gave, and fitted no longer once the catalogue changed what that seed deals, which stopped a night's learning).
/// </summary>
/// <remarks>
/// The board is summed up; and, for an encoder that sees the board (<see cref="SeesBoard"/>), also given square by
/// square, last (the user, 2026-11-27: bots "seeing where the pieces stand and how they move", not "backgammon in 2D"):
/// for each square, as the seat sees the board from its own edge, and each seat counted on from it, what its piece
/// there is worth, whether it leads, whether it is heavy, and how many of its pieces attack the square. What a
/// piece is worth, not what kind it is, so that what is learned of one kind serves every kind of a worth, as Grand
/// Legions deals ever other armies; and the attacks, so that it sees what is threatened and what is guarded, not only
/// where pieces stand. Summed alone, the board means the same in every position, but tells nothing of where.
/// </remarks>
public sealed class PositionEncoder
{
    /// <summary>How many faces of a die are told apart: a die showing more is not seen.</summary>
    public const int DieFaces = 6;

    /// <summary>How many numbers each square is, for each seat (<see cref="SeesBoard"/>).</summary>
    public const int PerSquare = 4;

    private const double PieceScale = 8;
    private const double ScoreScale = 50;
    private const double FeatureScale = 10;
    private const double WorthScale = 10;
    private const double AttackScale = 4;

    private readonly IRulesApplying _rules;
    private readonly ImmutableList<string> _measures;
    private readonly ImmutableList<PieceKindId> _kinds;
    private readonly int _seats;
    private readonly ImmutableDictionary<Direction, ImmutableDictionary<CellId, int>> _rows;
    private readonly ImmutableDictionary<Direction, ImmutableDictionary<CellId, int>> _squares;
    private readonly int _squareCount;

    public PositionEncoder([Required] IRulesApplying rules)
        : this(rules, int.MaxValue, seesBoard: false)
    {
    }

    /// <summary>An encoder that gives the board square by square too, if <paramref name="seesBoard"/>.</summary>
    public PositionEncoder([Required] IRulesApplying rules, bool seesBoard)
        : this(rules, int.MaxValue, seesBoard)
    {
    }

    /// <summary>
    /// An encoder seeing only the first <paramref name="measures"/> of the ruleset's own measures: as a network
    /// that learned before the ruleset offered the rest sees it.
    /// </summary>
    private PositionEncoder(IRulesApplying rules, int measures, bool seesBoard)
    {
        this.SeesBoard = seesBoard;
        GameState initial = rules.CreateInitialState();
        this._rules = rules;
        this._measures = rules is IPositionMeasuring measuring ? [.. measuring.Measures.Take(measures)] : [];
        this._kinds =
        [
            .. initial.Position.Occupants.Values
                .Select(piece => piece.Kind)
                .Concat(rules.Kinds)
                .Distinct()
                .OrderBy(kind => kind.Value, StringComparer.Ordinal),
        ];
        this._seats = initial.Seats.Count;
        this.Features = Appraisal
            .FeaturesOf(new DecisionRequest(rules, initial, initial.Seats[0].Id, []), initial)
            .Count - (rules is IPositionMeasuring all ? all.Measures.Count - this._measures.Count : 0);
        this._rows = initial.Seats
            .Select(seat => seat.Forward)
            .Distinct()
            .ToImmutableDictionary(forward => forward, forward => Rows(rules.Topology, forward));
        ImmutableDictionary<Direction, ImmutableDictionary<CellId, (int Row, int Column)>> placed = initial.Seats
            .Select(seat => seat.Forward)
            .Distinct()
            .ToImmutableDictionary(forward => forward, forward => Placed(rules.Topology, forward));
        ImmutableList<(int Row, int Column)> cells = [.. placed.Values.SelectMany(facing => facing.Values)];
        int columns = 1 + cells.Select(at => at.Column).Append(0).Max();
        this._squareCount = columns * (1 + cells.Select(at => at.Row).Append(0).Max());
        this._squares = placed.ToImmutableDictionary(
            facing => facing.Key,
            facing => facing.Value.ToImmutableDictionary(
                cell => cell.Key,
                cell => (cell.Value.Row * columns) + cell.Value.Column
            )
        );
    }

    /// <summary>Whether the board is given square by square too, after all else.</summary>
    public bool SeesBoard { get; }

    /// <summary>How many numbers a position is.</summary>
    public int Size => this.SquaresAt + (this.SeesBoard ? this._seats * this._squareCount * PerSquare : 0);

    /// <summary>
    /// An encoder giving the ruleset's positions as <paramref name="inputs"/> numbers, if any does: all its own
    /// measures, or only the first of them, for a network that learned before the ruleset offered the rest, which
    /// come last but for the board's squares; the squares too, if a network of their size learned them; else null.
    /// </summary>
    public static PositionEncoder? Fitting([Required] IRulesApplying rules, int inputs) =>
        Fitting(rules, inputs, seesBoard: true) ?? Fitting(rules, inputs, seesBoard: false);

    private static PositionEncoder? Fitting(IRulesApplying rules, int inputs, bool seesBoard)
    {
        PositionEncoder full = new(rules, seesBoard);
        int known = full._measures.Count - (full.Size - inputs);
        return known >= 0 && known <= full._measures.Count ? new PositionEncoder(rules, known, seesBoard) : null;
    }

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

    private int SquaresAt => this.FeaturesAt + this.Features;

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
            .Take(this.Features)
            .Select((value, at) => (this.FeaturesAt + at, value / FeatureScale));
        IEnumerable<(int Index, double Value)> squares = this.SeesBoard ? this.Squares(state, seat) : [];
        return
        [
            .. board.Concat(prisoners)
                .Concat(squares)
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
    /// weighted sum of a position's numbers so weighed is what Sage judges it worth, if it weighs each side alike;
    /// else, as near as one weight for the team's less its enemies' comes (<see cref="Sided.Mean"/>).
    /// </summary>
    public ImmutableList<(int Index, double Weight)> Weighing([Required] SageWeights weights) =>
    [
        .. new[]
            {
                weights.Score.Mean, weights.Material.Mean, weights.Leaders.Mean, weights.Prisoners.Mean,
                weights.Thrones.Mean, weights.Exposure, weights.Chances, weights.Mobility.Mean,
            }
            .Concat(this._measures.Select(name => weights.Measures.GetValueOrDefault(name).Mean))
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

    /// <summary>
    /// The board square by square as <paramref name="seat"/> sees it from its own edge: for each seat counted on
    /// from it, its piece on each square, by worth, leading and heavy; and its attacks on each square.
    /// </summary>
    private IEnumerable<(int Index, double Value)> Squares(GameState state, SeatId seat)
    {
        ImmutableDictionary<CellId, int> squares = this._squares[state.SeatOf(seat).Forward];
        int At(SeatId owner, CellId cell, int feature) =>
            this.SquaresAt + (((this.Relative(owner, seat) * this._squareCount) + squares[cell]) * PerSquare) + feature;
        IEnumerable<(int Index, double Value)> pieces = state.Position.Occupants.SelectMany(occupant =>
            new (int Feature, double Value)[]
                {
                    (0, this._rules.WorthOf(occupant.Value.Kind) / WorthScale),
                    (1, this._rules.IsLeader(occupant.Value.Kind) ? 1 : 0),
                    (2, this._rules.IsHeavy(occupant.Value.Kind) ? 1 : 0),
                }
                .Select(entry => (At(occupant.Value.Owner, occupant.Key, entry.Feature), entry.Value))
        );
        IEnumerable<(int Index, double Value)> attacks = state.Seats.SelectMany(other => this._rules
            .AttacksOf(state, other.Id)
            .Select(attack => (At(other.Id, attack.Target, 3), 1 / AttackScale))
        );
        return pieces.Concat(attacks);
    }

    /// <summary>Which of the seats <paramref name="other"/> is, counted on from <paramref name="seat"/>.</summary>
    private int Relative(SeatId other, SeatId seat) => (other.Value - seat.Value + this._seats) % this._seats;

    /// <summary>How many rows each cell is from the edge behind a seat facing <paramref name="forward"/>.</summary>
    private static ImmutableDictionary<CellId, int> Rows(ICellNavigating topology, Direction forward)
    {
        Direction back = new(-forward.First, -forward.Second);
        return topology.Cells.ToImmutableDictionary(cell => cell, cell => StepsFrom(topology, cell, back));
    }

    /// <summary>
    /// Where each cell stands to a seat facing <paramref name="forward"/>: how many rows from the edge behind it, and
    /// how many columns from the edge on its left (a quarter turn from forward, one way for every seat).
    /// </summary>
    private static ImmutableDictionary<CellId, (int Row, int Column)> Placed(
        ICellNavigating topology,
        Direction forward
    )
    {
        Direction back = new(-forward.First, -forward.Second);
        Direction left = new(-forward.Second, forward.First);
        return topology.Cells.ToImmutableDictionary(
            cell => cell,
            cell => (StepsFrom(topology, cell, back), StepsFrom(topology, cell, left))
        );
    }

    private static int StepsFrom(ICellNavigating topology, CellId cell, Direction direction) =>
        topology.Step(cell, direction) is { } next
            ? 1 + StepsFrom(topology, next, direction)
            : 0;
}
