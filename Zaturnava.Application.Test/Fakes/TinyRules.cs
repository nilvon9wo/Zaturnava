using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

/// <summary>
/// The smallest ruleset that still ends: two seats take turns passing until <paramref name="length"/>
/// turns have been played; the second seat has only so long to pass. Enough to drive the
/// application without any real game.
/// </summary>
internal sealed class TinyRules(int length) : IRulesApplying
{
    public const string RulesetId = "tiny";
    public const int SeatCount = 2;
    public const string Timer = "tiny-turn";

    private readonly int _length = length;

    public string Id => RulesetId;

    public string Name => "Tiny";

    public ICellNavigating Topology => throw new NotSupportedException("Tiny has no board.");

    public ImmutableDictionary<SeatId, CellId> Thrones => [];

    public GameState CreateInitialState() =>
        new(
            Position.Empty,
            [new Seat(new SeatId(0), new Direction(0, 1)), new Seat(new SeatId(1), new Direction(0, -1))],
            TeamGraph.FromTeams([[new SeatId(0)], [new SeatId(1)]]),
            new SeatId(0),
            1,
            [],
            []
        );

    public ImmutableList<GameAction> LegalActions([Required] GameState state) =>
        state.DecidingSeat is { } seat
            ? [new PassAction(seat)]
            : [];

    public Transition Apply([Required] GameState state, [Required] GameAction action)
    {
        bool isLegal = this.LegalActions(state).Contains(action);
        GameState next = isLegal
            ? this.After(state)
            : throw new IllegalActionException(action);
        return new Transition(next, []);
    }

    public bool MayOffer([Required] GameState state) => false;

    public DecisionTimer? TimerOf([Required] GameState state) =>
        state.DecidingSeat?.Value == 1
            ? new DecisionTimer(Timer, new PassAction(new SeatId(1)))
            : null;

    public GameAction Parse([Required] ActionDescriptor descriptor) => ActionParser.Standard.Parse(descriptor);

    public string NameOf(PieceKindId kind) => kind.Value;

    public string NameOf(SeatId seat) => $"Seat {seat.Value}";

    public string NameOfDie(int value) => $"Die {value}";

    private GameState After(GameState state)
    {
        int turn = state.TurnNumber + 1;
        SeatId? next = turn > this._length
            ? null
            : new SeatId((state.DecidingSeat!.Value.Value + 1) % SeatCount);
        return state with { TurnNumber = turn, DecidingSeat = next };
    }
}
