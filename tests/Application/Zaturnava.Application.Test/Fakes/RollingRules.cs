using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

/// <summary>
/// The smallest ruleset with dice: two seats take turns rolling one die, which scores its value,
/// until <paramref name="length"/> turns have been played.
/// </summary>
internal sealed class RollingRules(int length) : IRulesApplying
{
    public const string RulesetId = "rolling";

    private static readonly DiceRequest OneDie = new(1, 6);

    private readonly int _length = length;

    public string Id => RulesetId;

    public string Name => "Rolling";

    public ICellNavigating Topology => throw new NotSupportedException("Rolling has no board.");

    public ImmutableDictionary<SeatId, CellId> Thrones => [];

    public GameState CreateInitialState() =>
        new(
            Position.Empty,
            [new Seat(new SeatId(0), new Direction(0, 1)), new Seat(new SeatId(1), new Direction(0, -1))],
            TeamGraph.FromTeams([[new SeatId(0)], [new SeatId(1)]]),
            new SeatId(0),
            1,
            ImmutableDictionary<SeatId, int>.Empty.Add(new SeatId(0), 0).Add(new SeatId(1), 0),
            []
        );

    public ImmutableList<GameAction> LegalActions([Required] GameState state) =>
        (state.DecidingSeat, state.AwaitedDice) is ({ } seat, null)
            ? [new RollAction(seat)]
            : [];

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        action switch
        {
            DiceAction dice when state.AwaitedDice is not null =>
                new Transition(this.Scored(state, dice), [new DiceRolled(dice.Actor, dice.Values)]),
            RollAction when this.LegalActions(state).Contains(action) =>
                new Transition(state with { AwaitedDice = OneDie }, []),
            _ => throw new IllegalActionException(action),
        };

    public bool MayOffer([Required] GameState state) => false;

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) => true;

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => false;

    public bool MayRejoin([Required] GameState state, SeatId seat) => false;

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) => false;

    public DecisionTimer? TimerOf([Required] GameState state) => null;

    public GameAction Parse([Required] ActionDescriptor descriptor) => ActionParser.Standard.Parse(descriptor);

    public string NameOf(PieceKindId kind) => kind.Value;

    public string NameOf(SeatId seat) => $"Seat {seat.Value}";

    public int WorthOf(PieceKindId kind) => 1;

    public int RansomPointsOf([Required] GameState state, [Required] RansomOffer offer) => 0;

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) => [];

    public bool IsLeader(PieceKindId kind) => false;

    public bool IsHeavy(PieceKindId kind) => false;

    public string NameOfDie(int value) => $"Die {value}";

    public bool MayUseDie([Required] GameState state, int value) => true;

    private GameState Scored(GameState state, DiceAction dice)
    {
        int turn = state.TurnNumber + 1;
        SeatId? next = turn > this._length
            ? null
            : new SeatId((dice.Actor.Value + 1) % 2);
        return state with
        {
            AwaitedDice = null,
            Scores = state.Scores.SetItem(dice.Actor, state.Scores[dice.Actor] + dice.Values.Sum()),
            TurnNumber = turn,
            DecidingSeat = next,
        };
    }
}
