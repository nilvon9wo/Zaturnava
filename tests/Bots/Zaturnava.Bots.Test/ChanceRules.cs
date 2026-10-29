using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

/// <summary>
/// A turn of one choice: a deliberate move scoring <paramref name="deliberate"/> for certain; rolling one
/// two-sided die, then a move scoring three for each pip it shows; or passing. Any of them ends the turn,
/// but rolling, which waits on the die.
/// </summary>
internal sealed class ChanceRules(int deliberate) : IRulesApplying
{
    public const int PerPip = 3;

    public static readonly SeatId Seat = new(0);
    public static readonly SeatId Enemy = new(1);
    public static readonly MoveAction Deliberate = new(Seat, new CellId(0), new CellId(1));
    public static readonly MoveAction UseDie = new(Seat, new CellId(0), new CellId(2));
    public static readonly RollAction Roll = new(Seat);

    private readonly int _deliberate = deliberate;

    public string Id => "chance";

    public string Name => "Chance";

    public ICellNavigating Topology => throw new NotSupportedException();

    public ImmutableDictionary<SeatId, CellId> Thrones => [];

    /// <summary>Seat 0 deciding how to play its turn.</summary>
    public DecisionRequest Request()
    {
        GameState state = new(Position.Empty, [], TeamGraph.FromTeams([[Seat], [Enemy]]), Seat, 1, [], []);
        return new DecisionRequest(this, state, Seat, this.LegalActions(state));
    }

    public GameState CreateInitialState() => throw new NotSupportedException();

    public ImmutableList<GameAction> LegalActions([Required] GameState state) =>
        (state.DecidingSeat == Seat, state.Dice.IsEmpty) switch
        {
            (false, _) => [],
            (_, true) => [Deliberate, Roll, new PassAction(Seat)],
            _ => [UseDie, new PassAction(Seat)],
        };

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        new(
            action switch
            {
                RollAction => state with { AwaitedDice = new DiceRequest(1, 2) },
                DiceAction dice => state with { AwaitedDice = null, Dice = dice.Values },
                MoveAction move when move == Deliberate => Over(state, this._deliberate),
                MoveAction => Over(state, PerPip * state.Dice[0]),
                _ => Over(state, 0),
            },
            []
        );

    public bool MayOffer([Required] GameState state) => false;

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) => true;

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => false;

    public bool MayRejoin([Required] GameState state, SeatId seat) => false;

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) => false;

    public DecisionTimer TimerOf([Required] GameState state) => throw new NotSupportedException();

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public int WorthOf(PieceKindId kind) => ScoringRules.Worth[kind];

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) => [];

    public bool IsLeader(PieceKindId kind) => false;

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();

    public bool MayUseDie([Required] GameState state, int value) => true;

    private static GameState Over(GameState state, int points) =>
        state with
        {
            Scores = state.Scores.SetItem(Seat, state.Scores.GetValueOrDefault(Seat) + points),
            Dice = [],
            DecidingSeat = Enemy,
        };
}
