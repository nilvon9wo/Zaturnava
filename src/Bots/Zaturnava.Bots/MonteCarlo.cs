using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Pythia's search (Proposed): Monte Carlo tree search, open loop. Each of <paramref name="iterations"/> passes
/// starts at the decision to make and follows the actions most worth trying, as UCT reckons them, down the tree,
/// rolling the dice afresh wherever the game waits on them, so that the tree is of actions alone and every way the
/// dice may fall is sampled as often as it is likely. Where the pass first reaches an action not tried yet, it
/// tries it, and the position it leads to is judged by <paramref name="valuing"/> (Sage's learned
/// <see cref="Appraisal"/>, say), as AlphaZero judges by its network rather than playing on at random; a pass also
/// stops, and judges, once the game is over or <paramref name="horizon"/> decisions deep. The bot and its allies
/// play for the most its team can make of a position, its enemies for the least; untried actions are tried in a
/// random order, so that a pass costs one judgement. Values are scaled to those seen so far, so that UCT's
/// <paramref name="exploration"/> means the same whatever the game's scale. It plays the action it tried most,
/// rather than pass, and at random among equals.
/// </summary>
internal sealed class MonteCarlo(IPositionValuing valuing, int iterations, int horizon, double exploration)
{
    /// <summary>The actions not tried: offers, proposals and votes, playing nothing on the board.</summary>
    private static readonly ImmutableHashSet<Type> NotPlay =
    [
        typeof(OfferAction), typeof(ProposeEndAction), typeof(VoteAwayAction), typeof(PassAwayAction),
        typeof(RejoinAction),
    ];

    /// <summary>How many decisions with one thing to do a pass may go through free, before they count.</summary>
    private const int MostForced = 32;

    private readonly IPositionValuing _valuing = valuing;
    private readonly int _iterations = iterations;
    private readonly int _horizon = horizon;
    private readonly double _exploration = exploration;

    public GameAction Choose(DecisionRequest request, Random random)
    {
        ImmutableList<GameAction> playable = Playable.Of(request);
        return playable.Count == 1
            ? playable[0]
            : this.Searched(request, playable, random);
    }

    private GameAction Searched(DecisionRequest request, ImmutableList<GameAction> playable, Random random)
    {
        Tree tree = new(request, this._valuing, random, this._exploration);
        Node root = Tree.Root(playable);
        for (int pass = 0; pass < this._iterations; pass++)
        {
            tree.Pass(root, this._horizon);
        }

        int most = root.Children.Max(child => child.Visits);
        ImmutableList<GameAction> top =
            [.. root.Children.Where(child => child.Visits == most).Select(child => child.Action!)];
        ImmutableList<GameAction> active = [.. top.Where(action => action is not PassAction)];
        ImmutableList<GameAction> choices = active.IsEmpty
            ? top
            : active;
        return choices[random.Next(choices.Count)];
    }

    /// <summary>Whether an action is play on the board, not an offer, a proposal or a vote.</summary>
    private static bool IsPlay(GameAction action) => !NotPlay.Contains(action.GetType());

    /// <summary>
    /// A decision in the tree, reached by <see cref="Action"/>: how often passes have gone through it, the value
    /// they found, to the bot's team, and the actions tried from it. Which may be tried is known only once a pass
    /// stands at it, as they may differ with the dice.
    /// </summary>
    private sealed class Node(GameAction? action)
    {
        public GameAction? Action { get; } = action;

        public int Visits { get; set; }

        public double Total { get; set; }

        public List<Node> Children { get; } = [];

        public double Mean => this.Total / this.Visits;
    }

    /// <summary>One decision's search: its passes share the tree, the dice and the range of values seen.</summary>
    private sealed class Tree(DecisionRequest request, IPositionValuing valuing, Random random, double exploration)
    {
        private readonly DecisionRequest _request = request;
        private readonly IPositionValuing _valuing = valuing;
        private readonly Random _random = random;
        private readonly double _exploration = exploration;
        private double _lowest = double.PositiveInfinity;
        private double _highest = double.NegativeInfinity;

