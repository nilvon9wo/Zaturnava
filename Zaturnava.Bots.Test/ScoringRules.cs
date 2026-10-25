using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

/// <summary>
/// Rules in which each action scores whatever <paramref name="points"/> says for it (nothing, if it
/// says nothing), for its actor; all a bot can learn from them is what each action is worth, and what
/// each kind of piece is worth (<see cref="Worth"/>). Any seat may offer once its turn is over.
/// </summary>
internal sealed class ScoringRules(ImmutableDictionary<GameAction, int> points) : IRulesApplying
{
    public static readonly SeatId Seat = new(0);

    public static readonly ImmutableDictionary<PieceKindId, int> Worth =
        ImmutableDictionary<PieceKindId, int>.Empty
            .Add(new PieceKindId("pawn"), 1)
            .Add(new PieceKindId("horse"), 3)
            .Add(new PieceKindId("raja"), 5);

    private readonly ImmutableDictionary<GameAction, int> _points = points;

    public string Id => "scoring";

    public string Name => "Scoring";

    public ICellNavigating Topology => throw new NotSupportedException();

    public ImmutableDictionary<SeatId, CellId> Thrones => throw new NotSupportedException();

    public static GameState State() => new(Position.Empty, [], TeamGraph.FromTeams([]), Seat, 1, [], []);

    public GameState CreateInitialState() => State();

    public ImmutableList<GameAction> LegalActions([Required] GameState state) => [.. this._points.Keys];

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        new(
            state with
            {
                Scores = state.Scores.SetItem(
                    action.Actor,
                    state.Scores.GetValueOrDefault(action.Actor) + this._points.GetValueOrDefault(action)
                ),
            },
            []
        );

    public bool MayOffer([Required] GameState state) => true;

    public DecisionTimer TimerOf([Required] GameState state) => throw new NotSupportedException();

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public int WorthOf(PieceKindId kind) => Worth[kind];

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();
}
