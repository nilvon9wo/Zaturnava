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
/// says nothing), for its actor; all a bot can learn from them is what each action is worth.
/// </summary>
internal sealed class ScoringRules(ImmutableDictionary<GameAction, int> points) : IRulesApplying
{
    public static readonly SeatId Seat = new(0);

    private readonly ImmutableDictionary<GameAction, int> _points = points;

    public string Id => "scoring";

    public string Name => "Scoring";

    public ICellNavigating Topology => throw new NotSupportedException();

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

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();
}
