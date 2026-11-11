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
/// One node of a <see cref="TreeRules"/> game: who decides there (no one, once the game is over), what seats 0 and 2
/// have scored by then, and the nodes it leads to - one for each action of the seat deciding, or for each face of
/// the die it waits on, if it does (<see cref="IsDie"/>).
/// </summary>
internal sealed record TreeNode(SeatId? Seat, int Points, ImmutableList<int> Next)
{
    /// <summary>What seat 2, seat 0's ally, has scored by then.</summary>
    public int AllyPoints { get; init; }

    /// <summary>Whether the node waits on a die, with as many faces as it leads to nodes.</summary>
    public bool IsDie { get; init; }

    /// <summary>A node at which the game is over, seat 0 having scored <paramref name="points"/>.</summary>
    public static TreeNode Over(int points) => new(null, points, []);
}

/// <summary>
/// Rules of a game tree the test draws, node by node (<see cref="TreeNode"/>), from node 0: a state is a node, its
/// turn number the node's. Seat <c>s</c> moving from node <c>n</c> to node <c>m</c> is its move from cell n to cell m.
/// Seats 0 and 2 are allied against 1 and 3.
/// </summary>
internal sealed class TreeRules(ImmutableDictionary<int, TreeNode> nodes) : IRulesApplying
{
    public static readonly SeatId Seat = new(0);
    public static readonly SeatId Enemy = new(1);
    public static readonly SeatId Ally = new(2);

    private readonly ImmutableDictionary<int, TreeNode> _nodes = nodes;

    public string Id => "tree";

    public string Name => "Tree";

    public ICellNavigating Topology { get; } = new LineTopology();

    public ImmutableDictionary<SeatId, CellId> Thrones => [];

    /// <summary>Seat 0 deciding at node 0.</summary>
    public DecisionRequest Request()
    {
        GameState state = this.StateOf(0);
        return new DecisionRequest(this, state, Seat, this.LegalActions(state));
    }

    /// <summary>Seat 0's move from node <paramref name="from"/> to node <paramref name="to"/>.</summary>
    public static MoveAction Move(int from, int to) => MoveOf(Seat, from, to);

    private static MoveAction MoveOf(SeatId seat, int from, int to) => new(seat, new CellId(from), new CellId(to));

    public GameState CreateInitialState() => this.StateOf(0);

    public ImmutableList<GameAction> LegalActions([Required] GameState state)
    {
        TreeNode node = this._nodes[state.TurnNumber];
        return node.IsDie
            ? []
            : [.. node.Next.Select(next => MoveOf(node.Seat!.Value, state.TurnNumber, next))];
    }

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        new(
            this.StateOf(
                action is DiceAction dice
                    ? this._nodes[state.TurnNumber].Next[dice.Values[0] - 1]
                    : ((MoveAction)action).To.Value
            ),
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

    public int RansomPointsOf([Required] GameState state, [Required] RansomOffer offer) => 0;

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) => [];

    public bool IsLeader(PieceKindId kind) => false;

    public bool IsHeavy(PieceKindId kind) => false;

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();

    public bool MayUseDie([Required] GameState state, int value) => true;

    private GameState StateOf(int number)
    {
        TreeNode node = this._nodes[number];
        return new GameState(
            Position.Empty,
            [.. Enumerable.Range(0, 4).Select(seat => new Seat(new SeatId(seat), new Direction(1, 0)))],
            TeamGraph.FromTeams([[Seat, Ally], [Enemy, new SeatId(3)]]),
            node.Seat,
            number,
            ImmutableDictionary<SeatId, int>.Empty.Add(Seat, node.Points).Add(Ally, node.AllyPoints),
            []
        )
        {
            AwaitedDice = node.IsDie ? new DiceRequest(1, node.Next.Count) : null,
            Outcome = node.Seat is null ? new GameOutcome([], "over", "over") : null,
        };
    }
}