        /// <summary>The root, each of <paramref name="playable"/> a child, none tried yet.</summary>
        public static Node Root(ImmutableList<GameAction> playable)
        {
            Node root = new(null);
            root.Children.AddRange(playable.Select(action => new Node(action)));
            return root;
        }

        /// <summary>A pass down the tree from <paramref name="root"/>, and its value back up the way it came.</summary>
        public void Pass(Node root, int horizon)
        {
            List<Node> path = [root];
            GameState state = this._request.State;
            Node node = root;
            int depth = 0;
            int forced = 0;
            bool isNew = false;
            while (!isNew && state.Outcome is null && depth < horizon)
            {
                SeatId seat = state.DecidingSeat.GetValueOrDefault(); // a game not over waits on someone
                if (state.AwaitedDice is { } dice)
                {
                    state = this.Apply(state, new DiceAction(seat, this.Roll(dice)));
                    continue;
                }

                DecisionRequest deciding = this.Deciding(state, seat);
                ImmutableList<GameAction> legal = node == root
                    ? [.. root.Children.Select(child => child.Action!)]
                    : [.. Playable.Of(deciding).Where(IsPlay)];
                if (legal.IsEmpty)
                {
                    break;
                }

                if (legal.Count == 1 && forced < MostForced)
                {
                    forced++;
                    state = this.Apply(state, legal[0]);
                    continue;
                }

                (node, isNew) = this.Step(node, legal, deciding, state);
                state = this.Apply(state, node.Action!);
                path.Add(node);
                depth++;
            }

            double value = this._valuing.ValueOf(this._request, state);
            this._lowest = Math.Min(this._lowest, value);
            this._highest = Math.Max(this._highest, value);
            foreach (Node passed in path)
            {
                passed.Visits++;
                passed.Total += value;
            }
        }

        /// <summary>
        /// The child to go on to: an action untried here, at random, if any is legal now; else the tried one UCT
        /// rates best for the seat deciding. Whether it is new.
        /// </summary>
        private (Node Next, bool IsNew) Step(
            Node node,
            ImmutableList<GameAction> legal,
            DecisionRequest deciding,
            GameState state
        )
        {
            ImmutableHashSet<GameAction> allowed = [.. legal];
            ImmutableHashSet<GameAction> tried =
            [
                .. node.Children.Where(child => child.Visits > 0).Select(child => child.Action!),
            ];
            ImmutableList<GameAction> untried = [.. legal.Where(action => !tried.Contains(action))];
            if (untried.IsEmpty)
            {
                bool isTeams = state.Teams.AreAllied(deciding.Seat, this._request.Seat);
                Node best = node.Children
                    .Where(child => allowed.Contains(child.Action!))
                    .MaxBy(child => this.Rating(child, node.Visits, isTeams))!;
                return (best, false);
            }

            GameAction first = untried[this._random.Next(untried.Count)];
            Node? known = node.Children.Find(child => child.Action == first);
            if (known is not null)
            {
                return (known, true);
            }

            Node next = new(first);
            node.Children.Add(next);
            return (next, true);
        }

        /// <summary>
        /// UCT's rating of a child, for its parent's seat: its mean value scaled to those seen so far, from the
        /// bot's team's side if the seat is on it, else from the other side; and a bonus for having been tried
        /// seldom.
        /// </summary>
        private double Rating(Node child, int parentVisits, bool isTeams)
        {
            double range = this._highest - this._lowest;
            double scaled = range > 0
                ? (child.Mean - this._lowest) / range
                : 0.5;
            double exploit = isTeams
                ? scaled
                : 1 - scaled;
            return exploit + (this._exploration * Math.Sqrt(Math.Log(parentVisits) / child.Visits));
        }

        /// <summary>How the dice fall this time: each at random, ordered as <see cref="Foresight"/> has them.</summary>
        private ImmutableList<int> Roll(DiceRequest dice) =>
            [.. Enumerable.Range(0, dice.Count).Select(_ => this._random.Next(1, dice.Sides + 1)).Order()];

        private DecisionRequest Deciding(GameState state, SeatId seat) =>
            this._request with { State = state, Seat = seat, LegalActions = this._request.Rules.LegalActions(state) };

        private GameState Apply(GameState state, GameAction action) =>
            this._request.Rules.Apply(state, action).State;
    }
}
