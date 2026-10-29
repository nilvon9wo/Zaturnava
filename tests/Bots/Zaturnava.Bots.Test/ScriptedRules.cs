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
/// Rules in which each action the test names leads straight to the state it says, the turn passed to the
/// enemy - or to no one, once the game is over. Seats 0 and 2 are allied against 1 and 3, each with its
/// throne on the cell of its own number; a piece is worth what <see cref="ScoringRules.Worth"/> says, and
/// the Raja leads.
/// </summary>
internal sealed class ScriptedRules(ImmutableDictionary<GameAction, GameState> results) : IRulesApplying
{
    public static readonly SeatId Seat = new(0);
    public static readonly SeatId Enemy = new(1);
    public static readonly SeatId Ally = new(2);

    private readonly ImmutableDictionary<GameAction, GameState> _results = results;

    public string Id => "scripted";

    public string Name => "Scripted";

    public ICellNavigating Topology => throw new NotSupportedException();

    public ImmutableDictionary<SeatId, CellId> Thrones =>
        Enumerable.Range(0, 4).ToImmutableDictionary(seat => new SeatId(seat), seat => new CellId(seat));

    /// <summary>Seat 0 deciding, each piece on its cell.</summary>
    public static GameState State(params (int Cell, PieceKindId Kind, SeatId Owner)[] pieces) =>
        new(
            pieces
                .Select((piece, index) => (piece, index))
                .Aggregate(
                    Position.Empty,
                    (placed, next) => placed.Place(
                        new CellId(next.piece.Cell),
                        new Piece(new PieceId(next.index), next.piece.Kind, next.piece.Owner)
                    )
                ),
            [],
            TeamGraph.FromTeams([[Seat, Ally], [Enemy, new SeatId(3)]]),
            Seat,
            1,
            [],
            []
        );

    /// <summary>Seat 0 deciding which of the scripted actions to take.</summary>
    public DecisionRequest Request()
    {
        GameState state = State();
        return new DecisionRequest(this, state, Seat, this.LegalActions(state));
    }

    public GameState CreateInitialState() => throw new NotSupportedException();

    public ImmutableList<GameAction> LegalActions([Required] GameState state) =>
        state.DecidingSeat == Seat ? [.. this._results.Keys] : [];

    public Transition Apply([Required] GameState state, [Required] GameAction action)
    {
        GameState result = this._results[action];
        return new Transition(result with { DecidingSeat = result.Outcome is null ? Enemy : null }, []);
    }

    public bool MayOffer([Required] GameState state) => false;

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) => true;

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => false;

    public bool MayRejoin([Required] GameState state, SeatId seat) => false;

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) => false;

    public DecisionTimer TimerOf([Required] GameState state) => throw new NotSupportedException();

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public int WorthOf(PieceKindId kind) => ScoringRules.Worth[kind];

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) => [];

    public bool IsLeader(PieceKindId kind) => kind == LineRules.Raja;

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();

    public bool MayUseDie([Required] GameState state, int value) => true;
}
