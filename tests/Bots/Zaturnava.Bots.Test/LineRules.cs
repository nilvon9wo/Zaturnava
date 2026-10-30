using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

/// <summary>
/// Rules on a line of cells, in which every piece attacks the cells either side of it - but a Boat
/// attacks along the line either way, up to the first piece in its way - and the legal actions are
/// whichever the test says, but for moves from a cell left empty. A move goes where it says, and scores
/// its actor what it takes (<see cref="ScoringRules.Worth"/>); passing does nothing. Seats 0 and 2 are
/// allied against 1 and 3, the even seats facing up the line and the odd ones down it; seat 0's throne is cell
/// 0, and the Raja leads. Its one measure of its own is how many Pawns an army has. A game starts with seat
/// 0's Raja and Pawn at one end, and seat 1's at the other.
/// </summary>
internal sealed class LineRules(ImmutableList<GameAction> legalActions) : IRulesApplying, IPositionMeasuring
{
    public static readonly SeatId Seat = new(0);
    public static readonly SeatId Enemy = new(1);
    public static readonly CellId Throne = new(0);
    public static readonly PieceKindId Pawn = new("pawn");
    public static readonly PieceKindId Horse = new("horse");
    public static readonly PieceKindId Boat = new("boat");
    public static readonly PieceKindId Raja = new("raja");

    private const int Length = 8;

    private readonly ImmutableList<GameAction> _legalActions = legalActions;

    public string Id => "line";

    public string Name => "Line";

    public ICellNavigating Topology { get; } = new LineTopology();

    public ImmutableDictionary<SeatId, CellId> Thrones => ImmutableDictionary<SeatId, CellId>.Empty.Add(Seat, Throne);

    /// <summary>Seat 0 deciding, among <paramref name="legalActions"/>, with each piece on its cell.</summary>
    public static DecisionRequest Request(
        ImmutableList<GameAction> legalActions,
        params (int Cell, PieceKindId Kind, SeatId Owner)[] pieces
    )
    {
        LineRules rules = new(legalActions);
        ImmutableList<Seat> seats =
        [
            .. Enumerable.Range(0, 4)
                .Select(index => new Seat(new SeatId(index), new Direction(1 - (index % 2 * 2), 0))),
        ];
        Position position = pieces
            .Select((piece, index) => (piece, index))
            .Aggregate(
                Position.Empty,
                (placed, next) => placed.Place(
                    new CellId(next.piece.Cell),
                    new Piece(new PieceId(next.index), next.piece.Kind, next.piece.Owner)
                )
            );
        TeamGraph teams = TeamGraph.FromTeams([[Seat, new SeatId(2)], [Enemy, new SeatId(3)]]);
        GameState state = new(position, seats, teams, Seat, 1, [], []);
        return new DecisionRequest(rules, state, Seat, rules.LegalActions(state));
    }

    public static MoveAction Move(int from, int to) => new(Seat, new CellId(from), new CellId(to));

    public GameState CreateInitialState() =>
        Request(
            this._legalActions,
            (0, Raja, Seat),
            (1, Pawn, Seat),
            (Length - 2, Pawn, Enemy),
            (Length - 1, Raja, Enemy)
        ).State;

    public ImmutableList<GameAction> LegalActions([Required] GameState state) =>
        [
            .. this._legalActions.Where(action =>
                action is not MoveAction move || state.Position.PieceAt(move.From) is not null
            ),
        ];

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        action is MoveAction move
            ? new Transition(Moved(state, move), [])
            : new Transition(state, []);

    public bool MayOffer([Required] GameState state) => false;

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) => true;

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => false;

    public bool MayRejoin([Required] GameState state, SeatId seat) => false;

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) => false;

    public DecisionTimer TimerOf([Required] GameState state) => throw new NotSupportedException();

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public int WorthOf(PieceKindId kind) => ScoringRules.Worth[kind];

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) =>
    [
        .. state.Position.Occupants
            .Where(occupant => occupant.Value.Owner == seat)
            .SelectMany(occupant => new[] { -1, 1 }
                .SelectMany(step => Reach(state, occupant.Value, occupant.Key.Value, step))
                .Select(cell => new Attack(occupant.Value, occupant.Key, new CellId(cell)))
            ),
    ];

    /// <summary>The cells a piece attacks one way: the next, or, for a Boat, every one up to a piece.</summary>
    private static IEnumerable<int> Reach(GameState state, Piece piece, int from, int step)
    {
        int cell = from + step;
        while (cell is >= 0 and < Length)
        {
            yield return cell;
            if (piece.Kind != Boat || state.Position.PieceAt(new CellId(cell)) is not null)
            {
                yield break;
            }

            cell += step;
        }
    }

    public bool IsLeader(PieceKindId kind) => kind == Raja;

    public ImmutableList<string> Measures => ["pawns"];

    public ImmutableList<double> MeasuresOf([Required] GameState state, SeatId seat) =>
        [state.Position.Occupants.Values.Count(piece => piece.Owner == seat && piece.Kind == Pawn)];

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();

    public bool MayUseDie([Required] GameState state, int value) => true;

    private static GameState Moved(GameState state, MoveAction move)
    {
        Piece? taken = state.Position.PieceAt(move.To);
        int gain = taken is null
            ? 0
            : ScoringRules.Worth[taken.Kind];
        return state with
        {
            Position = state.Position.Relocate(move.From, move.To),
            Scores = state.Scores.SetItem(move.Actor, state.Scores.GetValueOrDefault(move.Actor) + gain),
        };
    }
}

/// <summary>The cells of a line, 0 up to 7: a step goes along it, and one any other way goes off it.</summary>
internal sealed class LineTopology : ICellNavigating
{
    private const int Length = 8;

    public ImmutableList<CellId> Cells { get; } = [.. Enumerable.Range(0, Length).Select(cell => new CellId(cell))];

    public CellId? Step(CellId from, Direction direction) =>
        (direction.Second, from.Value + direction.First) switch
        {
            (0, >= 0 and < Length) => new CellId(from.Value + direction.First),
            _ => null,
        };

    public ImmutableList<Direction> Resolve([Required] DirectionSet set, Direction forward) =>
        throw new NotSupportedException();

    public CellLayout LayoutOf(CellId cell) => throw new NotSupportedException();

    public string NameOf(CellId cell) => throw new NotSupportedException();
}
